using System;
using System.IO;
using System.Threading;
using Mars_Rover_Configuration;
using Mars_Rover_Comms;
using System.Xml.Serialization;
using Mars_Rover_RCU.Comms;
using Mars_Rover_RCU.Controllers;
using Mars_Rover_RCU.Utilities;

namespace Mars_Rover_RCU
{ 
    public class Program
    {

        private static readonly short closed = 0;
        private static readonly short open = 1;
        private static readonly short tank = 3;
        private static readonly short translate = 2;
        private static readonly short rotate = 1;
        private static readonly short normal = 0;
        private static readonly short STOP = 1500;

        public static short shoulderPos = 464;
        public static short elbowPos = 1000;
        public static short wristPos = 2000;
        public static short gripperPos = 1000;

        private static RCUComms comms;

        static bool debug = true;

        static bool arduinoReady = false;

        static ConfigureRCU rcuConfig; //Responsible for sending states back to OCU
        static StreamWriter log;

        static public Controllers.Maestro _Maestro;
        static public Controllers.DriveController _DriveController;
        static public Controllers.ServoController _ServoController;

        //Sensors
        static public Controllers.Sensors _Sensors;
        static public String[] sensorData;

        static public Controllers.PID _PID;

        static Utility.UpdateQueue<RobotState> stateQueue = new Utility.UpdateQueue<RobotState>(-1);
        static XmlSerializer robotStateDeserializer = new XmlSerializer(typeof(Mars_Rover_Comms.RobotState));

        //tcp/ip client for communicating with the ocu
        static public Mars_Rover_Comms.TCP.ZClient client;

        //static public bool connected;

        static Thread stateProcessor;
        static CancellationTokenSource tokenSource = new CancellationTokenSource();

        //static Mars_Rover_RCU.Kinematics.Kinematics kinematics;

        static DateTime APMconnectTime = DateTime.Now;

        static public string IPAddress;
        static public string Port;
        static public string SensorCOM;
        static public string DriveCOM;
        static public string ServoCOM;

        static private bool useMaestro = false;
        static private bool useSensors = true;
        static private bool usePID = true;
        static private bool useArduino = true;
        static private bool useServos = true;

        public static void Main(string[] args)
        {
            
            System.IO.StreamReader file = new System.IO.StreamReader("..\\..\\IP_Port.txt");
            IPAddress = file.ReadLine();
            Port = file.ReadLine();
            SensorCOM = file.ReadLine();
            DriveCOM = file.ReadLine();
            ServoCOM = file.ReadLine();
            //setup primary comms
            client = new Mars_Rover_Comms.TCP.ZClient(IPAddress, Convert.ToInt32(Port));
            client.PacketReceived += new EventHandler<DataArgs>(client_PacketReceived);

            try
            {

                comms = RCUComms.Instance; //Responsible for sending states back to OCU

                #region Maestro
                if (useMaestro)
                {
                    Logger.WriteLine("Creating Maestro");
                    _Maestro = new Maestro();
                    if (!_Maestro.isConnected())
                    {
                        useMaestro = false;
                    }
                }
                #endregion

                #region Sensors
                if (useSensors)
                {
                    Logger.WriteLine("Creating Sensors");
                    _Sensors = new Sensors();

                    try
                    {
                        if (_Sensors.OpenConnection(SensorCOM))
                        {
                            Logger.WriteLine("Sensors successfully created.");
                            arduinoReady = true;
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine("Error: " + ex.Message);
                        Logger.WriteLine("Sensors not created.");
                        arduinoReady = false;
                        useSensors = false;
                    }
                }
                sensorData = new string[6];
                #endregion

                #region PID
                if (usePID)
                {
                    Logger.WriteLine("Creating PID.");
                    _PID = new PID();
                    Logger.WriteLine("Pid Successfully Created");
                }
                #endregion

                #region Drive Controller
                if (useArduino)
                {
                    Logger.WriteLine("Creating Drive Controller.");
                    _DriveController = new DriveController();
                    try
                    {
                        if (_DriveController.OpenConnection(DriveCOM))
                        {
                            Logger.WriteLine("Drive Controller successfully created.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine("Error: " + ex.Message);
                        Logger.WriteLine("Drive Controller not created.");
                        useArduino = false;
                    }
                }
                #endregion

                #region Servos
                if (useServos)
                {
                    Logger.WriteLine("Creating Servo Controller.");
                    _ServoController = new ServoController();
                    try
                    {
                        if (_ServoController.OpenConnection(ServoCOM))
                        {
                            Logger.WriteLine("Servo Controller successfully created.");
                        }
                    }
                    catch (Exception ex)
                    {
                        Logger.WriteLine("Error: " + ex.Message);
                        Logger.WriteLine("Servo Controller not created.");
                        useServos = false;
                    }
                }
                #endregion

                if (debug)
                {
                    Logger.WriteLine("Maestro: " + useMaestro);
                    Logger.WriteLine("Sensors: " + useSensors);
                    Logger.WriteLine("Drive Controller: " + useArduino);
                    Logger.WriteLine("Servo COntroller: " + useServos);
                }
                
                //packet handler - runs in its own thread
                stateProcessor = new Thread(new ThreadStart(StateProcessorDoWork));
                stateProcessor.Name = "State Processor";
                stateProcessor.Start();
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Error during startup: " + ex.Message);
                //return;
            }

            while (true)
            {
                Logger.WriteLine("Enter exit to shutdown");
                string input = Console.ReadLine().ToLower();
                if (input.Contains("exit"))
                {
                    break;
                }
            }

            //Closing Remarks
            client.PacketReceived -= client_PacketReceived;
            client.Close();
            tokenSource.Cancel();
            if (stateProcessor != null)
                stateProcessor.Join();

           if (useMaestro && _Maestro != null)
            {
                _Maestro.TryToDisconnect();
            }

        } //End Main

        // handler for commands received from the ocu
        static void client_PacketReceived(object sender, DataArgs e)
        {
            if (client.IsConnected())
            {
                //Not LOS
            }
            else
            {
                //LOS
                _ServoController.setLOS(true);
                //_Maestro.setLOS(true);
                _DriveController.stopMotors();
            }
            try
            {
                using (MemoryStream ms = new MemoryStream(e.Data))
                {
                    //Logger.WriteLine("Packet Received");
                    // robot drive state received - enqueue the state so it is processed in the StateProcessorDoWork() below
                    RobotState state = (RobotState)robotStateDeserializer.Deserialize(ms);
                    stateQueue.Enqueue(state);
                }
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Error deserializing state: " + ex.Message);
            }
        }

        static void StateProcessorDoWork()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    Mars_Rover_Comms.RobotState robotState = stateQueue.Dequeue(tokenSource.Token);

                    if (client.IsConnected())
                    { 

                        if (robotState.DriveState != null)
                        {
                            if (debug)
                            {
                                // Debug Statements for each of the drivestates
                                Logger.WriteLine("Robot Drive Mode: " + robotState.DriveState.Mode);
                                Logger.WriteLine("Robot RightSpeed: " + robotState.DriveState.RightSpeed);
                                Logger.WriteLine("Robot LeftSpeed: " + robotState.DriveState.LeftSpeed);
                                Logger.WriteLine("Robot Radius: " + robotState.DriveState.radius);
                                Logger.WriteLine("Robot Arm State: " + robotState.DriveState.ArmState);
                                Logger.WriteLine("Robot Gripper Pos: " + robotState.DriveState.gripperPos);
                                Logger.WriteLine("Shoulder POS: " + robotState.DriveState.shoulderPos);
                                Logger.WriteLine("Elbow POS: " + robotState.DriveState.elbowPos);
                                Logger.WriteLine("Wrist POS: " + robotState.DriveState.wristPos);
                                Logger.WriteLine("Go to Home: " + robotState.DriveState.goToHome);
                                Logger.WriteLine("Go to Sample: " + robotState.DriveState.goToSample);
                                Logger.WriteLine("Go to Deposit: " + robotState.DriveState.goToDeposit);
                                Logger.WriteLine("Robot Headlight: " + robotState.DriveState.Headlights);
                                Logger.WriteLine("Robot Use Pid: " + robotState.DriveState.usePID);
                                Logger.WriteLine("AutoBrake: " + robotState.DriveState.AutoStop);
                                Logger.WriteLine("Controller State: " + robotState.DriveState.controllerControl);
                                Logger.WriteLine("Control State: " + robotState.DriveState.Control);
                            }

                            if (!robotState.DriveState.Control) //Connected but no control
                            {
                                if (useServos)
                                {
                                    _ServoController.setArmServos(shoulderPos, elbowPos, wristPos);
                                    _ServoController.noControl();
                                }
                                if (useArduino)
                                {
                                    _DriveController.stopMotors();
                                }
                                if (useMaestro)
                                {
                                    _Maestro.setArmServos(shoulderPos, elbowPos, wristPos);
                                    _Maestro.noControl();
                                }
                            }
                            else
                            {
                                if (useMaestro)
                                {
                                    // Set LOS to false
                                    _Maestro.setLOS(false);

                                    if (robotState.DriveState.goToHome)
                                    {
                                        shoulderPos = 464;
                                        elbowPos = 1000;
                                        wristPos = 2000;
                                        _Maestro.setArmServos(shoulderPos, elbowPos, wristPos);
                                    }
                                    else if (robotState.DriveState.goToSample)
                                    {
                                        shoulderPos = 2000;
                                        elbowPos = 600;
                                        wristPos = 1350;
                                        _Maestro.setArmServos(shoulderPos, elbowPos, wristPos);
                                    }
                                    else if (robotState.DriveState.goToDeposit)
                                    {
                                        shoulderPos = 2000;
                                        elbowPos = 1500;
                                        wristPos = 1400;
                                        _Maestro.setArmServos(shoulderPos, elbowPos, wristPos);
                                    }
                                    else
                                    {
                                        shoulderPos = robotState.DriveState.shoulderPos;
                                        elbowPos = robotState.DriveState.elbowPos;
                                        wristPos = robotState.DriveState.wristPos;
                                        _Maestro.setArmServos(shoulderPos, elbowPos, wristPos);
                                    }

                                    if (robotState.DriveState.gripperPos == closed)
                                    {
                                        gripperPos = closed;
                                        _Maestro.closeGripper();
                                    }
                                    else if (robotState.DriveState.gripperPos == open)
                                    {
                                        gripperPos = open;
                                        _Maestro.openGripper();
                                    }
                                }
                                if (useServos)
                                {
                                    // Set LOS to false
                                    _ServoController.setLOS(false);

                                    if (robotState.DriveState.goToHome)
                                    {
                                        shoulderPos = 464;
                                        elbowPos = 1000;
                                        wristPos = 2000;
                                        _ServoController.setArmServos(shoulderPos, elbowPos, wristPos);
                                    }
                                    else if (robotState.DriveState.goToSample)
                                    {
                                        shoulderPos = 2000;
                                        elbowPos = 600;
                                        wristPos = 1350;
                                        _ServoController.setArmServos(shoulderPos, elbowPos, wristPos);
                                    }
                                    else if (robotState.DriveState.goToDeposit)
                                    {
                                        shoulderPos = 2000;
                                        elbowPos = 1500;
                                        wristPos = 1400;
                                        _ServoController.setArmServos(shoulderPos, elbowPos, wristPos);
                                    }
                                    else
                                    {
                                        shoulderPos = robotState.DriveState.shoulderPos;
                                        elbowPos = robotState.DriveState.elbowPos;
                                        wristPos = robotState.DriveState.wristPos;
                                        _ServoController.setArmServos(shoulderPos, elbowPos, wristPos);
                                    }

                                    if (robotState.DriveState.gripperPos == closed)
                                    {
                                        gripperPos = closed;
                                        _ServoController.closeGripper();
                                    }
                                    else if (robotState.DriveState.gripperPos == open)
                                    {
                                        gripperPos = open;
                                        _ServoController.openGripper();
                                    }
                                }

                                if (useSensors)
                                {
                                    // Headlight Function
                                    if (robotState.DriveState.Headlights)
                                    {
                                        if (!_Sensors.headlightsEnabled())
                                        {
                                            _Sensors.enableHeadlights();
                                        }
                                    }
                                    else if (!robotState.DriveState.Headlights)
                                    {
                                        if (_Sensors.headlightsEnabled())
                                        {
                                            _Sensors.disableHeadlights();
                                        }
                                    }
                                    for (int i = 0; i < sensorData.Length; i++)
                                    {
                                        sensorData[i] = _Sensors.getData()[i];
                                    }
                                    if (int.Parse(sensorData[0]) < 128 && robotState.DriveState.AutoStop)
                                    {
                                        Program._DriveController.stopMotors();
                                        Program._DriveController.setMotors(1400, 1400);
                                        System.Threading.Thread.Sleep(100);
                                        Program._DriveController.stopMotors();
                                    }
                                }

                                if (usePID)
                                {
                                    if (robotState.DriveState.usePID == true)
                                    {
                                        if (!_PID.enabled)
                                        {
                                            _PID.enabled = true;
                                        }
                                    }
                                    else if (robotState.DriveState.usePID == false)
                                    {
                                        if (_PID.enabled)
                                        {
                                            _PID.enabled = false;
                                        }
                                    }
                                }

                                if (useArduino && useServos)
                                {
                                    //Decode Robot Mode
                                    Drive(robotState.DriveState.Mode, robotState.DriveState.radius, robotState.DriveState.LeftSpeed, robotState.DriveState.RightSpeed);
                                }
                            }
                        }
                    }
                    else
                    {
                        // LOS
                        if (useMaestro)
                        {
                            //_Maestro.setLOS(true);
                        }
                        if (useServos)
                        {
                            _ServoController.setLOS(true);
                        }
                        if (useArduino)
                        {
                            _DriveController.stopMotors();
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Logger.WriteLine("StateProcessor: " + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("StateProcessor: unhandled exception: " + ex.Message);
                }
            }
            Logger.WriteLine("StateProcessor exiting...");


        }

        private static void Drive(short driveMode, double radius, short leftSpeed, short rightSpeed)
        {
            if (driveMode == normal)
            {
                Turn(radius);
            }
            else if (driveMode == rotate)
            {
                _ServoController.setRotateMode();
                //_Maestro.setRotateMode();
            }
            else if (driveMode == translate)
            {
                _ServoController.setTranslateMode();
                //_Maestro.setTranslateMode();
            }
            else if (driveMode == tank)
            {
                _ServoController.setTankMode();
                //_Maestro.setTankMode();
            }
            _DriveController.setMotors(leftSpeed, rightSpeed);
        }

        public static void Turn(double radius)
        {
            int offset = 220;
            short turn = (short)Math.Round(radius * offset);
            _ServoController.setTurningServos((short)(1441 + turn), (short)(1520 + turn), (short)(1510 - turn), (short)(1425 - turn));
            //_Maestro.setTurningServos((short)(1441 + turn), (short)(1520 + turn), (short)(1510 - turn), (short)(1425 - turn));
        }
    }
}
