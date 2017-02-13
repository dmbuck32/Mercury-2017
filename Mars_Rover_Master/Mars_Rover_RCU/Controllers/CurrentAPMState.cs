using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text;
using System.ComponentModel;
using Mars_Rover_RCU.Utilities;
using Utility;
using System.Collections;

namespace Mars_Rover_RCU.Controllers
{

    public enum Firmwares
    {
        ArduPlane,
        ArduCopter2,
        ArduRover,
        Ateryx
    }

    public class CurrentAPMState : ICloneable
    {

        // multipliers
        public float multiplierdist = 1;
        internal string DistanceUnit = "";
        public float multiplierspeed = 1;
        internal string SpeedUnit = "";

        // position
        public double lat { get; set; }
        public double lng { get; set; }

        public float gpsstatus { get; set; }
        public float gpshdop { get; set; }

        public float satcount { get; set; }

        // mag
        public float mx { get; set; }
        public float my { get; set; }
        public float mz { get; set; }
        public float magfield { get { return (float)Math.Sqrt(Math.Pow(mx, 2) + Math.Pow(my, 2) + Math.Pow(mz, 2)); } }
     

        public bool failsafe { get; set; }
        public float rxrssi { get; set; }
        

        //nav state
        public float nav_bearing { get; set; }
        public float target_bearing { get; set; }
        public float wp_dist { get { return (_wpdist * multiplierdist); } set { _wpdist = value; } }
        public float xtrack_error { get; set; }
        public float wpno { get; set; }
        public string mode { get; set; }
        uint _mode = 99999;

        float _wpdist;
        float _aspd_error;

        //message
        public List<string> messages { get; set; }
        internal string message { get { if (messages.Count == 0) return ""; return messages[messages.Count - 1]; } }
        public string messageHigh { get {return _messagehigh;} set {_messagehigh = value;} }
        private string _messagehigh;
        public DateTime messageHighTime { get; set; }

        //battery
        public float battery_voltage { get { return _battery_voltage; } set { _battery_voltage = value; } }
        private float _battery_voltage;
        public float battery_remaining { get { return _battery_remaining; } set { _battery_remaining = value; if (_battery_remaining < 0 || _battery_remaining > 100) _battery_remaining = 0; } }
        private float _battery_remaining;
       
        public float current { get { return _current; } set { if (value < 0) return; if (_lastcurrent == DateTime.MinValue) _lastcurrent = datetime; battery_usedmah += (value * 1000) * (float)(datetime - _lastcurrent).TotalHours; _current = value; _lastcurrent = datetime; } }
        private float _current;
        private DateTime _lastcurrent = DateTime.MinValue;
       
        public float battery_usedmah { get; set; }

        public PointLatLngAlt HomeLocation = new PointLatLngAlt();
        public PointLatLngAlt MovingBase = null;
        PointLatLngAlt _trackerloc = new PointLatLngAlt();
        public PointLatLngAlt TrackerLocation { get { if (_trackerloc.Lng != 0) return _trackerloc; return HomeLocation; } set { _trackerloc = value; } }

        public float DistToHome
        {
            get
            {
                if (lat == 0 && lng == 0)
                    return 0;

                // shrinking factor for longitude going to poles direction
                double rads = Math.Abs(TrackerLocation.Lat) * 0.0174532925;
                double scaleLongDown = Math.Cos(rads);
                double scaleLongUp = 1.0f / Math.Cos(rads);

                //DST to Home
                double dstlat = Math.Abs(TrackerLocation.Lat - lat) * 111319.5;
                double dstlon = Math.Abs(TrackerLocation.Lng - lng) * 111319.5 * scaleLongDown;
                return (float)Math.Sqrt((dstlat * dstlat) + (dstlon * dstlon)) * multiplierdist;
            }
        }

        // sensor offsets
        public int mag_ofs_x { get; set; }
        public int mag_ofs_y { get; set; }
        public int mag_ofs_z { get; set; }

        // current firmware
        public Firmwares firmware = Firmwares.ArduRover;
        public float freemem { get; set; }
        public float load { get; set; }
        public float brklevel { get; set; }
        public bool armed { get; set; }

        // stats
        public ushort packetdropremote { get; set; }
        public ushort linkqualitygcs { get; set; }
        public float hwvoltage { get; set; }
        public ushort i2cerrors { get; set; }

        // requested stream rates
        public byte rateposition { get; set; }
        public byte ratestatus { get; set; }
        public byte ratesensors { get; set; }

        // reference
        public DateTime datetime { get; set; }
        public DateTime gpstime { get; set; }

        public bool connected { get { return (Program._APM.BaseStream.IsOpen); } }

        bool useLocation = false;
        internal bool batterymonitoring = false;

        internal bool MONO = false;

        public CurrentAPMState()
        {
            ResetInternals();

            var t = Type.GetType("Mono.Runtime");
            MONO = (t != null);
        }

        public void ResetInternals()
        {
            mode = "Unknown";
            _mode = 99999;
            messages = new List<string>();
            useLocation = false;
            rateposition = 3;
            ratestatus = 2;
            ratesensors = 2;
            datetime = DateTime.MinValue;
            battery_usedmah = 0;
            _lastcurrent = DateTime.MinValue;
        }

        const float rad2deg = (float)(180 / Math.PI);
        const float deg2rad = (float)(1.0 / rad2deg);

        private DateTime lastupdate = DateTime.Now;

        private DateTime lastsecondcounter = DateTime.Now;
        
        private PointLatLngAlt lastpos = new PointLatLngAlt();

        public string GetNameandUnit(string name)
        {
            string desc = name;
            try
            {
                var typeofthing = typeof(CurrentAPMState).GetProperty(name);
                if (typeofthing != null)
                {
                    var attrib = typeofthing.GetCustomAttributes(false);
                    //if (attrib.Length > 0)
                    //    desc = ((Attributes.DisplayTextAttribute)attrib[0]).Text;
                }
            }
            catch { }

            if (desc.Contains("(dist)"))
            {
                desc = desc.Replace("(dist)", "(" + Program._APM.MAV.cs.DistanceUnit + ")");
            }
            else if (desc.Contains("(speed)"))
            {
                desc = desc.Replace("(speed)", "(" + Program._APM.MAV.cs.SpeedUnit + ")");
            }

            return desc;
        }

        /// <summary>
        /// use for main serial port only
        /// </summary>
        /// <param name="bs"></param>
        //public void UpdateCurrentSettings(System.Windows.Forms.BindingSource bs)
        //{
        //   UpdateCurrentSettings(bs, false, Program._APM);
        //}

        //public void UpdateCurrentSettings(System.Windows.Forms.BindingSource bs, bool updatenow, MAVLinkInterface mavinterface)
        //{
        //    lock (this)
        //    {

        //        if (DateTime.Now > lastupdate.AddMilliseconds(50) || updatenow) // 20 hz
        //        {
        //            lastupdate = DateTime.Now;

        //            //check if valid mavinterface
        //            if (mavinterface != null && mavinterface.packetsnotlost != 0)
        //                linkqualitygcs = (ushort)((mavinterface.packetsnotlost / (mavinterface.packetsnotlost + mavinterface.packetslost)) * 100.0);

        //            if (DateTime.Now.Second != lastsecondcounter.Second)
        //            {
        //                lastsecondcounter = DateTime.Now;

        //                if (lastpos.Lat != 0 && lastpos.Lng != 0 && armed)
        //                {
        //                    lastpos = new PointLatLngAlt(lat, lng, 0, "");
        //                }

        //            }

        //            if (mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.STATUSTEXT] != null) // status text 
        //            {

        //                var msg = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.STATUSTEXT].ByteArrayToStructure<MAVLink.mavlink_statustext_t>(6);

        //                                        /*
        //                enum gcs_severity {
        //                    SEVERITY_LOW=1,
        //                    SEVERITY_MEDIUM,
        //                    SEVERITY_HIGH,
        //                    SEVERITY_CRITICAL
        //                };
        //                                         */
                  
        //                mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.STATUSTEXT] = null;
        //            }

        //            byte[] bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.FENCE_STATUS];

        //            if (bytearray != null)
        //            {
        //                var fence = bytearray.ByteArrayToStructure<MAVLink.mavlink_fence_status_t>(6);

        //                if (fence.breach_status != (byte)MAVLink.FENCE_BREACH.NONE)
        //                {
        //                    // fence breached
        //                    messageHigh = "Fence Breach";
        //                    messageHighTime = DateTime.Now;
        //                }

        //                mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.FENCE_STATUS] = null;
        //            }

        //            bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.SYSTEM_TIME];
        //            if (bytearray != null)
        //            {
        //                var systime = bytearray.ByteArrayToStructure<MAVLink.mavlink_system_time_t>(6);

        //                DateTime date1 = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

        //                date1 = date1.AddMilliseconds(systime.time_unix_usec / 1000);

        //                gpstime = date1;
        //            }

        //            bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.HWSTATUS];

        //            if (bytearray != null)
        //            {
        //                var hwstatus = bytearray.ByteArrayToStructure<MAVLink.mavlink_hwstatus_t>(6);

        //                hwvoltage = hwstatus.Vcc / 1000.0f;
        //                i2cerrors = hwstatus.I2Cerr;
        //            }


        //            bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.HEARTBEAT];
        //            if (bytearray != null)
        //            {
        //                var hb = bytearray.ByteArrayToStructure<MAVLink.mavlink_heartbeat_t>(6);

        //                if (hb.type == (byte)MAVLink.MAV_TYPE.GCS)
        //                {
        //                    // skip gcs hb's
        //                    // only happens on log playback - and shouldnt get them here
        //                }
        //                else
        //                {
        //                    armed = (hb.base_mode & (byte)MAVLink.MAV_MODE_FLAG.SAFETY_ARMED) == (byte)MAVLink.MAV_MODE_FLAG.SAFETY_ARMED;

        //                    // for future use
        //                    bool landed = hb.system_status == (byte)MAVLink.MAV_STATE.STANDBY;

        //                    failsafe = hb.system_status == (byte)MAVLink.MAV_STATE.CRITICAL;

        //                    string oldmode = mode;

        //                    if ((hb.base_mode & (byte)MAVLink.MAV_MODE_FLAG.CUSTOM_MODE_ENABLED) != 0)
        //                    {
        //                        // prevent running thsi unless we have to
        //                        if (_mode != hb.custom_mode)
        //                        {
        //                            List<KeyValuePair<int, string>> modelist = Common.getModesList(this);

        //                            bool found = false;

        //                            foreach (KeyValuePair<int, string> pair in modelist)
        //                            {
        //                                if (pair.Key == hb.custom_mode)
        //                                {
        //                                    mode = pair.Value.ToString();
        //                                    _mode = hb.custom_mode;
        //                                    found = true;
        //                                    break;
        //                                }
        //                            }

        //                            if (!found)
        //                            {
        //                                //log.Warn("Mode not found bm:" + hb.base_mode + " cm:" + hb.custom_mode);
        //                                _mode = hb.custom_mode;
        //                            }
        //                        }
        //                    }
        //                }
        //            }


        //            bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.SYS_STATUS];
        //            if (bytearray != null)
        //            {
        //                var sysstatus = bytearray.ByteArrayToStructure<MAVLink.mavlink_sys_status_t>(6);

        //                load = (float)sysstatus.load / 10.0f;

        //                battery_voltage = (float)sysstatus.voltage_battery / 1000.0f;
        //                battery_remaining = (float)sysstatus.battery_remaining;
        //                current = (float)sysstatus.current_battery / 100.0f;

        //                packetdropremote = sysstatus.drop_rate_comm;

        //                Mavlink_Sensors sensors_enabled = new Mavlink_Sensors(sysstatus.onboard_control_sensors_enabled);
        //                Mavlink_Sensors sensors_health = new Mavlink_Sensors(sysstatus.onboard_control_sensors_health);
        //                Mavlink_Sensors sensors_present = new Mavlink_Sensors(sysstatus.onboard_control_sensors_present);

        //                if (sensors_health.gps != sensors_enabled.gps)
        //                {
        //                    messageHigh = "Bad GPS Health";
        //                    messageHighTime = DateTime.Now;
        //                }
        //                else if (sensors_health.gyro != sensors_enabled.gyro)
        //                {
        //                    messageHigh = "Bad Gyro Health";
        //                    messageHighTime = DateTime.Now;
        //                }
        //                else if (sensors_health.accelerometer != sensors_enabled.accelerometer)
        //                {
        //                    messageHigh = "Bad Accel Health";
        //                    messageHighTime = DateTime.Now;
        //                }
        //                else if (sensors_health.compass != sensors_enabled.compass)
        //                {
        //                    messageHigh = "Bad Compass Health";
        //                    messageHighTime = DateTime.Now;
        //                }
        //                else if (sensors_health.barometer != sensors_enabled.barometer)
        //                {
        //                    messageHigh = "Bad Baro Health";
        //                    messageHighTime = DateTime.Now;
        //                }
        //                else if (sensors_health.optical_flow != sensors_enabled.optical_flow)
        //                {
        //                    messageHigh = "Bad OptFlow Health";
        //                    messageHighTime = DateTime.Now;
        //                }
        //                else if (sensors_present.rc_receiver != sensors_enabled.rc_receiver)
        //                {
        //                    int reenable;
        //                    //messageHigh = "NO RC Receiver";
        //                    //messageHighTime = DateTime.Now;
        //                }
                        

        //                mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.SYS_STATUS] = null;
        //            }


        //            bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.SENSOR_OFFSETS];
        //            if (bytearray != null)
        //            {
        //                var sensofs = bytearray.ByteArrayToStructure<MAVLink.mavlink_sensor_offsets_t>(6);

        //                mag_ofs_x = sensofs.mag_ofs_x;
        //                mag_ofs_y = sensofs.mag_ofs_y;
        //                mag_ofs_z = sensofs.mag_ofs_z;

        //            }

        //            bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.GPS_RAW_INT];
        //            if (bytearray != null)
        //            {
        //                var gps = bytearray.ByteArrayToStructure<MAVLink.mavlink_gps_raw_int_t>(6);

        //                if (!useLocation)
        //                {
        //                    lat = gps.lat * 1.0e-7f;
        //                    lng = gps.lon * 1.0e-7f;
        //                }

        //                gpsstatus = gps.fix_type;
        //                //Console.WriteLine("gpsfix {0}",gpsstatus);

        //                gpshdop = (float)Math.Round((double)gps.eph / 100.0,2);

        //                satcount = gps.satellites_visible;

        //               //MAVLink.packets[(byte)MAVLink.MSG_NAMES.GPS_RAW] = null;
        //            }

        //            bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.GPS_STATUS];
        //            if (bytearray != null)
        //            {
        //                var gps = bytearray.ByteArrayToStructure<MAVLink.mavlink_gps_status_t>(6);
        //                satcount = gps.satellites_visible;
        //            }


        //            bytearray = mavinterface.MAV.packets[(byte)MAVLink.MAVLINK_MSG_ID.GLOBAL_POSITION_INT];
        //            if (bytearray != null)
        //            {
        //                var loc = bytearray.ByteArrayToStructure<MAVLink.mavlink_global_position_int_t>(6);

        //                useLocation = true;
        //                if (loc.lat == 0 && loc.lon == 0)
        //                {
        //                    useLocation = false;
        //                }
        //                else
        //                {
        //                    lat = loc.lat / 10000000.0f;
        //                    lng = loc.lon / 10000000.0f;
        //                }
        //            }
        //        }

        //    }
        //}

        public object Clone()
        {
            return this.MemberwiseClone();
        }


        public class Mavlink_Sensors
        {
            BitArray bitArray = new BitArray(32);

            public Mavlink_Sensors()
            {
            }

            public Mavlink_Sensors(uint p)
            {
                bitArray = new BitArray(new int[] { (int)p});
            }

            public bool gyro { get { return bitArray[0]; } set { bitArray[0] = value; } }
            public bool accelerometer { get { return bitArray[1]; } set { bitArray[1] = value; } }
            public bool compass { get { return bitArray[2]; } set { bitArray[2] = value; } }
            public bool barometer { get { return bitArray[3]; } set { bitArray[3] = value; } }
            public bool differential_pressure { get { return bitArray[4]; } set { bitArray[4] = value; } }
            public bool gps { get { return bitArray[5]; } set { bitArray[5] = value; } }
            public bool optical_flow { get { return bitArray[6]; } set { bitArray[6] = value; } }
            public bool unused_7 { get { return bitArray[7]; } set { bitArray[7] = value; } }
            public bool unused_8 { get { return bitArray[8]; } set { bitArray[8] = value; } }
            public bool unused_9 { get { return bitArray[9]; } set { bitArray[9] = value; } }
            public bool rate_control { get { return bitArray[10]; } set { bitArray[10] = value; } }
            public bool attitude_stabilization { get { return bitArray[11]; } set { bitArray[11] = value; } }
            public bool yaw_position { get { return bitArray[12]; } set { bitArray[12] = value; } }
            public bool altitude_control { get { return bitArray[13]; } set { bitArray[13] = value; } }
            public bool xy_position_control { get { return bitArray[14]; } set { bitArray[14] = value; } }
            public bool motor_control { get { return bitArray[15]; } set { bitArray[15] = value; } }
            public bool rc_receiver { get { return bitArray[16]; } set { bitArray[16] = value; } }

            public int Value
            {
                get
                {
                    int[] array = new int[1];
                    bitArray.CopyTo(array, 0);
                    return array[0];
                }
                set 
                {
                    bitArray = new BitArray(value);
                }
            }
        }
    }
}