using System;
using System.Drawing;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Threading;
using System.Windows;
using System.Windows.Threading;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Forms;
using System.Windows.Forms.Integration;
using System.IO;
using System.Net;
using System.Reflection;
using GMap.NET;
using GMap.NET.WindowsForms;
using GMap.NET.WindowsForms.Markers;
using GMap.NET.MapProviders;
using Mars_Rover_OCU.Comms;
using Mars_Rover_OCU.ValidationRules;
using Mars_Rover_OCU.Utilities;
using System.ComponentModel;
using Mars_Rover_OCU.Properties;



namespace Mars_Rover_OCU
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>

    public partial class MainWindow : Window
    {
        private OCUComms comms;

        private bool hasControl;

        SolidColorBrush myWarningBrush = new SolidColorBrush();
        SolidColorBrush myErrorBrush = new SolidColorBrush();
        SolidColorBrush myGoodBrush = new SolidColorBrush();
        SolidColorBrush keyPressed = new SolidColorBrush();
        SolidColorBrush keyReleased = new SolidColorBrush();

        System.Timers.Timer sensorUpdates;

        DispatcherTimer stopWatch;
        DateTime startTime;

        private static readonly object instanceSync = new object();

        public MainWindow()
        {
            InitializeComponent();
            hasControl = false;

            sensorUpdates = new System.Timers.Timer();
            comms = OCUComms.Instance;

            comms.PropertyChanged += comms_LogChanged;
            comms.PropertyChanged += comms_IsClientConnectedChanged;
            sensorUpdates.Elapsed += sensorUpdates_Elapsed;

            myWarningBrush.Color = System.Windows.Media.Color.FromRgb(255, 209, 0);
            myErrorBrush.Color = System.Windows.Media.Color.FromRgb(255, 0, 20);
            myGoodBrush.Color = System.Windows.Media.Color.FromRgb(20, 255, 0);
            keyPressed.Color = System.Windows.Media.Color.FromArgb(204, 100, 100, 100);
            keyReleased.Color = System.Windows.Media.Color.FromArgb(204, 255, 255, 255);

            keyboard.IsEnabled = false;
            keyboard_Copy.IsEnabled = false;
            RoverSettings.IsEnabled = false;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            var window = Window.GetWindow(this);


            window.KeyDown += HandleKeyPress;
            window.KeyUp += HandleKeyRelease;

            startTime = DateTime.Now;
            stopWatch = new DispatcherTimer();
            stopWatch.Tick += new EventHandler(stopWatch_Tick);
            stopWatch.Interval = new TimeSpan(0, 0, 1);

            sensorUpdates.Start();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            
        }

        #region Rover Control

        #region Events

        private void HandleKeyPress(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (RoverSettings.IsEnabled == true)
            {
                if (e.Key == Key.F1) // Home Macro
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (comms.getF1() == false && comms.getF2() == false && comms.getF3() == false)
                        {
                            comms.setF1(true);
                            HomeMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF1() == false && comms.getF2() == true && comms.getF3() == false)
                        {
                            comms.setF2(false);
                            SampleCollectionMacroBtn.Fill = keyReleased;
                            comms.setF1(true);
                            HomeMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF1() == false && comms.getF2() == false && comms.getF3() == true)
                        {
                            comms.setF3(false);
                            SampleDepositMacroBtn.Fill = keyReleased;
                            comms.setF1(true);
                            HomeMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF1() == true)
                        {
                            comms.setF1(false);
                            HomeMacroBtn.Fill = keyReleased;
                        }                    
                    }));
                }

                if (e.Key == Key.F2) // Sample Collection Macro
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (comms.getF1() == false && comms.getF2() == false && comms.getF3() == false)
                        {
                            comms.setF2(true);
                            SampleCollectionMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF1() == true && comms.getF2() == false && comms.getF3() == false)
                        {
                            comms.setF1(false);
                            HomeMacroBtn.Fill = keyReleased;
                            comms.setF2(true);
                            SampleCollectionMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF1() == false && comms.getF2() == false && comms.getF3() == true)
                        {
                            comms.setF3(false);
                            SampleDepositMacroBtn.Fill = keyReleased;
                            comms.setF2(true);
                            SampleCollectionMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF2() == true)
                        {
                            comms.setF2(false);
                            SampleCollectionMacroBtn.Fill = keyReleased;
                        }
                    }));
                }

                if (e.Key == Key.F3) // Sample Deposit Macro
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (comms.getF1() == false && comms.getF2() == false && comms.getF3() == false)
                        {
                            comms.setF3(true);
                            SampleDepositMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF1() == true && comms.getF2() == false && comms.getF3() == false)
                        {
                            comms.setF1(false);
                            HomeMacroBtn.Fill = keyReleased;
                            comms.setF3(true);
                            SampleDepositMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF1() == false && comms.getF2() == true && comms.getF3() == false)
                        {
                            comms.setF2(false);
                            SampleCollectionMacroBtn.Fill = keyReleased;
                            comms.setF3(true);
                            SampleDepositMacroBtn.Fill = keyPressed;
                        }
                        else if (comms.getF3() == true)
                        {
                            comms.setF3(false);
                            SampleDepositMacroBtn.Fill = keyReleased;
                        }
                    }));
                }

                if (e.Key == Key.F4) //Toggle headlights
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        if (comms.getF4() == false)
                        {
                            comms.setF4(true);
                            ToggleHeadlightsBtn.Fill = keyPressed;
                        }
                        else
                        {
                            comms.setF4(false);
                            ToggleHeadlightsBtn.Fill = keyReleased;
                        }
                    }));
                }
            }

                comms.setKeyboardDriveState(e.Key, "down", (short)keyboardSpeedSlider.Value, (short)keyboardSpeedSlider1.Value);

                if (e.Key == Key.W) //Forward
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        forwardBtn.Fill = keyPressed;
                    }));
                }

                if (e.Key == Key.A) //Left
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        leftBtn.Fill = keyPressed;
                    }));
                }

                if (e.Key == Key.D) //Right
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        rightBtn.Fill = keyPressed;
                    }));
                }

                if (e.Key == Key.S) //Reverse
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        reverseBtn.Fill = keyPressed;
                    }));
                }

                if (e.Key == Key.I) //arm up
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ArmUpBtn.Fill = keyPressed;
                    }));
                }

                if (e.Key == Key.K) //arm down
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ArmDownBtn.Fill = keyPressed;
                    }));
                }

                if (e.Key == Key.J) //scoop in
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ScoopInBtn.Fill = keyPressed;
                    }));
                }

                if (e.Key == Key.L) //scoop out
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ScoopOutBtn.Fill = keyPressed;
                    }));
                }
        }

        //Todo create a key parse to consolidate into one function
        private void HandleKeyRelease(object sender, System.Windows.Input.KeyEventArgs e)
        {
                comms.setKeyboardDriveState(e.Key, "up", (short)keyboardSpeedSlider.Value, (short)keyboardSpeedSlider1.Value);

                if (e.Key == Key.W) //Forward
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        forwardBtn.Fill = keyReleased;
                    }));
                }

                if (e.Key == Key.A) //Left
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        leftBtn.Fill = keyReleased;
                    }));
                }

                if (e.Key == Key.D) //Right
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        rightBtn.Fill = keyReleased;
                    }));
                }

                if (e.Key == Key.S) //Reverse
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        reverseBtn.Fill = keyReleased;
                    }));
                }

                if (e.Key == Key.I) //arm up
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ArmUpBtn.Fill = keyReleased;
                    }));
                }

                if (e.Key == Key.K) //arm down
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ArmDownBtn.Fill = keyReleased;
                    }));
                }

                if (e.Key == Key.J) //scoop in
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ScoopInBtn.Fill = keyReleased;
                    }));
                }

                if (e.Key == Key.L) //scoop out
                {
                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        ScoopOutBtn.Fill = keyReleased;
                    }));
                }
        }

        private void stopWatch_Tick(object sender, EventArgs e)
        {
            this.Dispatcher.Invoke((Action)(() =>
            {
                elapsedTime.Content = (DateTime.Now - startTime).ToString();

                // Forcing the CommandManager to raise the RequerySuggested event
                CommandManager.InvalidateRequerySuggested();

                if ((DateTime.Now - startTime).Minutes >= 40)
                    elapsedTime.Foreground = myErrorBrush;
            }));

        }

        private void stopWatchReset_Clicked(object sender, RoutedEventArgs e)
        {
            stopWatch.Stop();
            startTime = DateTime.Now;
            stopWatch.Start();
        }

        void comms_IsClientConnectedChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("IsClientConnected"))
            {
                if (comms.isCommsEnabled())
                {

                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        connectionStatusLbl.Content = "Connected - No Control";
                        connectionStatusLbl.Foreground = myWarningBrush;

                        roverControlBtn.IsEnabled = true;
                        manualControlRB.IsEnabled = true;

                    }));

                }
                else
                {


                    this.Dispatcher.Invoke((Action)(() =>
                    {
                        connectionStatusLbl.Content = "Not Connected";
                        connectionStatusLbl.Foreground = myErrorBrush;

                        roverControlBtn.IsEnabled = false;
                        manualControlRB.IsEnabled = false;

                    }));

                }
            }

        }

        void comms_LogChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName.Equals("RobotLog"))
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    logConsole.Text = comms.getLog();
                    logConsole.ScrollToEnd();
                }));
            }
        }

        void sensorUpdates_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                this.Dispatcher.Invoke((Action)(() =>
                {
                    LeftSensor.Content = comms.getLeftSensor();
                    RightSensor.Content = comms.getRightSensor();
                    FrontSensor.Content = comms.getFrontLeftSensor();

                    SpeedSensitivity.Text = ControllerSettings.Default.Speed.ToString();
                }));
            }
            catch (TaskCanceledException)
            {//do nothing
            }
        }

        #endregion

        #region Rover Tab Click Events

        private void roverListenBtn_Click(object sender, RoutedEventArgs e)
        {

            comms.StartOutput();

            this.Dispatcher.Invoke((Action)(() =>
            {
                connectionStatusLbl.Content = "Listening...";
                connectionStatusLbl.Foreground = myGoodBrush;

                roverListenBtn.IsEnabled = false;
                disconnectRoverBtn.IsEnabled = true;
            }));
        }

        private void roverControlBtn_Click(object sender, RoutedEventArgs e)
        {

            if (!hasControl) //Take Control
            {
                comms.setDriveMethod(1);
                hasControl = true;

                this.Dispatcher.Invoke((Action)(() =>
                           {
                               xboxControlRB.IsEnabled = true;
                               keyControlRB.IsEnabled = true;
                               RoverSettings.IsEnabled = true;

                               xboxControlRB.IsChecked = true;
                               roverControlBtn.Content = "Release Control";

                               connectionStatusLbl.Content = "Connected - XBox Control";
                               connectionStatusLbl.Foreground = myGoodBrush;

                               startTime = DateTime.Now;
                               stopWatch.Start();
                           }));
            }
            else if (hasControl) //Release Control to RC
            {
                comms.setDriveMethod(0);
                hasControl = false;

                this.Dispatcher.Invoke((Action)(() =>
                           {
                               roverControlBtn.Content = "Take Control";

                               connectionStatusLbl.Content = "Connected - No Control";
                               connectionStatusLbl.Foreground = myWarningBrush;
                               xboxControlRB.IsEnabled = false;
                               keyControlRB.IsEnabled = false;
                               RoverSettings.IsEnabled = false;

                               keyboard.IsEnabled = false;
                               keyboard_Copy.IsEnabled = false;
                               stopWatch.Stop();
                           }));
            }

        }

        private void disconnectBtn_Click(object sender, RoutedEventArgs e) //Todo use release control click to clean up
        {
            if (comms.isCommsEnabled())
            {
                comms.DisableOutput();

                this.Dispatcher.Invoke((Action)(() =>
       {

           roverListenBtn.IsEnabled = true;
           disconnectRoverBtn.IsEnabled = false;
           xboxControlRB.IsEnabled = false;
           keyControlRB.IsEnabled = false;
           roverControlBtn.IsEnabled = false;
           manualControlRB.IsEnabled = false;
           RoverSettings.IsEnabled = false;
           keyboard.IsEnabled = false;
           keyboard_Copy.IsEnabled = false;
           connectionStatusLbl.Content = "Not Connected";
           connectionStatusLbl.Foreground = myErrorBrush;
       }));
            }

        }

        private void keyboard_Checked(object sender, RoutedEventArgs e)
        {
            comms.setDriveMethod(2);

            this.Dispatcher.Invoke((Action)(() =>
            {
                connectionStatusLbl.Content = "Connected - Key Control";
                connectionStatusLbl.Foreground = myGoodBrush;

                keyboard.IsEnabled = true;
                keyboard_Copy.IsEnabled = true;
            }));

        }

        private void xbox_Checked(object sender, RoutedEventArgs e)
        {
            if (connectionStatusLbl != null)
            {
                comms.setDriveMethod(1);

                this.Dispatcher.Invoke((Action)(() =>
                {
                    connectionStatusLbl.Content = "Connected - XBox Control";
                    connectionStatusLbl.Foreground = myGoodBrush;

                    keyboard.IsEnabled = false;
                    keyboard_Copy.IsEnabled = false;
                }));
            }

        }

        #endregion
        
        #endregion

    }
}

