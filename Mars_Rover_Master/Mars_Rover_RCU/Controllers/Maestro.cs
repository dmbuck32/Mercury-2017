using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Pololu.UsbWrapper;
using Pololu.Usc;
using Mars_Rover_RCU.Components;
using Mars_Rover_Configuration;
using Mars_Rover_RCU.Utilities;
using System.IO;

namespace Mars_Rover_RCU.Controllers
{
    class Maestro
    {
        private ConfigureMaestro configuration;

        private Dictionary<SteeringServoChannel, SteeringServoOutputSettings> steering_channel_map;
        private Dictionary<DrivingMotorChannel, DrivingMotorOutputSettings> driving_channel_map;
        private Dictionary<ControlSignalChannel, ControlSignalOutputSettings> control_channel_map;

        private Usc _Maestro = null;
        //private UscSettings _MaestroSettings;
        private const String _SN = "00119433";
        //private const String _SN = "00109277"; //In Robot
        //private const String _SN = "00072722";
        private int _timeout = 3000;

        private Boolean _useTurning;

        private List<TurningServo> turningServos;

        public Utility.UpdateQueue<Dictionary<Devices, int>> queue;

        private volatile bool isActive;
        private CancellationTokenSource tokenSource;
        private Thread update_thread;

        private int SteeringPWMValue = 1700; //keep track of steering value (arm servo)

        public Maestro(ConfigureMaestro config)
        {
            if (config == null)
                throw new ArgumentNullException("Maestro: Null Config");

            if (isActive)
                throw new Exception("Maestro Already Active");

            configuration = config;

            queue = new Utility.UpdateQueue<Dictionary<Devices, int>>(_timeout);

            Logger.WriteLine("Searching for maestro...");
            List<DeviceListItem> connectedDevices = Usc.getConnectedDevices();

            foreach (DeviceListItem device in connectedDevices)
            {
                if (device.serialNumber != _SN)
                {
                    continue;
                }

                _Maestro = new Usc(device);

                Logger.WriteLine("Connected!");
                isActive = true;

                showInitConfig(_Maestro, new ushort[] { 0, 3, 4, 5 }); //2,7,8,9,10,13,14,15,15,23});

                //getConf(_Maestro, @"c:\Users\Nikki\Documents\maestro_settings_2.txt");
                //configure(_Maestro, @"c:\Users\Nikki\Documents\maestro_settings.txt");
              
                this.ParseConfiguration();

                tokenSource = new CancellationTokenSource();

                try
                {
                    update_thread = new Thread(new ParameterizedThreadStart(p => UpdateWorker()));
                    update_thread.Name = "Maestro Worker Thread";
                    update_thread.IsBackground = true;
                    isActive = true;
                    update_thread.Start();
                }
                catch (Exception ex)
                {
                    Logger.WriteLine("Maestro Activate: " + ex.Message);
                    CleanupUpdateWorker();
                    isActive = false;
                }
            }
            
        }

        private void ParseConfiguration()
        {

            steering_channel_map = new Dictionary<SteeringServoChannel, SteeringServoOutputSettings>();
            driving_channel_map = new Dictionary<DrivingMotorChannel, DrivingMotorOutputSettings>();
            control_channel_map = new Dictionary<ControlSignalChannel, ControlSignalOutputSettings>();

            //Control
            control_channel_map.Add(configuration.ControlOutput.Channel, configuration.ControlOutput);

            //Turning Motors
            steering_channel_map.Add(configuration.SteeringServoOutput1.Channel, configuration.SteeringServoOutput1);
            steering_channel_map.Add(configuration.SteeringServoOutput2.Channel, configuration.SteeringServoOutput2);
            steering_channel_map.Add(configuration.SteeringServoOutput3.Channel, configuration.SteeringServoOutput3);
            steering_channel_map.Add(configuration.SteeringServoOutput4.Channel, configuration.SteeringServoOutput4);

            //Driving Motors
            driving_channel_map.Add(configuration.DrivingMotorOutput6.Channel, configuration.DrivingMotorOutput6);
            driving_channel_map.Add(configuration.DrivingMotorOutput7.Channel, configuration.DrivingMotorOutput7); //mercury arm motor
            driving_channel_map.Add(configuration.DrivingMotorOutput8.Channel, configuration.DrivingMotorOutput8); //mercury left wheels
            driving_channel_map.Add(configuration.DrivingMotorOutput9.Channel, configuration.DrivingMotorOutput9); //mercury right wheels
            driving_channel_map.Add(configuration.DrivingMotorOutput10.Channel, configuration.DrivingMotorOutput10);
            driving_channel_map.Add(configuration.DrivingMotorOutput11.Channel, configuration.DrivingMotorOutput11);

        }

        private void UpdateWorker()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {

                try
                {
                    Dictionary<Devices, int> state = queue.Dequeue(tokenSource.Token);
                
                    //Control Signal 
                    _Maestro.setTarget((Byte)Devices.ControlSignal, (UInt16)(getControlValue(state, ControlSignalChannel.Channel23) * 4));

                    //Turning Motors
                     _Maestro.setTarget((Byte)Devices.FrontLeftSteering, (UInt16)(getSteeringValue(state, SteeringServoChannel.Channel13)*4));
                    _Maestro.setTarget((Byte)Devices.FrontRightSteering, (UInt16)(getSteeringValue(state, SteeringServoChannel.Channel5)*4)); //was 15 arm servo
                    _Maestro.setTarget((Byte)Devices.RearLeftSteering, (UInt16)(getSteeringValue(state, SteeringServoChannel.Channel14)*4));
                    _Maestro.setTarget((Byte)Devices.RearRightSteering, (UInt16)(getSteeringValue(state, SteeringServoChannel.Channel16)*4));

                    //Driving Motors
                    _Maestro.setTarget((Byte)Devices.FrontLeftWheel, (UInt16)(getDrivingValue(state, DrivingMotorChannel.Channel1) * 4));
                    _Maestro.setTarget((Byte)Devices.FrontRightWheel, (UInt16)(getDrivingValue(state, DrivingMotorChannel.Channel4) * 4)); //was 10 arm motor
                    _Maestro.setTarget((Byte)Devices.MidLeftWheel, (UInt16)(getDrivingValue(state, DrivingMotorChannel.Channel0) * 4)); //was 7
                    _Maestro.setTarget((Byte)Devices.MidRightWheel, (UInt16)(getDrivingValue(state, DrivingMotorChannel.Channel3) * 4)); //was 8
                    _Maestro.setTarget((Byte)Devices.RearLeftWheel, (UInt16)(getDrivingValue(state, DrivingMotorChannel.Channel2) * 4));
                    _Maestro.setTarget((Byte)Devices.RearRightWheel, (UInt16)(getDrivingValue(state, DrivingMotorChannel.Channel9) * 4));

                }
                catch
                { }
                
            }
        }

        private void CleanupUpdateWorker()
        {
            tokenSource.Cancel();
            if (update_thread != null && update_thread.ThreadState != ThreadState.Unstarted)
            {
                update_thread.Join();
                update_thread = null;
            }
        }

        int getControlValue(Dictionary<Devices, int> state, ControlSignalChannel channel)
        {
            int val;
            ControlSignalOutputSettings outputSettings;

            if (control_channel_map.TryGetValue(channel, out outputSettings))
            {
                if (state.TryGetValue(outputSettings.Device, out val))
                {
                    if (val == 1)
                    {
                        return 1600;
                    }
                }
            }

            return 1850;
        }

        int getDrivingValue(Dictionary<Devices, int> state, DrivingMotorChannel channel)
        {
            int val;
           DrivingMotorOutputSettings outputSettings;

            if (driving_channel_map.TryGetValue(channel, out outputSettings))
            {
                if (state.TryGetValue(outputSettings.Device, out val))
                {
                    int high = outputSettings.PWM_Map.PWM_High;
                    int low = outputSettings.PWM_Map.PWM_Low;
                    int stop = outputSettings.StopValue;

                    if (val < 0)
                        val = (int)((double)(stop - low)) / 45 * val + stop;
                    else if (val > 0)
                        val = (int)((double)(high - stop)) / 45 * val + stop;
                    else
                        val = (int)(stop);
                }
                else
                {
                    val = 0;
                }

                //Logger.WriteLine(val.ToString());
                return val;
            }
            else
            { //value not in configuration, return motor stop value
                val = 0;
            }
            return val;

        }

        int getSteeringValue(Dictionary<Devices, int> state, SteeringServoChannel channel)
        {
            int val;
            SteeringServoOutputSettings outputSettings;
            
            if (steering_channel_map.TryGetValue(channel, out outputSettings))
            {
                if (state.TryGetValue(outputSettings.Device, out val))
                {
                    int high = outputSettings.PWM_Map.PWM_High; //1700
                    int low = outputSettings.PWM_Map.PWM_Low; //1000
                    int stop = outputSettings.StopValue; //1700
                    
                   // val = -val;

                    if (SteeringPWMValue + val > high && channel == SteeringServoChannel.Channel5)
                        SteeringPWMValue = high;
                    else if (SteeringPWMValue + val < low && channel == SteeringServoChannel.Channel5)
                        SteeringPWMValue = low;
                    else if (SteeringPWMValue + val <= high && SteeringPWMValue + val >= low && channel == SteeringServoChannel.Channel5)
                        SteeringPWMValue += 1 * val;
                    
                    /*
                    if (val < 0)
                        val = (int)((double)(stop - low)) / 45 * val + stop;
                    else if (val > 0)
                        val = (int)((double)(high - stop)) / 45 * val + stop;
                    else
                        val = (int)(stop);
                    */
                }
                else
                {
                    return 1700; //gripper closed
                }

                //Logger.WriteLine(val.ToString());
                //return SteeringPWMValue;
            }
            else
            { //value not in configuration, return motor stop value
                return 1700;
            }
            return SteeringPWMValue;
        }
            
       /* public void activateTurning(Boolean toCenter, String target = "1500")
        {
            _useTurning = true;

            turningServos = new List<TurningServo>(4);
            
            turningServos.Add(new TurningServo(Devices.FrontLeftSteering));
            turningServos.Add(new TurningServo(Devices.FrontRightSteering));
            turningServos.Add(new TurningServo(Devices.RearLeftSteering));
            turningServos.Add(new TurningServo(Devices.RearRightSteering));

            if (toCenter)
                //Return to Center if the wheels are free to move (not stuck)
                foreach (TurningServo servo in turningServos)
                {
                _Maestro.setTarget(servo.getChannel(), (UInt16)(UInt16.Parse(target) * 4));   
                }
        }
        * */


        public void Deactivate()
        {
            _Maestro.disconnect();
        }

        public void EnqueueState(Dictionary<Devices, int> state)
        {
            try
            {
                queue.Enqueue(state);
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Maestro Enqueue: " + ex.Message);
            }
        }

        static void showInitConfig(Usc maestro, ushort[] channels)
        {
            UscSettings maestroSettings = maestro.getUscSettings();
            List<ChannelSetting> channelSetting = maestroSettings.channelSettings;
      
            foreach(ushort index in channels)
            {
                Logger.WriteLine("Channel: " + index);
                Logger.WriteLine("Minimum: " + channelSetting.ElementAt(index).minimum / 4);
                Logger.WriteLine("Maximum: " + channelSetting.ElementAt(index).maximum / 4);
                Logger.WriteLine("Neutral: " + channelSetting.ElementAt(index).neutral / 4);
                Logger.WriteLine("Speed: " + channelSetting.ElementAt(index).speed);
                Logger.WriteLine("Acceleration: " + channelSetting.ElementAt(index).acceleration);
                
            }
        }

        static void getConf(Usc usc, string filename)
        {
            Stream file = File.Open(filename, FileMode.Create);
            StreamWriter sw = new StreamWriter(file);
            ConfigurationFile.save(usc.getUscSettings(), sw);
            sw.Close();
            file.Close();
        }

        static void configure(Usc usc, string filename)
        {
            Stream file = File.Open(filename, FileMode.Open);
            StreamReader sr = new StreamReader(file);
            List<String> warnings = new List<string>();
            UscSettings settings = ConfigurationFile.load(sr, warnings);
            usc.fixSettings(settings, warnings);
            usc.setUscSettings(settings, true);
            sr.Close();
            file.Close();
            usc.reinitialize();
        }

    }

   

}
