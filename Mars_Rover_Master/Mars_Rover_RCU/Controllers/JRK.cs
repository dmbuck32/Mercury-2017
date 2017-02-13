using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pololu.Jrk;
using Pololu.UsbWrapper;
using System.Threading;
using Mars_Rover_Configuration;

namespace Mars_Rover_RCU.Controllers
{
    public class JRK : Jrk
    {
        private ConfigureJRK _configuration;
        private String _serialNumber;
        private Byte channel;
        private static Dictionary<SteeringServoChannel, SteeringServoOutputSettings> steering_channel_map;
        static Utility.UpdateQueue<Dictionary<Devices, int>> queue;
        private static CancellationTokenSource token;
        private static Thread jrk_thread;

        public JRK(DeviceListItem deviceList, ConfigureJRK config):base(deviceList)
        {
            _serialNumber = deviceList.serialNumber;

            if (_serialNumber.Equals("00018877"))
                channel = 1;
            
            _configuration = config;

            queue = new Utility.UpdateQueue<Dictionary<Devices, int>>(3000);

            this.ParseConfiguration();

            token = new CancellationTokenSource();

            jrk_thread = new Thread(new ParameterizedThreadStart(p => UpdateWorker()));
            jrk_thread.Name = "JRK Worker Thread";
            jrk_thread.IsBackground = true;
            jrk_thread.Start();     
        }

        private void activate()
        {

        }

        private int getSteeringValue(Dictionary<Devices, int> state, SteeringServoChannel channel)
        {
            int val;
            SteeringServoOutputSettings outputSettings;

            if (steering_channel_map.TryGetValue(channel, out outputSettings))
            {
                if (state.TryGetValue(outputSettings.Device, out val))
                {
                    int high = outputSettings.PWM_Map.PWM_High;
                    int low = outputSettings.PWM_Map.PWM_Low;
                    int stop = outputSettings.StopValue;
                    
                    if (val < stop)
                        val = (int)((double)(stop - low)) / 45 * val;
                    else if (val > stop)
                        val = (int)((double)(high - stop)) / 45 * val;
                    else
                        val = stop;
                }
                else
                {
                    val = 2250;
                }

                return val;
            }
            else
            { //value not in configuration, return motor stop value
                val = 0;
            }
            return val;
        }

        public void EnqueueState(Dictionary<Devices, int> state)
        {
            try
            {
                queue.Enqueue(state);
            }
            catch (Exception ex)
            {
                Mars_Rover_RCU.Utilities.Logger.WriteLine("JRK SN: " + _serialNumber + " Enqueue: " + ex.Message);
            }
        }

        private void ParseConfiguration()
        {

            steering_channel_map = new Dictionary<SteeringServoChannel, SteeringServoOutputSettings>();

            steering_channel_map.Add(_configuration.SteeringServoOutput1.Channel, _configuration.SteeringServoOutput1);

        }

        private void UpdateWorker()
        {
            while (!token.Token.IsCancellationRequested)
            {

                try
                {
                    Dictionary<Devices, int> state = queue.Dequeue(token.Token);
                    this.setTarget((UInt16)(getSteeringValue(state, SteeringServoChannel.Channel1)));
                    //this.setTarget((UInt16)(2296));

                }
                catch
                { }
            }
        }


        private void CleanupUpdateWorker()
        {
            token.Cancel();
            if (jrk_thread != null && jrk_thread.ThreadState != System.Threading.ThreadState.Unstarted)
            {
                this.motorOff();
                jrk_thread.Join();
                jrk_thread = null;
            }
        }

    }
}
