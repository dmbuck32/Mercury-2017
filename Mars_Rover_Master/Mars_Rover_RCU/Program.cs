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
    //Main Program Entry PoC:\Users\Jason\Dropbox\My Documents\WVU\Robotics_2014\Software\Mars_Rover_Master\Mars_Rover_RCU\Program.csint
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

        static bool debug = false;

        static bool arduinoReady = false;

        static ConfigureRCU rcuConfig; //Responsible for sending states back to OCU
        static StreamWriter log;

        static public Controllers.Maestro _Maestro;
        static public Controllers.Arduino DriveController;

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

        static private bool useMaestro = false;
        static private bool useSensors = false;
        static private bool usePID = false;
        static private bool useArduino = true;

        public static void Main(string[] args)
        {
            
            System.IO.StreamReader file = new System.IO.StreamReader("..\\..\\IP_Port.txt");
            IPAddress = file.ReadLine();
            Port = file.ReadLine();
            SensorCOM = file.ReadLine();
            DriveCOM = file.ReadLine();
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

                #region Arduino
                if (useArduino)
                {
                    Logger.WriteLine("Creating Drive Controller.");
                    //DriveController = new Arduino(DriveCOM);
                    DriveController = new Arduino("COM14");
                    DriveController.digitalWrite(1, Arduino.LOW);
                    Logger.WriteLine("Drive Controller Created.");
                }
                #endregion


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
                //Logger.WriteLine("LOS");
                _Maestro.setLOS(true);
                DriveInterface(STOP, STOP);
                //_Maestro.setDriveServos(STOP, STOP);
                //_Maestro.setArmServos(shoulderPos, elbowPos, wristPos);
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
                                Logger.WriteLine("Robot Headlight: " + robotState.DriveState.Headlights);
                                Logger.WriteLine("Robot RightSpeed: " + robotState.DriveState.RightSpeed);
                                Logger.WriteLine("Robot LeftSpeed: " + robotState.DriveState.LeftSpeed);
                                Logger.WriteLine("Robot Use Pid: " + robotState.DriveState.usePID);
                                Logger.WriteLine("Robot Radius: " + robotState.DriveState.radius);
                                Logger.WriteLine("Control State: " + robotState.DriveState.Control);
                                Logger.WriteLine("Controller State: " + robotState.DriveState.controllerControl);
                                Logger.WriteLine("Robot Arm State: " + robotState.DriveState.ArmState);
                                Logger.WriteLine("Robot Gripper Pos: " + robotState.DriveState.gripperPos);
                                Logger.WriteLine("Shoulder POS: " + robotState.DriveState.shoulderPos);
                                Logger.WriteLine("Elbow POS: " + robotState.DriveState.elbowPos);
                                Logger.WriteLine("Wrist POS: " + robotState.DriveState.wristPos);
                                Logger.WriteLine("Go to Home: " + robotState.DriveState.goToHome);
                                Logger.WriteLine("Go to Sample: " + robotState.DriveState.goToSample);
                                Logger.WriteLine("Go to Deposit: " + robotState.DriveState.goToDeposit);
                            }

                            if (!robotState.DriveState.Control) //Connected but no control
                            {
                                _Maestro.setArmServos(shoulderPos, elbowPos, wristPos);
                                _Maestro.setDriveServos(STOP, STOP);
                                _Maestro.noControl();
                            }
                            else
                            {
                                if (useMaestro) {
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
                                
                                if (useSensors)
                                {
                                    // Headlight Function
                                    if (robotState.DriveState.Headlights == true && arduinoReady)
                                    {
                                        if (!_Sensors.headlightsEnabled())
                                        {
                                            _Sensors.enableHeadlights();
                                        }
                                    }
                                    else if (robotState.DriveState.Headlights == false & arduinoReady)
                                    {
                                        if (_Sensors.headlightsEnabled())
                                        {
                                            _Sensors.disableHeadlights();
                                        }
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

                                if (useArduino && useMaestro)
                                {
                                    //Decode Robot Mode
                                    Drive(robotState.DriveState.Mode, robotState.DriveState.radius, robotState.DriveState.LeftSpeed, robotState.DriveState.RightSpeed);
                                }

                                if (useArduino)
                                {
                                    DriveInterface(robotState.DriveState.LeftSpeed, robotState.DriveState.RightSpeed);
                                    DriveController.digitalWrite(13, Arduino.HIGH);
                                    System.Threading.Thread.Sleep(100);
                                    DriveController.digitalWrite(13, Arduino.LOW);
                                }
                    }
                        }
                    }
                    else
                    {
                        _Maestro.setLOS(true);
                        DriveInterface(STOP, STOP);
                        //_Maestro.setArmServos(shoulderPos, elbowPos, wristPos);
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
                DriveInterface(leftSpeed, rightSpeed);
            }
            else if (driveMode == rotate)
            {
                _Maestro.setRotateMode();
                DriveInterface(leftSpeed, (short)(-rightSpeed));
            }
            else if (driveMode == translate)
            {
                _Maestro.setTranslateMode();
                DriveInterface(leftSpeed, (short)(-rightSpeed));

            }
            else if (driveMode == tank)
            {
                _Maestro.setTankMode();
                DriveInterface(leftSpeed, rightSpeed);

            }
        }

        public static void Turn(double radius)
        {
            int offset = 220;
            short turn = (short)Math.Round(radius * offset);
            _Maestro.setTurningServos((short)(1441 - turn), (short)(1520 - turn), (short)(1510 + turn), (short)(1425 + turn));
        }

        private static void DriveInterface(short leftSpeed, short rightSpeed)
        {
            int leftDirPin = 3;
            int rightDirPin = 4;
            int leftPWMPin = 5;
            int rightPWMPin = 6;
            byte leftDir = 0;
            byte rightDir = 0;
            int leftPWM = 0;
            int rightPWM = 0;

            if (leftSpeed > 1500)
            {
                leftPWM = (int)Map(leftSpeed, 1500, 2000, 0, 255);
                leftDir = 1;
            } else if (leftSpeed < 1500)
            {
                leftPWM = (int)Map(leftSpeed, 1500, 1000, 0, 255);
                leftDir = 0;
            }

            if (rightSpeed > 1500)
            {
                rightPWM = (int)Map(rightSpeed, 1500, 2000, 0, 255);
                rightDir = 1;
            }
            else if (leftSpeed < 1500)
            {
                rightPWM = (int)Map(rightSpeed, 1500, 1000, 0, 255);
                rightDir = 0;
            }
            Logger.WriteLine("" + leftPWM);
            Logger.WriteLine("" + leftDir);
            Logger.WriteLine("" + rightPWM);
            Logger.WriteLine("" + rightDir);
            DriveController.digitalWrite(leftDirPin, leftDir);
            DriveController.digitalWrite(rightDirPin, rightDir);
            DriveController.analogWrite(leftPWMPin, leftPWM);
            DriveController.analogWrite(rightPWMPin, rightPWM);
        }

        public static decimal Map(decimal value, decimal fromSource, decimal toSource, decimal fromTarget, decimal toTarget)
        {
            return (value - fromSource) / (toSource - fromSource) * (toTarget - fromTarget) + fromTarget;
        }

    }
}
