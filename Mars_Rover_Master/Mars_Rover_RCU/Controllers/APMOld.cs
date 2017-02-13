using System;
using System.Collections.Generic;
using System.Collections;
using System.IO.Ports;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mars_Rover_Configuration;
using System.Reactive.Subjects;
using System.Runtime.InteropServices;
using Mars_Rover_APM;
using Mars_Rover_Comms;
using Mars_Rover_APM.Utilities;
using Mars_Rover_RCU.Utilities;

namespace Mars_Rover_RCU.Controllers
{
    public class APMInterface : APM
    {

        public bool debug = false;

        #region APMInterface Variables and Objects

        public event EventHandler ParamListChanged;
       
        /// <summary>
        /// used to prevent comport access for exclusive use
        /// </summary>
        public bool giveComport { get { return _giveComport; } set { _giveComport = value; } }
        bool _giveComport = false;

        internal string plaintxtline = "";
        string buildplaintxtline = "";

        public Dictionary<string, APM_PARAM_TYPE> param_types = new Dictionary<string, APM_PARAM_TYPE>();

        public APMState APM = new APMState();

        public double CONNECT_TIMEOUT_SECONDS = 30;


        /// <summary>
        /// used for outbound packet sending
        /// </summary>
        internal int packetcount = 0;

        /// <summary>
        /// used to calc packets per second on any single message type - used for stream rate comparaison
        /// </summary>
        public double[] packetspersecond { get; set; }
        /// <summary>
        /// time last seen a packet of a type
        /// </summary>
        DateTime[] packetspersecondbuild = new DateTime[256];

        /* a library for composing asynchronous and event-based programs using observable sequences and LINQ-style query operators. 
        * Rx = Observables + LINQ + Schedulers
        * http://rx.codeplex.com/
        */
        private readonly Subject<int> _bytesReceivedSubj = new Subject<int>();
        private readonly Subject<int> _bytesSentSubj = new Subject<int>();

        /// <summary>
        /// Observable of the count of bytes received, notified when the bytes themselves are received
        /// </summary>
        public IObservable<int> BytesReceived { get { return _bytesReceivedSubj; } }

        /// <summary>
        /// Observable of the count of bytes sent, notified when the bytes themselves are received
        /// </summary>
        public IObservable<int> BytesSent { get { return _bytesSentSubj; } }

        /// <summary>
        /// Observable of the count of packets skipped (on reception), 
        /// calculated from periods where received packet sequence is not
        /// contiguous
        /// </summary>
        public Subject<int> WhenPacketLost { get; set; }

        public Subject<int> WhenPacketReceived { get; set; }

        //Serial Port write lock
        private volatile object objlock = new object();
        /// <summary>
        /// used for a readlock on readpacket
        /// </summary>
        volatile object readlock = new object();
        /// <summary>
        /// time seen of last apm packet
        /// </summary>
        public DateTime lastvalidpacket { get; set; }

        float synclost;
        internal float packetslost = 0;
        internal float packetsnotlost = 0;
        DateTime packetlosttimer = DateTime.MinValue;

        /// <summary>
        /// used for last bad serial characters
        /// </summary>
        byte[] lastbad;

        //private Dictionary<MotorChannel, MotorOutputSettings> motor_channel_map;
        //private Dictionary<SteeringServoChannel, SteeringServoOutputSettings> steering_channel_map;
        private Hashtable paramList;

        private Thread update_thread;
        public SerialPort serial_port;
        public SerialPort serial_mirror {get; set;}
        private ConfigureAPM apmConfig;

        //public Utility.UpdateQueue<Dictionary<Devices, int>> queue;
        //public Utility.UpdateQueue<string> queueCmd;

        //private CancellationTokenSource tokenSource;

        public APMInterface(ConfigureAPM config)
        {
            if (config == null)
                throw new ArgumentNullException("APM: config");

            apmConfig = config;

            //queue = new Utility.UpdateQueue<Dictionary<Devices, int>>(config.Timeout);
            //queueCmd = new Utility.UpdateQueue<string>(config.Timeout);     
            //this.ParseConfiguration();

            this.serial_port = new SerialPort(config.COM_Port, config.Baud_Rate, Parity.None, 8, StopBits.One);
            this.serial_port.DtrEnable = config.DTREnable;
            this.serial_port.ReadBufferSize = config.ReadBufferSize;
            this.serial_port.ReadTimeout = config.Timeout;
            //this.serial_port.WriteTimeout = config.Timeout;
            this.serial_port.ErrorReceived += new SerialErrorReceivedEventHandler(serial_port_ErrorReceived);

            // init fields
            this.packetcount = 0;

            this.packetspersecond = new double[0x100];
            this.packetspersecondbuild = new DateTime[0x100];
            this._bytesReceivedSubj = new Subject<int>();
            this._bytesSentSubj = new Subject<int>();
            this.WhenPacketLost = new Subject<int>();
            this.WhenPacketReceived = new Subject<int>();
            this.readlock = new object();
            this.lastvalidpacket = DateTime.MinValue;

            this.packetslost = 0f;
            this.packetsnotlost = 0f;
            this.packetlosttimer = DateTime.MinValue;
            this.lastbad = new byte[2];
        }
        

        #endregion

        public SerialPort getAPMPort()
        {
            return serial_port;
        }

        void serial_port_ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            Logger.WriteLine("Is this my error: " + e.EventType.ToString());
        }

        private void ParseConfiguration()
        {
            //motor_channel_map = new Dictionary<MotorChannel, MotorOutputSettings>();
            //steering_channel_map = new Dictionary<SteeringServoChannel, SteeringServoOutputSettings>();

            /*foreach (var setting in apmConfig.MotorOutputSettingsList)
                motor_channel_map.Add(setting.Channel, setting);
            foreach (var setting in apmConfig.SteeringServoOuputSettingsList)
                steering_channel_map.Add(setting.Channel, setting); */
        }

        public void Activate(bool getparams)
        {
            if (serial_port.IsOpen)
                throw new Exception("APM: Already Active");

            if (getparams)
            {
                //Thread bg = new Thread(new ThreadStart(getStartedWithParams));
                //bg.Name = "APM Background Thread";
                //bg.Start();
                getStartedWithParams();
            }
            else
            {
                Thread bg= new Thread(new ThreadStart(getStartedWithOutParams));
                bg.Name = "APM Background Thread";
                bg.Start();
            }       
        }

        public void Deactivate()
        {
            try
            {
                if (serial_port.IsOpen)
                    serial_port.Close();
            }
            catch { }
        }

        private void getStartedWithOutParams()
        {
            openBg(false);
        }

        private void getStartedWithParams()
        {
            openBg(true);

        }

        private void openBg(bool getParams)
        {
            Logger.WriteLine("APM Connecting...");

            giveComport = true;

            // allow settings to settle - previous dtr 
            System.Threading.Thread.Sleep(500);

            // reset
            APM.sysid = 0;
            APM.compid = 0;
            APM.param = new Hashtable();
            APM.packets.Initialize();

            bool hbseen = false;

            try
            {
                lock (objlock) // so we dont have random traffic
                {
                    serial_port.Open();

                    serial_port.DiscardInBuffer();

                    // other boards seem to have issues if there is no delay? posible bootloader timeout issue
                    Thread.Sleep(1000);
                }

                byte[] buffer = new byte[0];
                byte[] buffer1 = new byte[0];

                DateTime start = DateTime.Now;
                DateTime deadline = start.AddSeconds(CONNECT_TIMEOUT_SECONDS);

                var countDown = new System.Timers.Timer { Interval = 1000, AutoReset = false };
                countDown.Elapsed += (sender, e) =>
                {
                    int secondsRemaining = (deadline - e.SignalTime).Seconds;
                    if (secondsRemaining > 0) countDown.Start();
                };
                countDown.Start();

                int count = 0;

                while (true)
                {
                    if (DateTime.Now > deadline)
                    {
                        //if (Progress != null)
                        //    Progress(-1, "No Heatbeat Packets");
                        countDown.Stop();
                        this.Deactivate();

                        if (hbseen)
                        {
                            Logger.WriteLine("Only 1 Heatbeat Received");
                            throw new Exception("Only 1 Mavlink Heartbeat Packets was read from this port - Verify your hardware is setup correctly\nMission Planner waits for 2 valid heartbeat packets before connecting");
                        }
                        else
                        {
                            Logger.WriteLine("No Heatbeat Packets Received");
                            throw new Exception(@"Can not establish a connection\n
Please check the following
1. You have firmware loaded
2. You have the correct serial port selected
3. PX4 - You have the microsd card installed
4. Try a diffrent usb port\n\n" + "No Mavlink Heartbeat Packets where read from this port - Verify Baud Rate and setup\nMission Planner waits for 2 valid heartbeat packets before connecting");
                        }
                    }

                    System.Threading.Thread.Sleep(1);

                    // incase we are in setup mode
                    //serial_port.WriteLine("planner\rgcs\r");

                    // can see 2 heartbeat packets at any time, and will connect - was one after the other

                    if (buffer.Length == 0)
                        buffer = getHeartBeat();

                    // incase we are in setup mode
                    //serial_port.WriteLine("planner\rgcs\r");

                    System.Threading.Thread.Sleep(1);

                    if (buffer1.Length == 0)
                        buffer1 = getHeartBeat();


                    if (buffer.Length > 0 || buffer1.Length > 0)
                        hbseen = true;

                    count++;

                    if (buffer.Length > 5 && buffer1.Length > 5 && buffer[3] == buffer1[3] && buffer[4] == buffer1[4])
                    {
                        apm_heartbeat_t hb = buffer.ByteArrayToStructure<apm_heartbeat_t>(6);

                        if (hb.type != (byte)Mars_Rover_APM.APM.APM_TYPE.GCS)
                        {

                            //APM.apmversion = hb.mavlink_version;
                            APM.aptype = (APM_TYPE)hb.type;
                            APM.apname = (APM_AUTOPILOT)hb.autopilot;

                            setAPType();

                            APM.sysid = buffer[3];
                            APM.compid = buffer[4];
                            APM.recvpacketcount = buffer[2];
                           // Logger.WriteLine("ID sys {0} comp {1}", APM.sysid, APM.compid);
                            break;
                        }
                    }

                }

                countDown.Stop();

                Logger.WriteLine("Getting Params.. (sysid " + APM.sysid + " compid " + APM.compid + ") ");

                if (getParams)
                {
                    getParamListBG();
                }
            }
            catch (Exception e)
            {
                try
                {
                    serial_port.Close();
                }
                catch { }
                giveComport = false;
                //if (string.IsNullOrEmpty(progressWorkerEventArgs.ErrorMessage))
                //    progressWorkerEventArgs.ErrorMessage = "Connect Failed";
                Logger.WriteLine(e.Message);
                throw;
            }
            //frmProgressReporter.Close();
            giveComport = false;
            Logger.WriteLine("Done open " + APM.sysid + " " + APM.compid);
            packetslost = 0;
            synclost = 0;
        }

        /// <summary>
        /// Get param list from apm
        /// </summary>
        /// <returns></returns>
        private Hashtable getParamListBG()
        {
            giveComport = true;
            List<int> indexsreceived = new List<int>();

            // clear old
            APM.param = new Hashtable();

            int retrys = 6;
            int param_count = 0;
            int param_total = 1;

            apm_param_request_list_t req = new apm_param_request_list_t();
            req.target_system = APM.sysid;
            req.target_component = APM.compid;

            generatePacket((byte)APM_MSG_ID.PARAM_REQUEST_LIST, req);

            DateTime start = DateTime.Now;
            DateTime restart = DateTime.Now;

            DateTime lastmessage = DateTime.MinValue;

            //hires.Stopwatch stopwatch = new hires.Stopwatch();
            int packets = 0;

            do
            {
                // 4 seconds between valid packets
                if (!(start.AddMilliseconds(4000) > DateTime.Now))
                {
                    // try getting individual params
                    for (short i = 0; i <= (param_total - 1); i++)
                    {
                        if (!indexsreceived.Contains(i))
                        {
                            // prevent dropping out of this get params loop
                            try
                            {
                                //TODO: Get here
                                GetParam(i);
                                param_count++;
                                indexsreceived.Add(i);
                            }
                            catch
                            {
                                try
                                {
                                    // GetParam(i);
                                    // param_count++;
                                    // indexsreceived.Add(i);
                                }
                                catch { }
                                // fail over to full list
                                //break;
                            }
                        }
                    }

                    if (retrys == 4)
                    {
                        requestDatastream(Mars_Rover_APM.APM.APM_DATA_STREAM.ALL, 1);
                    }

                    if (retrys > 0)
                    {
                        //Logger.WriteLine("getParamList Retry {0} sys {1} comp {2}", retrys, APM.sysid, APM.compid);
                        generatePacket((byte)APM_MSG_ID.PARAM_REQUEST_LIST, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    giveComport = false;
                    if (packets > 0 && param_total == 1)
                    {
                        throw new Exception("Timeout on read - getParamList\n" + packets + " Packets where received, but no paramater packets where received\n");
                    }
                    if (packets == 0)
                    {
                        throw new Exception("Timeout on read - getParamList\nNo Packets where received\n");
                    }

                    throw new Exception("Timeout on read - getParamList\nReceived: " + indexsreceived.Count + " of " + param_total + " after 6 retrys\n\nPlease Check\n1. Link Speed\n2. Link Quality\n3. Hardware hasn't hung");
                }

                //Logger.WriteLine(DateTime.Now.Millisecond + " gp0 ");

                byte[] buffer = readPacket();
                //Logger.WriteLine(DateTime.Now.Millisecond + " gp1 ");
                if (buffer.Length > 5)
                {
                    packets++;
                    // stopwatch.Start();
                    if (buffer[5] == (byte)APM_MSG_ID.PARAM_VALUE)
                    {
                        restart = DateTime.Now;
                        start = DateTime.Now;

                        apm_param_value_t par = buffer.ByteArrayToStructure<apm_param_value_t>(6);

                        // set new target
                        param_total = (par.param_count);

                        string paramID = System.Text.ASCIIEncoding.ASCII.GetString(par.param_id);

                        int pos = paramID.IndexOf('\0');
                        if (pos != -1)
                        {
                            paramID = paramID.Substring(0, pos);
                        }

                        // check if we already have it
                        if (indexsreceived.Contains(par.param_index))
                        {
                            Logger.WriteLine("Already got " + (par.param_index) + " '" + paramID + "'");
                            continue;
                        }

                        //Logger.WriteLine(DateTime.Now.Millisecond + " gp2 ");

                        //if (!MainV2.MONO)
                            //Logger.WriteLine(DateTime.Now.Millisecond + " got param " + (par.param_index) + " of " + (par.param_count) + " name: " + paramID);

                        //Logger.WriteLine(DateTime.Now.Millisecond + " gp2a ");

                        APM.param[paramID] = (par.param_value);

                        //Logger.WriteLine(DateTime.Now.Millisecond + " gp2b ");

                        param_count++;
                        indexsreceived.Add(par.param_index);

                        param_types[paramID] = (APM_PARAM_TYPE)par.param_type;

                        //Logger.WriteLine(DateTime.Now.Millisecond + " gp3 ");

                        // we hit the last param - lets escape eq total = 176 index = 0-175
                        if (par.param_index == (param_total - 1))
                            start = DateTime.MinValue;
                    }
                    //stopwatch.Stop();
                    // Logger.WriteLine("Time elapsed: {0}", stopwatch.Elapsed);
                    // Logger.WriteLine(DateTime.Now.Millisecond + " gp4 " + BaseStream.BytesToRead);
                }
             
            } while (indexsreceived.Count < param_total);

            if (indexsreceived.Count != param_total)
            {
                throw new Exception("Missing Params");
            }
            giveComport = false;
            return APM.param;
        }

        public float GetParam(string name)
        {
            return GetParam(name, -1);
        }

        public float GetParam(short index)
        {
            return GetParam("", index);
        }

        /// <summary>
        /// Get param by either index or name
        /// </summary>
        /// <param name="index"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        internal float GetParam(string name = "", short index = -1)
        {
            if (name == "" && index == -1)
                return 0;

            Logger.WriteLine("GetParam name: " + name + " index: " + index);

            giveComport = true;
            byte[] buffer;

            apm_param_request_read_t req = new apm_param_request_read_t();
            req.target_system = APM.sysid;
            req.target_component = APM.compid;
            req.param_index = index;
            if (index == -1)
            {
                req.param_id = System.Text.ASCIIEncoding.ASCII.GetBytes(name);
                Array.Resize(ref req.param_id, 16);
            }

            generatePacket((byte)APM_MSG_ID.PARAM_REQUEST_READ, req);

            DateTime start = DateTime.Now;
            int retrys = 3;

            while (true)
            {
                if (!(start.AddMilliseconds(700) > DateTime.Now))
                {
                    if (retrys > 0)
                    {
                       Logger.WriteLine("GetParam Retry " + retrys);
                        generatePacket((byte)APM_MSG_ID.PARAM_REQUEST_READ, req);
                        start = DateTime.Now;
                        retrys--;
                        continue;
                    }
                    giveComport = false;
                    throw new Exception("Timeout on read - GetParam");
                }

                buffer = readPacket();
                if (buffer.Length > 5)
                {
                    if (buffer[5] == (byte)APM_MSG_ID.PARAM_VALUE)
                    {
                        giveComport = false;

                        apm_param_value_t par = buffer.ByteArrayToStructure<apm_param_value_t>(6);

                        string st = System.Text.ASCIIEncoding.ASCII.GetString(par.param_id);

                        int pos = st.IndexOf('\0');

                        if (pos != -1)
                        {
                            st = st.Substring(0, pos);
                        }

                        // not the correct id
                        if (!(par.param_index == index || st == name))
                        {
                            //Logger.WriteLine("Wrong Answer {0} - {1} - {2}    --- '{3}' vs '{4}'", par.param_index, ASCIIEncoding.ASCII.GetString(par.param_id), par.param_value, ASCIIEncoding.ASCII.GetString(req.param_id).TrimEnd(), st);
                            continue;
                        }

                        // update table
                        APM.param[st] = par.param_value;

                        param_types[st] = (APM_PARAM_TYPE)par.param_type;

                        Logger.WriteLine(DateTime.Now.Millisecond + " got param " + (par.param_index) + " of " + (par.param_count) + " name: " + st);

                        return par.param_value;
                    }
                }
            }
        }

        public void requestDatastream(Mars_Rover_APM.APM.APM_DATA_STREAM id, byte hzrate)
        {

            double pps = 0;

            switch (id)
            {
                case Mars_Rover_APM.APM.APM_DATA_STREAM.ALL:

                    break;
                case Mars_Rover_APM.APM.APM_DATA_STREAM.EXTENDED_STATUS:
                    if (packetspersecondbuild[(byte)APM_MSG_ID.SYS_STATUS] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[(byte)APM_MSG_ID.SYS_STATUS];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case Mars_Rover_APM.APM.APM_DATA_STREAM.EXTRA1:
                    if (packetspersecondbuild[(byte)APM_MSG_ID.ATTITUDE] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[(byte)APM_MSG_ID.ATTITUDE];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case Mars_Rover_APM.APM.APM_DATA_STREAM.EXTRA2:
                    if (packetspersecondbuild[(byte)APM_MSG_ID.VFR_HUD] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[(byte)APM_MSG_ID.VFR_HUD];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case Mars_Rover_APM.APM.APM_DATA_STREAM.EXTRA3:
                    if (packetspersecondbuild[(byte)APM_MSG_ID.AHRS] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[(byte)APM_MSG_ID.AHRS];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case Mars_Rover_APM.APM.APM_DATA_STREAM.POSITION:
                    if (packetspersecondbuild[(byte)APM_MSG_ID.GLOBAL_POSITION_INT] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[(byte)APM_MSG_ID.GLOBAL_POSITION_INT];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case Mars_Rover_APM.APM.APM_DATA_STREAM.RAW_CONTROLLER:
                    if (packetspersecondbuild[(byte)APM_MSG_ID.RC_CHANNELS_SCALED] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[(byte)APM_MSG_ID.RC_CHANNELS_SCALED];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case Mars_Rover_APM.APM.APM_DATA_STREAM.RAW_SENSORS:
                    if (packetspersecondbuild[(byte)APM_MSG_ID.RAW_IMU] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[(byte)APM_MSG_ID.RAW_IMU];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
                case Mars_Rover_APM.APM.APM_DATA_STREAM.RC_CHANNELS:
                    if (packetspersecondbuild[(byte)APM_MSG_ID.RC_CHANNELS_RAW] < DateTime.Now.AddSeconds(-2))
                        break;
                    pps = packetspersecond[(byte)APM_MSG_ID.RC_CHANNELS_RAW];
                    if (hzratecheck(pps, hzrate))
                    {
                        return;
                    }
                    break;
            }

            //packetspersecond[temp[5]];

            if (pps == 0 && hzrate == 0)
            {
                return;
            }


            //Logger.WriteLine("Request stream {0} at {1} hz", Enum.Parse(typeof(APM_DATA_STREAM), id.ToString()), hzrate);
            getDatastream(id, hzrate);
        }

        // returns true for ok
        bool hzratecheck(double pps, int hzrate)
        {

            if (hzrate == 0 && pps == 0)
            {
                return true;
            }
            else if (hzrate == 1 && pps >= 0.5 && pps <= 2)
            {
                return true;
            }
            else if (hzrate == 3 && pps >= 2 && hzrate < 5)
            {
                return true;
            }
            else if (hzrate == 10 && pps > 5 && hzrate < 15)
            {
                return true;
            }
            else if (hzrate > 15 && pps > 15)
            {
                return true;
            }

            return false;

        }

        void getDatastream(Mars_Rover_APM.APM.APM_DATA_STREAM id, byte hzrate)
        {
            apm_request_data_stream_t req = new apm_request_data_stream_t();
            req.target_system = APM.sysid;
            req.target_component = APM.compid;

            req.req_message_rate = hzrate;
            req.start_stop = 1; // start
            req.req_stream_id = (byte)id; // id

            // send each one twice.
            generatePacket((byte)APM_MSG_ID.REQUEST_DATA_STREAM, req);
            generatePacket((byte)APM_MSG_ID.REQUEST_DATA_STREAM, req);
        }

        public byte[] getHeartBeat()
        {
            DateTime start = DateTime.Now;
            int readcount = 0;
            while (true)
            {
                byte[] buffer = readPacket();
                readcount++;
                if (buffer.Length > 5)
                {
                    //log.Info("getHB packet received: " + buffer.Length + " btr " + serial_port.BytesToRead + " type " + buffer[5] );
                    if (buffer[5] == (byte)APM_MSG_ID.HEARTBEAT)
                    {
                        apm_heartbeat_t hb = buffer.ByteArrayToStructure<apm_heartbeat_t>(6);

                        if (hb.type != (byte)Mars_Rover_APM.APM.APM_TYPE.GCS)
                        {
                            return buffer;
                        }
                    }
                }
                if (DateTime.Now > start.AddMilliseconds(2200) || readcount > 200) // was 1200 , now 2.2 sec
                    return new byte[0];
            }
        }

        private void generatePacket(byte messageType, object indata)
        {
            Logger.WriteLine("Generating Packet for APM...");

            if (!serial_port.IsOpen)
            {
                Logger.WriteLine("Serial Port is not open!");
                return;
            }

            lock (objlock)
            {
                byte[] data;

                data = APMUtil.StructureToByteArray(indata);

                byte[] packet = new byte[data.Length + 6 + 2];

                packet[0] = 254;
                packet[1] = (byte)data.Length;
                packet[2] = (byte)packetcount;

                packetcount++;

                packet[3] = 255; // this is always 255 - MYGCS
                packet[4] = 190; // (byte)MAV_COMPONENT.MAV_COMP_ID_MISSIONPLANNER;
                packet[5] = messageType;

                int i = 6;
                foreach (byte b in data)
                {
                    packet[i] = b;
                    i++;
                }

                ushort checksum = APMCRC.crc_calculate(packet, packet[1] + 6);

                checksum = APMCRC.crc_accumulate(APM_MESSAGE_CRCS[messageType], checksum);

                byte ck_a = (byte)(checksum & 0xFF); ///< High byte
                byte ck_b = (byte)(checksum >> 8); ///< Low byte
                                                   ///
                packet[i] = ck_a;
                i += 1;
                packet[i] = ck_b;
                i += 1;

                if (serial_port.IsOpen)
                {
                    serial_port.Write(packet, 0, i);

                    //Notifies all subscribed observers about the arrival of the specified element in the sequence.
                    _bytesSentSubj.OnNext(i);
                }

            }
        }

        public byte[] readPacket()
        {
            byte[] buffer = new byte[260];
            int count = 0;
            int length = 0;
            int readcount = 0;
            lastbad = new byte[2];

            serial_port.ReadTimeout = 1200; // 1200 ms between chars - the gps detection requires this.

            DateTime start = DateTime.Now;

            lock (readlock)
            {
                while (serial_port.IsOpen)
                {
                    try
                    {
                        if (readcount > 300)
                        {
                            Logger.WriteLine("APM readpacket No valid apm packets");
                            break;
                        }
                        readcount++;

                            DateTime to = DateTime.Now.AddMilliseconds(serial_port.ReadTimeout);
                 
                            while (serial_port.IsOpen && serial_port.BytesToRead <= 0)
                            {
                                if (DateTime.Now > to)
                                {
                                    //Logger.WriteLine("APM: 1 wait time out btr {0} len {1}", serial_port.BytesToRead, length);
                                    throw new Exception("Timeout");
                                }
                                System.Threading.Thread.Sleep(1);
                                
                                if (debug)
                                    Logger.WriteLine(DateTime.Now.Millisecond + " SR0b " + serial_port.BytesToRead);
                            }
                            
                            if (debug)
                                Logger.WriteLine(DateTime.Now.Millisecond + " SR1a " + serial_port.BytesToRead);
                            
                        if (serial_port.IsOpen)
                            {
                                serial_port.Read(buffer, count, 1);
                            }
                        
                        if (debug)    
                            Logger.WriteLine(DateTime.Now.Millisecond + " SR1b " + serial_port.BytesToRead);
                        
                    }
                    catch (Exception e) { Logger.WriteLine("AOM readpacket read error: " + e.ToString()); break; }

                    // check if looks like a mavlink packet and check for exclusions and write to console
                    if (buffer[0] != 254)
                    {
                        if (buffer[0] >= 0x20 && buffer[0] <= 127 || buffer[0] == '\n' || buffer[0] == '\r')
                        {
                            // check for line termination
                            if (buffer[0] == '\r' || buffer[0] == '\n')
                            {
                                // check new line is valid
                                if (buildplaintxtline.Length > 3)
                                    plaintxtline = buildplaintxtline;

                                // reset for next line
                                buildplaintxtline = "";
                            }
                            Console.Write((char)buffer[0]);
                            buildplaintxtline += (char)buffer[0];
                        }
                        _bytesReceivedSubj.OnNext(1);
                        count = 0;
                        lastbad[0] = lastbad[1];
                        lastbad[1] = buffer[0];
                        buffer[1] = 0;
                        continue;
                    }
                    // reset count on valid packet
                    readcount = 0;

                    if (debug)
                        Logger.WriteLine(DateTime.Now.Millisecond + " SR2 " + serial_port.BytesToRead);

                    // check for a header
                    if (buffer[0] == 254)
                    {
                        // if we have the header, and no other chars, get the length and packet identifiers
                        if (count == 0)
                        {
                            DateTime to = DateTime.Now.AddMilliseconds(serial_port.ReadTimeout);

                            while (serial_port.IsOpen && serial_port.BytesToRead < 5)
                            {
                                if (DateTime.Now > to)
                                {
                                    //Logger.WriteLine("APM: 2 wait time out btr {0} len {1}", serial_port.BytesToRead, length);
                                    throw new Exception("Timeout");
                                }
                                System.Threading.Thread.Sleep(1);
                                
                                if (debug)
                                    Logger.WriteLine(DateTime.Now.Millisecond + " SR0b " + serial_port.BytesToRead);
                            }
                            int read = serial_port.Read(buffer, 1, 5);
                            count = read;
                        }

                        // packet length
                        length = buffer[1] + 6 + 2 - 2; // data + header + checksum - U - length
                        if (count >= 5)
                        {
                            if (APM.sysid != 0)
                            {
                                if (APM.sysid != buffer[3] || APM.compid != buffer[4])
                                {
                                    if (buffer[3] == '3' && buffer[4] == 'D')
                                    {
                                        // this is a 3dr radio rssi packet
                                    }
                                    else
                                    {
                                       //Logger.WriteLine("APM Bad Packet (not addressed to this APM) got {0} {1} vs {2} {3}", buffer[3], buffer[4], APM.sysid, APM.compid);
                                        return new byte[0];
                                    }
                                }
                            }

                            try
                            {
                                    DateTime to = DateTime.Now.AddMilliseconds(serial_port.ReadTimeout);

                                    while (serial_port.IsOpen && serial_port.BytesToRead < (length - 4))
                                    {
                                        if (DateTime.Now > to)
                                        {
                                            //Logger.WriteLine("APM: 3 wait time out btr {0} len {1}", serial_port.BytesToRead, length);
                                            break;
                                        }
                                        System.Threading.Thread.Sleep(1);
                                    }
                                    if (serial_port.IsOpen)
                                    {
                                        int read = serial_port.Read(buffer, 6, length - 4);              
                                    }
                                
                                count = length + 2;
                            }
                            catch { break; }
                            break;
                        }
                    }

                    count++;
                    if (count == 299)
                        break;
                }

                if (debug)
                    Logger.WriteLine(DateTime.Now.Millisecond + " SR3 " + serial_port.BytesToRead);
            }// end readlock

            Array.Resize<byte>(ref buffer, count);

            _bytesReceivedSubj.OnNext(buffer.Length);

            if (packetlosttimer.AddSeconds(5) < DateTime.Now)
            {
                packetlosttimer = DateTime.Now;
                packetslost = (packetslost * 0.8f);
                packetsnotlost = (packetsnotlost * 0.8f);
            }
           
            //MAV.cs.linkqualitygcs = (ushort)((packetsnotlost / (packetsnotlost + packetslost)) * 100.0);

            //if (bpstime.Second != DateTime.Now.Second && !logreadmode && serial_port.IsOpen)
            //{
            //    Console.Write("bps {0} loss {1} left {2} mem {3}      \n", bps1, synclost, serial_port.BytesToRead, System.GC.GetTotalMemory(false) / 1024 / 1024.0);
            //    bps2 = bps1; // prev sec
            //    bps1 = 0; // current sec
            //    bpstime = DateTime.Now;
            //}

            //bps1 += buffer.Length;

            //bps = (bps1 + bps2) / 2;

            //if (buffer.Length >= 5 && (buffer[3] == 255 || buffer[3] == 253)) // gcs packet
            //{
            //    getWPsfromstream(ref buffer);
            //    return buffer;// new byte[0];
            //}

            ushort crc = APMCRC.crc_calculate(buffer, buffer.Length - 2);

            if (buffer.Length > 5 && buffer[0] == 254)
            {
                crc = APMCRC.crc_accumulate(APM_MESSAGE_CRCS[buffer[5]], crc);
            }

            if (buffer.Length > 5 && buffer[1] != APM_MESSAGE_LENGTHS[buffer[5]])
            {
                if (APM_MESSAGE_LENGTHS[buffer[5]] == 0) // pass for unknown packets
                {

                }
                else
                {
                    //Logger.WriteLine("APM Bad Packet (Len Fail) len {0} pkno {1}", buffer.Length, buffer[5]);
                    if (buffer.Length == 11 && buffer[0] == 'U' && buffer[5] == 0)
                    {
                        string message = "APM 0.9 Heartbeat, Please upgrade your AP, This planner is for APM 1.0\n\n";
                        throw new Exception(message);
                    }
                    return new byte[0];
                }
            }

            if (buffer.Length < 5 || buffer[buffer.Length - 1] != (crc >> 8) || buffer[buffer.Length - 2] != (crc & 0xff))
            {
                int packetno = -1;
                if (buffer.Length > 5)
                {
                    packetno = buffer[5];
                }
                if (packetno != -1 && buffer.Length > 5 && APM_MESSAGE_INFO[packetno] != null)
                    //Logger.WriteLine("APM Bad Packet (crc fail) len {0} crc {1} vs {4} pkno {2} {3}", buffer.Length, crc, packetno, APM_MESSAGE_INFO[packetno].ToString(), BitConverter.ToUInt16(buffer, buffer.Length - 2));
                return new byte[0];
            }

            try
            {
                if ((buffer[0] == 'U' || buffer[0] == 254) && buffer.Length >= buffer[1])
                {
                    if (buffer[3] == '3' && buffer[4] == 'D')
                    {

                    }
                    else
                    {
                        byte packetSeqNo = buffer[2];
                        int expectedPacketSeqNo = ((APM.recvpacketcount + 1) % 0x100);

                        {
                            if (packetSeqNo != expectedPacketSeqNo)
                            {
                                synclost++; // actualy sync loss's
                                int numLost = 0;

                                if (packetSeqNo < ((APM.recvpacketcount + 1))) // recvpacketcount = 255 then   10 < 256 = true if was % 0x100 this would fail
                                {
                                    numLost = 0x100 - expectedPacketSeqNo + packetSeqNo;
                                }
                                else
                                {
                                    numLost = packetSeqNo - APM.recvpacketcount;
                                }
                                packetslost += numLost;
                                WhenPacketLost.OnNext(numLost);

                                //Logger.WriteLine("lost pkts new seqno {0} pkts lost {1}", packetSeqNo, numLost);
                            }

                            packetsnotlost++;

                            APM.recvpacketcount = packetSeqNo;
                        }
                        WhenPacketReceived.OnNext(1);
                        // Logger.WriteLine(DateTime.Now.Millisecond);
                    }

                    if (double.IsInfinity(packetspersecond[buffer[5]]))
                        packetspersecond[buffer[5]] = 0;

                    packetspersecond[buffer[5]] = (((1000 / ((DateTime.Now - packetspersecondbuild[buffer[5]]).TotalMilliseconds) + packetspersecond[buffer[5]]) / 2));

                    packetspersecondbuild[buffer[5]] = DateTime.Now;

                    // store packet history
                    lock (objlock)
                    {
                        APM.packets[buffer[5]] = buffer;
                        APM.packetseencount[buffer[5]]++;
                    }

                    if (buffer[5] == (byte)Mars_Rover_APM.APM.APM_MSG_ID.STATUSTEXT) // status text
                    {
                        var msg = APM.packets[(byte)Mars_Rover_APM.APM.APM_MSG_ID.STATUSTEXT].ByteArrayToStructure<Mars_Rover_APM.APM.apm_statustext_t>(6);

                        byte sev = msg.severity;

                        string logdata = Encoding.ASCII.GetString(buffer, 7, buffer.Length - 7);
                        int ind = logdata.IndexOf('\0');
                        if (ind != -1)
                            logdata = logdata.Substring(0, ind);
                        Logger.WriteLine(DateTime.Now + " " + logdata);
                    }

                    // set ap type
                    if (buffer[5] == (byte)Mars_Rover_APM.APM.APM_MSG_ID.HEARTBEAT)
                    {
                        apm_heartbeat_t hb = buffer.ByteArrayToStructure<apm_heartbeat_t>(6);

                        if (hb.type != (byte)Mars_Rover_APM.APM.APM_TYPE.GCS)
                        {
                            //apmlinkversion = hb.mavlink_version;
                            APM.aptype = (APM_TYPE)hb.type;
                            APM.apname = (APM_AUTOPILOT)hb.autopilot;
                            setAPType();
                        }
                    }

                    getWPsfromstream(ref buffer);
                    try
                    {
                        // full rw from mirror stream
                        if (serial_mirror != null && serial_mirror.IsOpen)
                        {
                            serial_mirror.Write(buffer, 0, buffer.Length);

                            while (serial_mirror.BytesToRead > 0)
                            {
                                byte[] buf = new byte[1024];

                                int len = serial_mirror.Read(buf, 0, buf.Length);

                                serial_port.Write(buf, 0, len);
                            }
                        }
                    }
                    catch { }
                }
            }
            catch { }

            if (buffer[3] == '3' && buffer[4] == 'D')
            {
                // dont update last packet time for 3dr radio packets
            }
            else
            {
                lastvalidpacket = DateTime.Now;
            }

            //            Console.Write((DateTime.Now - start).TotalMilliseconds.ToString("00.000") + "\t" + temp.Length + "     \r");

            if (debug)
                Logger.WriteLine(DateTime.Now.Millisecond + " SR4 " + serial_port.BytesToRead);

            return buffer;
        }

        public void setAPType()
        {
            switch (APM.apname)
            {
                case APM_AUTOPILOT.ARDUPILOTMEGA:
                    switch (APM.aptype)
                    {
                        case Mars_Rover_APM.APM.APM_TYPE.FIXED_WING:
                            //APM.cs.firmware = MainV2.Firmwares.ArduPlane;
                            break;
                        case Mars_Rover_APM.APM.APM_TYPE.QUADROTOR:
                            //APM.cs.firmware = MainV2.Firmwares.ArduCopter2;
                            break;
                        case Mars_Rover_APM.APM.APM_TYPE.TRICOPTER:
                            //APM.cs.firmware = MainV2.Firmwares.ArduCopter2;
                            break;
                        case Mars_Rover_APM.APM.APM_TYPE.HEXAROTOR:
                            //APM.cs.firmware = MainV2.Firmwares.ArduCopter2;
                            break;
                        case Mars_Rover_APM.APM.APM_TYPE.OCTOROTOR:
                            //APM.cs.firmware = MainV2.Firmwares.ArduCopter2;
                            break;
                        case Mars_Rover_APM.APM.APM_TYPE.HELICOPTER:
                            //APM.cs.firmware = MainV2.Firmwares.ArduCopter2;
                            break;
                        case Mars_Rover_APM.APM.APM_TYPE.GROUND_ROVER:
                            //APM.cs.firmware = MainV2.Firmwares.ArduRover;
                            break;
                        default:
                            break;
                    }
                    break;
                case APM_AUTOPILOT.UDB:
                    switch (APM.aptype)
                    {
                        case Mars_Rover_APM.APM.APM_TYPE.FIXED_WING:
                            //APM.cs.firmware = MainV2.Firmwares.ArduPlane;
                            break;
                    }
                    break;
                case APM_AUTOPILOT.GENERIC:
                    switch (APM.aptype)
                    {
                        case Mars_Rover_APM.APM.APM_TYPE.FIXED_WING:
                            //APM.cs.firmware = MainV2.Firmwares.Ateryx;
                            break;
                    }
                    break;
            }
        }

        private void CleanupUpdateWorker()
        {
            //tokenSource.Cancel();
            if (update_thread != null && update_thread.ThreadState != ThreadState.Unstarted)
            {
                update_thread.Join();
                update_thread = null;
            }
        }

        private void UpdateWorker()
        {
           /* while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    if (!serial_port.IsOpen)
                        serial_port.Open();

                    Dictionary<Devices, int> state = queue.Dequeue(tokenSource.Token);

                    // Write Motor Values //
                    
                    serial_port.Write("#" + 0 + " P" + GetMotorValue(state, MotorChannel.Channel0) + " S" + 750 + " \r");
                    serial_port.Write("#" + 1 + " P" + GetMotorValue(state, MotorChannel.Channel1) + " S" + 750 + " \r");
                    serial_port.Write("#" + 2 + " P" + GetMotorValue(state, MotorChannel.Channel2) + " S" + 750 + " \r");
                    serial_port.Write("#" + 3 + " P" + GetMotorValue(state, MotorChannel.Channel3) + " S" + 750 + " \r");
                    serial_port.Write("#" + 4 + " P" + GetMotorValue(state, MotorChannel.Channel4) + " S" + 750 + " \r");
                    serial_port.Write("#" + 5 + " P" + GetMotorValue(state, MotorChannel.Channel5) + " S" + 750 + " \r");

                    // Write Steering Servo Values //
                    serial_port.Write("#" + 16 + " P" + GetSteeringValue(state, SteeringServoChannel.Channel16) + " S" + 750 + " \r");
                    serial_port.Write("#" + 17 + " P" + GetSteeringValue(state, SteeringServoChannel.Channel17) + " S" + 750 + " \r");
                    serial_port.Write("#" + 18 + " P" + GetSteeringValue(state, SteeringServoChannel.Channel18) + " S" + 750 + " \r");
                    serial_port.Write("#" + 19 + " P" + GetSteeringValue(state, SteeringServoChannel.Channel19) + " S" + 750 + " \r");
                  

                }
                catch (TimeoutException ex)
                {
                    //send stop values!
                    Logger.WriteLine("APM: " + ex.Message);
                    //E_Stop();
                }
                catch (InvalidOperationException ex)
                {
                    //port is not open!
                    //todo : how to handle?
                    Logger.WriteLine("SERIAL PORT NOT OPEN: " + this.apmConfig.COM_Port.ToString());
                }
                catch (OperationCanceledException ex)
                {
                    Logger.WriteLine("APM Controller: " + ex.Message);
                    //E_Stop();
                    break;
                }
                catch (Exception ex)
                {
                    // log
                }
            } 
          */
        }

                /// <summary>
        /// Used to extract mission from log file - both sent or received
        /// </summary>
        /// <param name="buffer">packet</param>
        void getWPsfromstream(ref byte[] buffer)
        {
            if (buffer[5] == (byte)APM_MSG_ID.MISSION_COUNT)
            {
                // clear old
                APM.wps.Clear();
            }

            if (buffer[5] == (byte)Mars_Rover_APM.APM.APM_MSG_ID.MISSION_ITEM)
            {
                apm_mission_item_t wp = buffer.ByteArrayToStructure<apm_mission_item_t>(6);

                if (wp.current == 2)
                {
                    // guide mode wp
                    APM.GuidedMode = wp;
                }
                else
                {
                    APM.wps[wp.seq] = wp;
                }

                //Logger.WriteLine("WP # {7} cmd {8} p1 {0} p2 {1} p3 {2} p4 {3} x {4} y {5} z {6}", wp.param1, wp.param2, wp.param3, wp.param4, wp.x, wp.y, wp.z, wp.seq, wp.command);
            }

            if (buffer[5] == (byte)APM_MSG_ID.RALLY_POINT)
            {
                apm_rally_point_t rallypt = buffer.ByteArrayToStructure<apm_rally_point_t>(6);

                APM.rallypoints[rallypt.idx] = rallypt;

                //Logger.WriteLine("RP # {0} {1} {2} {3} {4}", rallypt.idx, rallypt.lat,rallypt.lng,rallypt.alt, rallypt.break_alt);
            }

            if (buffer[5] == (byte)APM_MSG_ID.FENCE_POINT)
            {
               apm_fence_point_t fencept = buffer.ByteArrayToStructure<apm_fence_point_t>(6);

                APM.fencepoints[fencept.idx] = fencept;
            }
        }

        int GetMotorValue(Dictionary<Devices, int> state, MotorChannel channel)
        {
            int val = 0;
            /*
            MotorOutputSettings outputSettings;
            if (motor_channel_map.TryGetValue(channel, out outputSettings))
            {
                if (state.TryGetValue(outputSettings.Device, out val))
                {
                    int high = outputSettings.PWM_Map.PWM_High;
                    int low = outputSettings.PWM_Map.PWM_Low;
                    int stop = outputSettings.StopValue;
                    if (val < 0)
                        val = (int)((double)(stop - low)) / 100 * val + stop;
                    else if (val > 0)
                        val = (int)((double)(high - stop)) / 100 * val + stop;
                    else
                        val = stop;
                }
                else
                {
                    val = outputSettings.StopValue;
                }
            }
            else
            { //value not in configuration, return motor stop value
                val = outputSettings.StopValue;
            }
            return val;
        }

        int GetSteeringValue(Dictionary<Devices, int> state, SteeringServoChannel channel)
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
                    val = -val;
                    if (val < 0)
                        val = (int)((double)(stop - low)) / 45 * val + stop;
                    else if (val > 0)
                        val = (int)((double)(high - stop)) / 45 * val + stop;
                    else
                        val = stop;
                }
                else
                {
                    val = 0;
                }
            }
            else
            { //value not in configuration, return motor stop value
                val = 0;
            }
             */
            return val;
        }

        public void EnqueueState(Dictionary<Devices, int> state)
        {
            try
            {
                //queue.Enqueue(state);
            }
            catch (Exception ex)
            {
                Logger.WriteLine("APM Enqueue: " + ex.Message);
            }
        }

        public void EnqueueCmd(string cmd)
        {
            try
            {
                //queueCmd.Enqueue(cmd);
            }
            catch (Exception ex)
            {
                Logger.WriteLine("APM Enqueue: " + ex.Message);
            }
        }

            
    }


}
