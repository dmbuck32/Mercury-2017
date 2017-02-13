using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{
    public enum APMParameter
    {

    }

    [Serializable]
    public class ConfigureAPM
    {
        private string com_port;
        private int timeout;
        private int readBufferSize;
        private bool dtrEnable;
        private int baudRate;

        //private List<MotorOutputSettings> motorSettings;
        //private List<SteeringServoOutputSettings> steeringSettings;


        //Constructors
        private ConfigureAPM() //Only for serialization
        {
        }

        public ConfigureAPM(int timeout, string com_port)
        {
            this.timeout = timeout;
            this.com_port = com_port;
        }


        //Properties

       /* public List<MotorOutputSettings> MotorOutputSettingsList
        {
            get { return motorSettings; }
            set { motorSettings = value; }
        }

        public List<SteeringServoOutputSettings> SteeringServoOuputSettingsList
        {
            get { return steeringSettings; }
            set { steeringSettings = value; }
        }
        */

        public int Baud_Rate
        {
            get { return baudRate; }
            set { baudRate = value; }
        }

        public string COM_Port
        {
            get { return com_port; }
            set { com_port = value; }
        }

        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }

        public int ReadBufferSize
        {
            get { return readBufferSize; }
            set { readBufferSize = value; }
        }

        public bool DTREnable
        {
            get { return dtrEnable; }
            set { dtrEnable = value; }
        }
        
    }
}
