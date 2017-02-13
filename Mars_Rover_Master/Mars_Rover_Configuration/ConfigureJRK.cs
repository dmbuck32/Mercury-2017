using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{
    [Serializable]
    public class ConfigureJRK
    {
        private SteeringServoOutputSettings steeringSettings1;
        //private SteeringServoOutputSettings steeringSettings2;
        //private SteeringServoOutputSettings steeringSettings3;
        //private SteeringServoOutputSettings steeringSettings4;
        private int timeout;

        private ConfigureJRK() //Only for serialization
        {
        }

        public ConfigureJRK(int timeout)
        {
            this.timeout = timeout;
            this.steeringSettings1 = new SteeringServoOutputSettings();

        }

       // public ConfigureJRK(int timeout, SteeringServoOutputSettings steeringSettings1, SteeringServoOutputSettings steeringSettings2,
       //     SteeringServoOutputSettings steeringSettings3, SteeringServoOutputSettings steeringSettings4)
        public ConfigureJRK(int timeout, SteeringServoOutputSettings steeringSettings1)
        {
            if (timeout <= 0)
                throw new ArgumentOutOfRangeException("timeout");
            if (steeringSettings1 == null)
                throw new ArgumentOutOfRangeException("steeringSettings");

            this.timeout = timeout;
            this.steeringSettings1 = steeringSettings1;
            //this.steeringSettings2 = steeringSettings2;
            //this.steeringSettings3 = steeringSettings3;
            //this.steeringSettings4 = steeringSettings4;
        }


        public SteeringServoOutputSettings SteeringServoOutput1
        {
            get { return steeringSettings1; }
            set { steeringSettings1 = value; }
        }

        //public SteeringServoOutputSettings SteeringServoOutput2
        //{
        //    get { return steeringSettings2; }
        //    set { steeringSettings2 = value; }
        //}

        //public SteeringServoOutputSettings SteeringServoOutput3
        //{
        //    get { return steeringSettings3; }
        //    set { steeringSettings3 = value; }
        //}

        //public SteeringServoOutputSettings SteeringServoOutput4
        //{
        //    get { return steeringSettings4; }
        //    set { steeringSettings4 = value; }
        //}

        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }


    }
}
