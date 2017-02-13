using System;
using System.Diagnostics;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using Mars_Rover_Configuration;
using Mars_Rover_Comms;
using System.Xml.Serialization;
using Mars_Rover_RCU;
using Mars_Rover_RCU.Controllers;
using Pololu.Jrk;
using Pololu.UsbWrapper;
using System.Runtime.InteropServices;


namespace Mars_Rover_RCU
{
    //Main Program Entry Point
    class Program
    {
        //Used to send console output back to server
        private delegate void DataRead(string data);
        private static event DataRead OnDataRead;
        private static String myLog; 

        static bool debug = true;

        static ConfigureRCU rcuConfig;

        static Controllers.APMInterface _APM;
        static Controllers.Maestro _Maestro;
        static List<Jrk> _JRKList;

        static Utility.UpdateQueue<RobotState> stateQueue = new Utility.UpdateQueue<RobotState>(-1);
        static XmlSerializer robotStateDeserializer = new XmlSerializer(typeof(Mars_Rover_Comms.RobotState));

        //tcp/ip client for communicating with the ocu
        static Mars_Rover_Comms.TCP.ZClient client;

        static Boolean useRoboTeq = true;
        static Boolean useMaestro = true;
        static Boolean useAPM = true;
        static Boolean useARM = true;
        static Boolean useTurn = true;
        static Boolean useJRK = true;

        static Thread stateProcessor;
        static CancellationTokenSource tokenSource = new CancellationTokenSource();

        static Mars_Rover_RCU.Kinematics.Kinematics kinematics;

        static void Main(string[] args)
        {
            Thread readingThread = new Thread(Read);       
            readingThread.Start();

            foreach (string str in args)
            {
                char[] separator = { '=' };
                string[] arg_pair = str.Split(separator, 2, StringSplitOptions.RemoveEmptyEntries);

                if (str == "--no_roboteq")
                {
                    Console.WriteLine("No Roboteq Specified!");
                    useRoboTeq = false;
                }
                else if (str == "--no_maestro")
                {
                    Console.WriteLine("No Maestro Specified!");
                    useMaestro = false;
                }
                else if (str == "--no_apm")
                {
                    Console.WriteLine("No APM Specified!");
                    useAPM = false;
                }
                else if (str == "--no_arm")
                {
                    Console.WriteLine("No ARM Specified!");
                    useARM = false;
                }
                else if (str == "--no_turn")
                {
                    Console.WriteLine("No Turn Specified!");
                    useTurn = false;
                }
                else if (str == "--no_jrk")
                {
                    Console.WriteLine("No Jrk Specified!");
                    useJRK = false;
                }
                else
                {
                    // handle the config file option
                    switch (arg_pair[0])
                    {
                        case "--config":
                            //load file...
                            XmlSerializer deserializer = new System.Xml.Serialization.XmlSerializer(typeof(ConfigureRCU));
                            try
                            {
                                using (StreamReader file_reader = new StreamReader(arg_pair[1]))
                                {
                                    rcuConfig = (ConfigureRCU)deserializer.Deserialize(file_reader);
                                }
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine("Error opening configuration file: " + ex.Message);
                            }
                            break;
                        default:
                            break;
                    }
                }

            }

            try
            {
                if (rcuConfig == null)
                {
                    Console.WriteLine("Usage: mars.rcu.exe --config=filename [--no_roboteq] [--no_apm] [--no_maestro]");
                    throw new Exception("No configuration specified");
                }

                if (useAPM)
                {
                    _APM = new Controllers.APMInterface(rcuConfig.APMConfig);
                    _APM.Activate(true);
                }

                if (useJRK)
                {
                    List<DeviceListItem> scan = Jrk.getConnectedDevices();
                    if (scan.Count > 0)
                    {
                        foreach (DeviceListItem device in scan)
                        {
                            Jrk mydevice = new Jrk(device);
                            Console.WriteLine("Jrk #SN " + device.serialNumber + " connected!");
                        }

                    }
                    else
                    {
                        throw new Exception("No Jrks Found!!");
                    }

                }

                if (useMaestro)
                {
                    _Maestro = new Controllers.Maestro(rcuConfig.MaestroConfig, useARM, useTurn);

                    if (useTurn)
                    {
                        _Maestro.activateTurning(true); //Method has optional parameter target:"1500"    
                    }
                }

                //kinematics for drive system
                kinematics = new Kinematics.Kinematics(1000);

                //setup primary comms
                client = new Mars_Rover_Comms.TCP.ZClient(rcuConfig.WVUServerIP, rcuConfig.ListeningPort);
                client.PacketReceived += new EventHandler<DataArgs>(client_PacketReceived);

                //packet handler - runs in its own thread
                stateProcessor = new Thread(new ThreadStart(StateProcessorDoWork));
                stateProcessor.Name = "State Processor";

                stateProcessor.Start();
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error during startup: " + ex.Message);
                //return;
            }

            while (true)
            {
                Console.WriteLine("Enter exit to shutdown");
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
            stateProcessor.Join();


            if (useJRK && _JRKList.Count > 0)
                foreach (Jrk device in _JRKList)
                    device.disconnect();

            if (useMaestro && _Maestro != null)
                _Maestro.Deactivate();

            //if (useRoboTeq)
            //{
            //foreach (var controller in driveControllers)
            //controller.Deactivate();
            //}

        } //End Main

        // handler for commands received from the ocu
        static void client_PacketReceived(object sender, DataArgs e)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(e.Data))
                {
                    // robot drive state received - enqueue the state so it is processed in the StateProcessorDoWork() below
                    RobotState state = (RobotState)robotStateDeserializer.Deserialize(ms);
                    stateQueue.Enqueue(state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing state: " + ex.Message);
            }
        }

        static void StateProcessorDoWork()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    Mars_Rover_Comms.RobotState robotState = stateQueue.Dequeue(tokenSource.Token);

                    // APM Controller
                    if ((robotState.CmdLineState != null))
                    {
                        if (robotState.CmdLineState.CmdLine.Equals("welcome houston"))
                        {
                            Console.WriteLine("bazingaaa!");
                        }

                        //if (debug)
                        Console.WriteLine(robotState.CmdLineState.CmdLine.ToString());
                    }

                    if ((robotState.DriveState != null) && (useMaestro))
                    {
                        Dictionary<Devices, int> driveState = kinematics.GetWheelStates(robotState.DriveState.Radius, robotState.DriveState.Speed);
                        _Maestro.EnqueueState(driveState);
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine("StateProcessor: " + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("StateProcessor: unhandled exception: " + ex.Message);
                }
            }
            Console.WriteLine("StateProcessor exiting...");

        }

        #region Console Sync

        private static void Read(object parameter)
        {
            //Process process = parameter as Process;
           Process process = Process.GetCurrentProcess();
           myLog = process.StandardOutput.ReadToEnd();
        }
        #endregion


    }
}
