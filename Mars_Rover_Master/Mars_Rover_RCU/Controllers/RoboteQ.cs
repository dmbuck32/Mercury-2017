using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO.Ports;
using System.Threading;
using Mars_Rover_Configuration;

namespace Mars_Rover_RCU.Controllers
{
    public class RoboteQ 
    {
        private ConfigureRoboteq configuration;
        private Dictionary<RoboteqChannel, RoboteqOutputSettings> channel_map;
        private SerialPort serial_port;
        private CancellationTokenSource tokenSource;
        private Thread update_thread;

        private volatile bool isActive;

        private Utility.UpdateQueue<Dictionary<Devices, int>> queue;

        public RoboteQ(RoboteqConfiguration config)
        {
            if (config == null)
                throw new ArgumentNullException("config");

            this.configuration = config;
            isActive = false;
            queue = new Utility.UpdateQueue<Dictionary<Devices, int>>(config.Timeout);
            this.ParseConfiguration();
            serial_port = new SerialPort(config.COM_Port, 115200, Parity.None, 8, StopBits.One);
            serial_port.ReadTimeout = 1000;
            serial_port.ErrorReceived += new SerialErrorReceivedEventHandler(serial_port_ErrorReceived);
        }

        void serial_port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine(e.EventType.ToString());
        }

        public bool IsActive
        {
            get { return IsActive; }
        }

        public void Activate()
        {
            if (isActive)
                throw new Exception("Aready activated");

            tokenSource = new CancellationTokenSource();

            if (!serial_port.IsOpen) //ensure port is open
                serial_port.Open();

            serial_port.DiscardInBuffer();
            serial_port.DiscardOutBuffer();
            ////send initialization commands

            //start worker thread
            try
            {
                update_thread = new Thread(new ParameterizedThreadStart(p => UpdateWorker()));
                update_thread.IsBackground = true;
                isActive = true;
                update_thread.Start();
            }
            catch (Exception ex)
            {
                serial_port.Close();
                isActive = false;
            }

        }

        public void Deactivate()
        {
            if (!isActive)
                return;

            //shutdown worker thread
            CleanupUpdateWorker();
            isActive = false;

            if (serial_port != null && serial_port.IsOpen)
            {
                E_Stop();
                serial_port.Close();
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

        public void SetValues(Dictionary<Devices, int> device_mapping)
        {
            //post update to queue
            queue.Enqueue(device_mapping);
        }

        private void ParseConfiguration()
        {
            channel_map = new Dictionary<RoboteqChannel, RoboteqOutputSettings>();
            foreach (var setting in configuration.OutputSettings)
            {
                channel_map.Add(setting.Channel, setting);
            }
        }

        #region Motor operations


        private void UpdateWorker()
        {
            // !M <m1> <m2> for motors pair up commands with underscore, end commands with \r

            int motor1, motor2;
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    //try to recover serial port if it has become disconnected
                    if (!serial_port.IsOpen)
                        serial_port.Open();

                    Dictionary<Devices, int> state = queue.Dequeue(tokenSource.Token);

                    motor1 = GetMotorValue(state, RoboteqChannel.Motor1);
                    motor2 = GetMotorValue(state, RoboteqChannel.Motor2);

                    serial_port.Write("!M " + motor1.ToString() + " " + motor2.ToString() + "\r");
                }
                catch (TimeoutException ex)
                {
                    //send stop values!
                    Console.WriteLine("Roboteq timeout, stopping motors...");
                    E_Stop();
                }
                catch (InvalidOperationException ex)
                {
                    //port is not open!
                    //todo : how to handle?
                    Console.WriteLine("SERIAL PORT NOT OPEN: " + this.configuration.COM_Port.ToString());
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine("Roboteq: " + ex.Message);
                    E_Stop();
                    break;
                }
                catch (Exception ex)
                {
                    // log
                }
            }
        }

        int GetMotorValue(Dictionary<Devices, int> state, RoboteqChannel channel)
        {
            int val;
            RoboteqOutputSettings outputSettings;
            if (channel_map.TryGetValue(channel, out outputSettings))
            {
                if (state.TryGetValue(outputSettings.Device, out val))
                {
                    if (outputSettings.Invert)
                        val *= -1;
                }
                else
                { //information missing from state, return motor stop value
                    val = 0;
                }
            }
            else
            { //value not in configuration, return motor stop value
                val = 0;
            }

            return val;
        }

        //private void SetMotorValue(Dictionary<Devices, int> state, RoboteqChannel channel)
        //{
        //    int val;
        //    RoboteqOutputSettings outputSettings;
        //    if (channel_map.TryGetValue(channel, out outputSettings)) //check if this roboteq has config for this channel
        //    {
        //        if (state.TryGetValue(outputSettings.Device, out val)) //see if the state contains updated value
        //        {
        //            if (outputSettings.Invert)
        //                val *= -1;

        //            switch (channel) //set the appropriate backing variable, set ignore flags
        //            {
        //                case RoboteqChannel.Motor1:
        //                    motor1Value = val;
        //                    Interlocked.Exchange(ref watch_motor1_ignore, 1);
        //                    break;
        //                case RoboteqChannel.Motor2:
        //                    motor2Value = val;
        //                    Interlocked.Exchange(ref watch_motor2_ignore, 1);
        //                    break;
        //                default:
        //                    break;
        //            }
        //        }
        //    }
        //    //if not using motor channel, do nothing
        //}

      
        #endregion

        private void MonitorWorker()
        {
            char[] type_split = new char[]{'=', '\r', '\n'};
            char[] value_separator = new char[]{':'};
            while (!tokenSource.IsCancellationRequested)
            {
                try
                {
                    string result = this.serial_port.ReadTo("\r");

                    //split up any 
                    string[] type_values = result.Split(type_split, StringSplitOptions.RemoveEmptyEntries);

                }
                catch (TimeoutException ex)
                {
                }
                catch (Exception ex)
                {

                }
            }
        }

        private void E_Stop()
        {
            try
            {
                motor1Value = 0;
                motor2Value = 0;
                Update();
            }
            catch (Exception) { }
        }

    }
}
