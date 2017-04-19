using System;
using System.ComponentModel;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Timers;
using System.Threading.Tasks;
using System.Threading;
using System.Xml.Serialization;
using Mars_Rover_Comms;
using Mars_Rover_Comms.TCP;
using Mars_Rover_OCU;
using Mars_Rover_OCU.Utilities;
using System.Windows.Threading;
using System.Windows.Input;
using System.IO;

namespace Mars_Rover_OCU.Comms
{

    public sealed class OCUComms : INotifyPropertyChanged
    {

        private sealed class KeyboardDrive
        {
            private static KeyboardDrive _keyboardInstance;
            private static readonly object _keyboardSync = new object();
            private DriveState _driveState;

            public static KeyboardDrive KeyboardInstance
            {
                get
                {
                    if (_keyboardInstance == null)
                    {
                        lock (_keyboardSync)
                        {
                            if (_keyboardInstance == null)
                                _keyboardInstance = new KeyboardDrive();
                        }
                    }

                    return _keyboardInstance;
                }
            }

            private KeyboardDrive()
            {
                lock (_keyboardSync)
                {
                    _driveState = new DriveState();
                    
                    /*
                    _driveState.Radius = 2047;
                    _driveState.Speed = 0;
                    _driveState.ArmSpeed = 0;
                    _driveState.ScoopIn = false;
                    _driveState.ScoopOut = false;
                    _driveState.FrontStopArmDown = false;
                    _driveState.FrontStopArmUp = false;
                    _driveState.Headlights = false;
                    _driveState.WallFollow = false;
                    */
                }
            }

            public void setDriveState(short speed, short radius, short armSpeed, bool scoopIn, bool scoopOut)
            {
                lock (_keyboardSync)
                {
                    /*
                    _driveState.Speed = speed;
                    _driveState.Radius = radius;
                    _driveState.ArmSpeed = armSpeed;
                    _driveState.ScoopIn = scoopIn;//Left Pressed or J
                    _driveState.ScoopOut = scoopOut;//Right Pressed or L
                    */
                }
            }

            public DriveState getDriveState()
            {
                return _driveState;
            }
        }

        private static OCUComms instance;
        private KeyboardDrive keyboardDrive;

        private static readonly object instanceSync = new object();

        //Mercury
        private bool W_Pressed = false;
        private bool A_Pressed = false;
        private bool S_Pressed = false;
        private bool D_Pressed = false;
        private bool Up_Pressed = false;
        private bool Down_Pressed = false;
        private bool Left_Pressed = false;
        private bool Right_Pressed = false;
        private bool F1_Pressed = false;
        private bool F2_Pressed = false;
        private bool F3_Pressed = false;
        private bool F4_Pressed = false;
        private bool F5_Pressed = false;
        private bool F6_Pressed = false;
        private bool Q_Pressed = false;
        private bool E_Pressed = false;
        //End Mercury

        private bool isEnabled;
        private bool _isControllerEnabled;
        private bool _isKeyboardEnabled;
        private Boolean _isClientConnected;

        private ushort _driveMethod = 0; //1 for xbox, 2 for keyboard TODO: Create enum
        private String _lat = "null";
        private String _lng = "null";
        private String _heading = "null";

        private static string LeftSensor; // global sensors-- info from rcu
        private static string RightSensor;
        private static string FrontSensor;

        private static short shoulderPos;
        private static short elbowPos;
        private static short wristPos;
        private static short gripperPos;

        private static short shoulderOCU;
        private static short elbowOCU;
        private static short wristOCU;

        private static short DriveMode;
        private static short ArmMode;

        private bool headlights = false;
        private bool usePID = false;

        private ZServer server;
        private XmlSerializer serializer;
        private static XmlSerializer returnStateDeserializer = new XmlSerializer(typeof(Mars_Rover_Comms.RobotReturnState));

        static Utility.UpdateQueue<RobotReturnState> stateQueue;

        private String _Log = "";

        private System.Timers.Timer worker;
        private System.Timers.Timer updateLog;
        static Thread stateProcessor;
        static CancellationTokenSource tokenSource = new CancellationTokenSource();

        private readonly object workerSync = new object();
        private readonly object stateSync = new object();


        private CmdLineState previousCmd = new CmdLineState() { CmdLine = "" };


        //“The factory pattern is used to replace class constructors, abstracting the process of object generation 
        //so that the type of the object instantiated can be determined at run-time.” Factory method is just like 
        //regular method but when we are talking about patterns it just returns the instance of a class at run-time. 
        public static OCUComms Instance
        {
            get
            {
                if (instance == null)
                {
                    lock (instanceSync)
                    {
                        if (instance == null)
                            instance = new OCUComms();
                    }
                }

                return instance;
            }
        }

        private OCUComms()
        {
            isEnabled = false;
            _isControllerEnabled = false;
            _isKeyboardEnabled = false;
            _isClientConnected = false;


            serializer = new System.Xml.Serialization.XmlSerializer(typeof(RobotState));
            stateQueue = new Utility.UpdateQueue<RobotReturnState>(-1);

            worker = new System.Timers.Timer();
            updateLog = new System.Timers.Timer();

            //packet handler - runs in its own thread
            stateProcessor = new Thread(new ThreadStart(StateProcessorDoWork));
            stateProcessor.Name = "State Processor";

            updateLog.Elapsed += new ElapsedEventHandler(updateLog_Elapsed);
            worker.Elapsed += new ElapsedEventHandler(worker_Elapsed);

            Properties.Settings.Default.PropertyChanged += new System.ComponentModel.PropertyChangedEventHandler(Default_PropertyChanged);

            
        }


        #region Properties

        public ushort DriveMethod
        {
            get { return _driveMethod; }
            private set
            {
                _driveMethod = value;

            }
        }

        public String RobotLog
        {
            get
            {
                return _Log;

            }
            private set
            {
                if (!value.Equals(_Log))
                {
                    _Log = value;
                    OnPropertyChanged("RobotLog");
                }

            }
        }

        public bool IsControllerEnabled
        {
            get { return _isControllerEnabled; }
            private set
            {
                _isControllerEnabled = value;
            }
        }

        public bool IsKeyboardEnabled
        {
            get { return _isKeyboardEnabled; }
            private set
            {
                _isKeyboardEnabled = value;
            }
        }

        //This Property is tied to the OCUComms Class. It is updated
        //real time if the ZServer client connected property changes.
        public Boolean IsClientConnected
        {
            get { return _isClientConnected; }
            private set
            {
                if (value != _isClientConnected)
                {
                    _isClientConnected = value;
                    OnPropertyChanged("IsClientConnected");
                }
            }
        }

        #endregion

        #region Property Changed Events

        public event PropertyChangedEventHandler PropertyChanged;

        void OnPropertyChanged(PropertyChangedEventArgs e)
        {
            PropertyChangedEventHandler handler = PropertyChanged;
            if (handler != null)
                handler(this, e);
        }

        void OnPropertyChanged(string propertyName)
        {
            OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        void server_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            this.IsClientConnected = server.IsClientConnected;
        }


        #endregion

        #region Default Properties

        public bool IsEnabled
        {
            get { return isEnabled; }
            private set
            {
                if (value != isEnabled)
                {
                    isEnabled = value;
                    OnEnabledChanged();
                }
            }
        }

        #endregion

        #region Default Propery Changed Events

        void Default_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("OutputInterval"))
            {
                worker.Interval = Properties.Settings.Default.OutputInterval;
            }
        }

        public event EventHandler EnabledChanged;

        private void OnEnabledChanged()
        {
            if (EnabledChanged != null)
                EnabledChanged(this, new EventArgs());
        }

        #endregion

        #region Timers

        void updateLog_Elapsed(object sender, ElapsedEventArgs e)
        {

            if (!_Log.Equals(server.getLog()))
                RobotLog = server.getLog();

        }

        void worker_Elapsed(object sender, ElapsedEventArgs e)
        {

            if (System.Threading.Monitor.TryEnter(workerSync))
            {
                try
                {
                    RobotState outputState = new RobotState();

                    if (_driveMethod == 1) //Controller
                    {

                        try
                        {
                            DriveController.setFunctions(F1_Pressed, F2_Pressed, F3_Pressed);
                            if (F1_Pressed || F2_Pressed || F3_Pressed)
                            {
                                DriveController.updateArm(shoulderPos, elbowPos, wristPos);
                            }
                            outputState.DriveState = DriveController.getDriveState();
                            headlights = outputState.DriveState.Headlights || F4_Pressed;
                            usePID = outputState.DriveState.usePID || F5_Pressed;
                            outputState.DriveState.Headlights = headlights;
                            outputState.DriveState.usePID = usePID;
                            outputState.DriveState.Control = true;
                            outputState.DriveState.controllerControl = true;
                            DriveMode = outputState.DriveState.Mode;
                            ArmMode = outputState.DriveState.ArmState;
                            shoulderOCU = outputState.DriveState.shoulderPos;
                            elbowOCU = outputState.DriveState.elbowPos;
                            wristOCU = outputState.DriveState.wristPos;
                            outputState.DriveState.AutoStop = F6_Pressed;
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine("Controller not connected!!" + ex.Message);
                            //outputState.DriveState = new DriveState() { Radius = 2047, Speed = 0, ScoopIn = false, ScoopOut = false, FrontStopArmDown = false, FrontStopArmUp = false, Headlights = false, WallFollow = false};
                            outputState.DriveState.Control = false;
                        }
                    }
                    else if (_driveMethod == 2) //Keyboard
                    {
                        try
                        {
                            /*
                            outputState.DriveState = keyboardDrive.getDriveState();
                            outputState.DriveState.WallFollow = F1_Pressed;
                            outputState.DriveState.FrontStopArmDown = F2_Pressed;
                            outputState.DriveState.FrontStopArmUp = F3_Pressed;
                            outputState.DriveState.Headlights = F4_Pressed;
                            outputState.DriveState.Control = 1;
                            outputState.DriveState.controllerControl = false;
                            */
                        }
                        catch (Exception ex)
                        {
                            //outputState.DriveState = new DriveState() { Radius = 2047, Speed = 0, ScoopIn = false, ScoopOut = false, FrontStopArmDown = false, FrontStopArmUp = false, Headlights = false, WallFollow = false};
                            outputState.DriveState.Control = false;
                            Console.WriteLine("Output State: " + ex.Message);
                        }
                    }
                    else //No control, give back to RC Contoller
                    {
                        //outputState.DriveState = new DriveState() { Radius = 2047, Speed = 0, ScoopIn = false, ScoopOut = false, FrontStopArmDown = false, FrontStopArmUp = false, Headlights = false, WallFollow = false};
                        outputState.DriveState.Control = false;
                    }

                    //Send the same arm state regardless of drive method
                    try
                    {
                        //outputState.ArmState = ArmController.getArmState();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Arm State: " + ex.Message);
                        //Add failsafe if necessary 
                    }

                    //Send the same cmd line state regardless of drive method
                    try
                    {
                        outputState.CmdLineState = CmdLineInput.GetCmdLineState();

                        if (outputState.CmdLineState.CmdLine.Equals(previousCmd.CmdLine))
                        {
                            outputState.CmdLineState = new CmdLineState() { CmdLine = "" };
                            CmdLineInput.CmdLineEvent("");
                        }
                        previousCmd = outputState.CmdLineState;
                    }
                    catch (Exception ex)
                    {
                        outputState.CmdLineState = new CmdLineState() { CmdLine = "" };
                        previousCmd = outputState.CmdLineState;
                        Console.WriteLine("Cmd state: " + ex.Message);
                    }

                    // all UI control received, now send to robot clients
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        serializer.Serialize(ms, outputState);
                        server.SendToAllClients(ms);
                        //Console.WriteLine("UI Control Serialized and Sent to Robot Clients");
                    }
                }
                catch (Exception ex)
                {
                    //log it or something!
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    System.Threading.Monitor.Exit(workerSync);
                }
            }

        }
        #endregion

        #region Output Control
        public void StartOutput()
        {      
            //todo : exception handling
            if (isEnabled)
                return;

            Console.WriteLine("ZServer started on: " + Thread.CurrentThread.Name);

            this.server = new Mars_Rover_Comms.TCP.ZServer(Properties.Settings.Default.ListeningPort);
            
            this.server.PropertyChanged += server_PropertyChanged;
            //Handle return state
            this.server.PacketReceived += new EventHandler<DataArgs>(server_PacketReceived);

            this.server.Start();

            //Set to 100ms by default. 
            worker.Interval = Properties.Settings.Default.OutputInterval;

            //Start the main worker thread
            worker.Enabled = true;
            //Returns RCU console ouput to OCU console
            //updateLog.Enabled = true;
            //Set enable flag property
            IsEnabled = true;
    
            if (stateProcessor.ThreadState == ThreadState.Unstarted)
                stateProcessor.Start();

        }

        public void DisableOutput()
        {
            if (!isEnabled)
                return;

            worker.Enabled = false;
            updateLog.Enabled = false;

            this.setDriveMethod(0);
                    
            server.Stop();

            IsEnabled = false;

        }


        #endregion

        #region Keyboard Control
        public void setDriveMethod(ushort method)
        {
            lock (instanceSync)
            {
                if (method == 1)
                {
                    _isControllerEnabled = true;
                    _isKeyboardEnabled = false;
                    this._driveMethod = 1;
                }
                else if (method == 2)
                {
                    keyboardDrive = KeyboardDrive.KeyboardInstance;
                    _isControllerEnabled = false;
                    _isKeyboardEnabled = true;
                    this._driveMethod = 2;
                }
                else if (method == 0) //Return control to RC
                {
                    _isControllerEnabled = false;
                    _isKeyboardEnabled = false;
                    this._driveMethod = 0;
                }
            }

        }

        public void setKeyboardDriveState(Key key, string state, short speed, short armSpeed)
        {
            if (_isKeyboardEnabled)
            {
                short _speed = -1;
                short _direction = -1;
                short _armSpeed = -1;

                //Begin key check (this will let me know what keys are pressed)
                
                //Check for W
                if (key == Key.W && state.Equals("down"))
                    W_Pressed = true;
                else if (key == Key.W && state.Equals("up"))
                    W_Pressed = false;

                //Check for A
                if (key == Key.A && state.Equals("down"))
                    A_Pressed = true;
                else if (key == Key.A && state.Equals("up"))
                    A_Pressed = false;

                //Check for D
                if (key == Key.D && state.Equals("down"))
                    D_Pressed = true;
                else if (key == Key.D && state.Equals("up"))
                    D_Pressed = false;

                //Check for S
                if (key == Key.S && state.Equals("down"))
                    S_Pressed = true;
                else if (key == Key.S && state.Equals("up"))
                    S_Pressed = false;
                
                //Check for I
                if (key == Key.I && state.Equals("down"))
                    Up_Pressed = true;
                else if (key == Key.I && state.Equals("up"))
                    Up_Pressed = false;

                //Check for K
                if (key == Key.K && state.Equals("down"))
                    Down_Pressed = true;
                else if (key == Key.K && state.Equals("up"))
                    Down_Pressed = false;

                //Check for J
                if (key == Key.J && state.Equals("down"))
                    Left_Pressed = true;
                else if (key == Key.J && state.Equals("up"))
                    Left_Pressed = false;

                //Check for L
                if (key == Key.L && state.Equals("down"))
                    Right_Pressed = true;
                else if (key == Key.L && state.Equals("up"))
                    Right_Pressed = false;
                //end key check

                //Check for Q
                if (key == Key.Q && state.Equals("down"))
                    Q_Pressed = true;
                else if (key == Key.Q && state.Equals("up"))
                    Q_Pressed = false;
                //end key check

                //Check for E
                if (key == Key.E && state.Equals("down"))
                    E_Pressed = true;
                else if (key == Key.E && state.Equals("up"))
                    E_Pressed = false;
                //end key check

                //Begin Execution of the key combinations for driving motors
                /*
                //Indicates that W and A has been pressed (Veer left while moving forward)
                if (W_Pressed == true && A_Pressed == true && S_Pressed == false && D_Pressed == false)
                {
                    _speed = speed;
                    _direction = 4; //veer left
                }
                //Indicates that W and D has been pressed (Veer right while moving forward)
                else if (W_Pressed == true && A_Pressed == false && S_Pressed == false && D_Pressed == true)
                {
                    _speed = speed;
                    _direction = 5; //veer right
                }
                //Indicates that S and A has been pressed (Veer left while moving backward)
                else if (W_Pressed == false && A_Pressed == true && S_Pressed == true && D_Pressed == false)
                {
                    _speed = speed;
                    _direction = 6; //veer left backwards
                }
                //Indicates that S and D has been pressed (Veer right while moving backward)
                else if (W_Pressed == false && A_Pressed == false && S_Pressed == true && D_Pressed == true)
                {
                    _speed = speed;
                    _direction = 7; //veer right backwards
                }
                */
                //Indicates that only W has been pressed (Move Forward)
                if (W_Pressed == true && A_Pressed == false && S_Pressed == false && D_Pressed == false)
                {
                    _speed = speed;
                    _direction = 0;//forward
                }
                //Indicates that only A has been pressed (Spin in place to the left or counterclockwise)
                else if (W_Pressed == false && A_Pressed == true && S_Pressed == false && D_Pressed == false)
                {
                    _speed = speed;
                    _direction = 2;//left
                }
                //Indicates that only S has been pressed (Move Backwards)
                else if (W_Pressed == false && A_Pressed == false && S_Pressed == true && D_Pressed == false)
                {
                    _speed = speed;
                    _direction = 3;//backwards
                }
                //Indicates that only D has been pressed (Spin in place to the right or clockwise)
                else if (W_Pressed == false && A_Pressed == false && S_Pressed == false && D_Pressed == true)
                {
                    _speed = speed;
                    _direction = 1;//right
                }
                //Indicates that no buttons have been pressed (Do nothing - reset state)
                else if (W_Pressed == false && A_Pressed == false && S_Pressed == false && D_Pressed == false)
                {
                    _speed = 0;
                    _direction = 0;
                }

                //Turn Left Slowly
                if (E_Pressed == true && Q_Pressed == false)
                {
                    _speed = 50;
                    _direction = 1;
                }
                //Indicates that no buttons have been pressed (Do nothing - reset state)
                else if (W_Pressed == false && A_Pressed == false && S_Pressed == false && D_Pressed == false)
                {
                    _speed = 0;
                    _direction = 0;
                }
                
                //Turn Right Slowly
                if (Q_Pressed == true && E_Pressed == false)
                {
                    _speed = 50;
                    _direction = 2;
                }
                //Begin execution of key combinations for arm 
                if (Up_Pressed == true && Down_Pressed == false) //move arm up
                {
                    _armSpeed = 0;
                }

                else if (Down_Pressed == true && Up_Pressed == false) // move arm down
                {
                    _armSpeed = 3;//K Pressed
                }

                else if (Down_Pressed == false && Up_Pressed == false) //arm not doing anything
                {
                    _armSpeed = -2;//Stop Arm
                }

                //NOTES:
                //1.) gripper speed is constant for the keyboard and is taken care of in kinematics 
                //2.) if j or l has been hit then left_pressed/ right_pressed will be true use this for scoopIn/scoopOut values in setDriveState
               
                /*
                if (_speed == -1)
                    _speed = keyboardDrive.getDriveState().Speed;
                if (_direction == -1)
                    _direction = keyboardDrive.getDriveState().Radius;
                if (_armSpeed == -1)
                    _armSpeed = keyboardDrive.getDriveState().ArmSpeed;
                */
                keyboardDrive.setDriveState(_speed, _direction, _armSpeed, Left_Pressed, Right_Pressed);
            }
        }

        #endregion

        public bool isCommsEnabled()
        {
            lock (instanceSync)
            {
                return isEnabled;
            }
        }

        public String getLog()
        {
            return _Log;
        }

        private void server_PacketReceived(object sender, DataArgs e)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(e.Data))
                {
                    RobotReturnState state = (RobotReturnState)returnStateDeserializer.Deserialize(ms);
                    stateQueue.Enqueue(state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing state: " + ex.Message);
            }
        }

        private void StateProcessorDoWork()
       {
           while (!tokenSource.Token.IsCancellationRequested)
           {
             try
               {                                              
                    Mars_Rover_Comms.RobotReturnState robotState = stateQueue.Dequeue(tokenSource.Token);

                    LeftSensor = robotState.PositionReturnState.leftDistance;
                    RightSensor = robotState.PositionReturnState.rightDistance;
                    FrontSensor = robotState.PositionReturnState.frontDistance;

                    shoulderPos = robotState.ArmReturnState.shoulderPos;
                    elbowPos = robotState.ArmReturnState.elbowPos;
                    wristPos = robotState.ArmReturnState.wristPos;
                    gripperPos = robotState.ArmReturnState.gripperPos;

                    updateLogString(robotState.LogState.Data);

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

        private void updateLogString(string msg)
        {
            if (msg != null || msg != "")
            {
                string cleanEntry = formatLog(msg);
                RobotLog = RobotLog + cleanEntry + "\n";
            }
   
        }

        private String formatLog(string data)
        {
            String trim = data.Substring(0, data.Length - 1);
            return trim.Replace("??", "\n").Replace("?", "\n");
        }

        public string getLat()
        {
            return _lat;
        }

        public string getLng()
        {
            return _lng;
        }

        public string getHeading()
        {
            return _heading;
        }

        public string getLeftSensor()
        {
            return LeftSensor;
        }
           
        public string getRightSensor()
        {
            return RightSensor;
        }

        public string getFrontLeftSensor()
        {
            return FrontSensor;
        }

        public short getShoulderPos()
        {
            return shoulderPos;
        }

        public short getElbowPos()
        {
            return elbowPos;
        }

        public short getWristPos()
        {
            return wristPos;
        }

        public short getShoulderOCU()
        {
            return shoulderOCU;
        }

        public short getElbowOCU()
        {
            return elbowOCU;
        }

        public short getWristOCU()
        {
            return wristOCU;
        }

        public short getGripperPos()
        {
            return gripperPos;
        }

        public bool getHeadlights()
        {
            return headlights;
        }

        public bool getPID()
        {
            return usePID;
        }

        public short getDriveMode()
        {
            return DriveMode;
        }

        public short getArmMode()
        {
            return ArmMode;
        }
        
        public bool getF1()
        {
            return F1_Pressed;
        }

        public void setF1(bool value)
        {
            F1_Pressed = value;
        }

        public bool getF2()
        {
            return F2_Pressed;
        }

        public void setF2(bool value)
        {
            F2_Pressed = value;
        }

        public bool getF3()
        {
            return F3_Pressed;
        }

        public void setF3(bool value)
        {
            F3_Pressed = value;
        }

        public bool getF4()
        {
            return F4_Pressed;
        }

        public void setF4(bool value)
        {
            F4_Pressed = value;
        }

        public bool getF5()
        {
            return F5_Pressed;
        }

        public void setF5(bool value)
        {
            F5_Pressed = value;
        }
        public bool getF6()
        {
            return F6_Pressed;
        }

        public void setF6(bool value)
        {
            F6_Pressed = value;
        }
    }
}
