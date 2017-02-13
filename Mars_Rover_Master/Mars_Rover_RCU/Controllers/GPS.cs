using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using Mars_Rover_RCU.Utilities;

namespace Mars_Rover_RCU.Controllers
{
    public class GPS
    {
        private string _lat;
        private string _lng;
        private string _heading;
        private bool _isConnected;

        private static SerialPort _gpsPort;

        private object datalock = new object();

        public GPS()
        {
            _lat = "null";
            _lng = "null";
            _heading = "null";

            _gpsPort = new SerialPort();
            _gpsPort.PortName = "COM6";
            _gpsPort.BaudRate = 115200;


        }


        public string getHeading()
        {
          lock (datalock)
            {
                return _heading;
            }
        }

        public string getLat()
        {
            lock (datalock)
            {
                return _lat;
            }
        }

        public string getLng()
        {
            lock (datalock)
            {
                return _lng;
            }
        }

        public void startPort()
        {
            _gpsPort.Open();
            Logger.WriteLine("GPS Connected!!");
        }

        public void closePort()
        {
            _gpsPort.Close();
        }

        public void updatePosition()
        {
            parsePostiton(_gpsPort.ReadLine());
        }

        public void parsePostiton(string msg)
        {
            try
            {
                if (msg != null)
                {

                    char[] firstSeparator = { ' ' };
                    string[] msg_pair = msg.Split(firstSeparator);

                    foreach (string str in msg_pair)
                    {

                        char[] secondSeparator = { ':' };
                        string[] inner_msg_pair = str.Split(secondSeparator);

                        foreach (string str2 in inner_msg_pair)
                        {
                            if (inner_msg_pair[0].Equals("Lat"))
                            {
                                _lat = inner_msg_pair[1];
                                continue;
                            }

                            if (inner_msg_pair[0].Equals("Lon"))
                            {
                                _lng = inner_msg_pair[1];
                                continue;
                            }

                            if (inner_msg_pair[0].Equals("Head"))
                            {
                                _heading = inner_msg_pair[1].Replace("\r", "");
                                continue;
                            }

                        }

                    }

                }
            }
            catch { }
        }

    }
}
