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
        private static RCUComms comms;

        static bool debug = true;

        static ConfigureRCU rcuConfig; //Responsible for sending states back to OCU
        static StreamWriter log;

        static public Controllers.Maestro _Maestro;

        //Sensors
        static Controllers.Sensors _Sensors;
        static public String[,] sensorData;

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

        public static void Main(string[] args)
        { 
            string desktop = Environment.GetFolderPath(Environment.SpecialFolder.Desktop);
            System.IO.StreamReader file = new System.IO.StreamReader(desktop + "\\Mercury-2017\\IP_Port.txt");
            IPAddress = file.ReadLine();
            Port = file.ReadLine();
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

                //Tests
                //_Maestro.pauseClaw();
                //_Maestro.resetTrigger();

                #region Sensors
                _Sensors = new Sensors();
                sensorData = new string[4, 2];
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
                    Logger.WriteLine("Packet Receieved");
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
                            if (robotState.DriveState.Headlights == true)
                            {
                                _Roomba.powerHeadlights(1);
                            }
                            else if (robotState.DriveState.Headlights == false)
                            {
                                _Roomba.powerHeadlights(0);
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
                            if (robotState.DriveState.WallFollow == true)
                            {
                                _MiniMaestro.launch();//F1
                            }
                            if (robotState.DriveState.FrontStopArmDown == true)
                            {
                                _MiniMaestro.resetLaunch();//F2
                            }
                            */
                        }
                    }
                    else
                    {
                        //_Roomba.LOS();

                        if (useMaestro)
                        {
                            /*Dictionary<Devices, int> driveState = kinematics.GetWheelStates(2047, 0, 0, false, false, false, false, false);
                            driveState[Devices.ControlSignal] = 0;
                            _Maestro.EnqueueState(driveState);*/
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

    }
}
