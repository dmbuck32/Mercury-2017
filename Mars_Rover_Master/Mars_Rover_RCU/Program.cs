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
        private static readonly short shoulder = 0;
        private static readonly short elbow = 1;
        private static readonly short wrist = 2;

        private static RCUComms comms;

        static bool debug = true;

        static ConfigureRCU rcuConfig; //Responsible for sending states back to OCU
        static StreamWriter log;

        static public Controllers.Maestro _Maestro;

        //Sensors
        static Controllers.Sensors _Sensors;
        static public String[] sensorData;

        static Controllers.PID _PID;

        static Utility.UpdateQueue<RobotState> stateQueue = new Utility.UpdateQueue<RobotState>(-1);
        static XmlSerializer robotStateDeserializer = new XmlSerializer(typeof(Mars_Rover_Comms.RobotState));

        //tcp/ip client for communicating with the ocu
        static public Mars_Rover_Comms.TCP.ZClient client;

        static Boolean useMaestro = true;
        //static public bool connected;

        static Thread stateProcessor;
        static CancellationTokenSource tokenSource = new CancellationTokenSource();

        //static Mars_Rover_RCU.Kinematics.Kinematics kinematics;

        static DateTime APMconnectTime = DateTime.Now;

        static public String IPAddress;
        static public String Port;
        static public String COM;

        public static void Main(string[] args)
        {
            System.IO.StreamReader file = new System.IO.StreamReader("..\\..\\IP_Port.txt");
            IPAddress = file.ReadLine();
            Port = file.ReadLine();
            COM = file.ReadLine();
            //setup primary comms
            client = new Mars_Rover_Comms.TCP.ZClient(IPAddress, Convert.ToInt32(Port));
            client.PacketReceived += new EventHandler<DataArgs>(client_PacketReceived);

            try
            {

                comms = RCUComms.Instance; //Responsible for sending states back to OCU

                #region Maestro
                Logger.WriteLine("Creating Maestro");
                _Maestro = new Maestro();
                #endregion

                //Tests
                //_Maestro.pauseClaw();
                //_Maestro.resetTrigger();

                #region Sensors
                Logger.WriteLine("Creating Sensors");
                _Sensors = new Sensors();
                
                if (!_Sensors.OpenConnection(COM))
                {
                    Logger.WriteLine("Attempting to connect to Arduino.");
                }
                
                sensorData = new string[6];
                #endregion

                #region PID
                _PID = new PID();
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

                        // Set LOS to false
                        _Maestro.setLOS(false);

                        if (robotState.DriveState != null)
                        {
                            Logger.WriteLine("Robot Drive Mode: " + robotState.DriveState.Mode);
                            Logger.WriteLine("Robot Arm State: " + robotState.DriveState.ArmState);
                            Logger.WriteLine("Robot Gripper Pos: " + robotState.DriveState.gripperPos);
                            Logger.WriteLine("Robot Headlight: " + robotState.DriveState.Headlights);
                            Logger.WriteLine("Robot RightSpeed: " + robotState.DriveState.RightSpeed);
                            Logger.WriteLine("Robot LeftSpeed: " + robotState.DriveState.LeftSpeed);
                            Logger.WriteLine("Robot Use Pid: " + robotState.DriveState.usePID);
                            
                            // Headlight Function
                            if (robotState.DriveState.Headlights == true)
                            {
                                if (!_Sensors.headlightsEnabled())
                                {
                                    _Sensors.enableHeadlights();
                                }
                            }
                            else if (robotState.DriveState.Headlights == false)
                            {
                                if (_Sensors.headlightsEnabled())
                                {
                                    _Sensors.disableHeadlights();
                                }
                            }

                            if (robotState.DriveState.usePID == true)
                            {
                                if (!_PID.isEnabled())
                                {
                                    _PID.enable();
                                }
                            }
                            else if (robotState.DriveState.usePID == false)
                            {
                                if (_PID.isEnabled())
                                {
                                    _PID.disable();
                                }
                            }
                            
                            //Decode Robot Mode
                            if (robotState.DriveState.Mode == normal)
                            {
                               short temp = robotState.DriveState.LeftSpeed;
                                // TODO: Implement turning based on radius
                                _Maestro.setDriveServos(robotState.DriveState.LeftSpeed, robotState.DriveState.RightSpeed);
                            }
                            else if (robotState.DriveState.Mode == rotate)
                            {
                                _Maestro.setRotateMode();
                                _Maestro.setDriveServos(robotState.DriveState.LeftSpeed, robotState.DriveState.RightSpeed);
                            }
                            else if (robotState.DriveState.Mode == translate)
                            {
                                _Maestro.setTranslateMode();
                                _Maestro.setDriveServos(robotState.DriveState.LeftSpeed, robotState.DriveState.RightSpeed);
                                
                            }
                            else if (robotState.DriveState.Mode == tank)
                            {
                                _Maestro.setTankMode();
                                _Maestro.setDriveServos(robotState.DriveState.LeftSpeed, robotState.DriveState.RightSpeed);
                                
                            }
                            

                            /*
                            if (robotState.DriveState.FrontStopArmUp == true && _Roomba.getAutobrake() == false)
                            {
                                _Roomba.setAutobrake(true);
                                Logger.WriteLine("Autobrake is " + _Roomba.getAutobrake());
                                
                            }
                            else if (robotState.DriveState.FrontStopArmUp == false && _Roomba.getAutobrake() == true)
                            {
                                _Roomba.setAutobrake(false);
                                Logger.WriteLine("Autobrake is " + _Roomba.getAutobrake());
                                
                            }
                            if (robotState.DriveState.Radius >= 0 && robotState.DriveState.Radius <= 7)
                            {
                                _Roomba.drive(robotState.DriveState.Radius, robotState.DriveState.Speed);
                            }
                            if (robotState.DriveState.LeftSpeed != 0 && robotState.DriveState.RightSpeed != 0 && robotState.DriveState.controllerControl)
                            {
                                _Roomba.directDrive(robotState.DriveState.LeftSpeed, robotState.DriveState.RightSpeed);
                            }
                            if (robotState.DriveState.ArmSpeed == 0 || robotState.DriveState.ArmSpeed == 3 || robotState.DriveState.ArmSpeed == -1)
                            {
                                _MiniMaestro.moveElbow(robotState.DriveState.ArmSpeed);
                                _MiniMaestro.moveShoulder(robotState.DriveState.ArmSpeed);
                            }
                            if (robotState.DriveState.ScoopIn == true)
                            {
                                _MiniMaestro.moveClaw(0);
                            }
                            if (robotState.DriveState.ScoopOut == true)
                            {
                                _MiniMaestro.moveClaw(3);
                                System.Threading.Thread.Sleep(1500);
                                _MiniMaestro.pauseClaw();
                            }
                            */
                        }
                    }
                    else
                    {
                        _Maestro.setLOS(true);
                        _Maestro.setDriveServos(STOP, STOP);
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

    }
}
