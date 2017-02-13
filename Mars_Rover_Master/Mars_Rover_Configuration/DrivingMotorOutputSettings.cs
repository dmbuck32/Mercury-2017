using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{
    public enum DrivingMotorChannel
    {
        Channel1 = 1,
        Channel2 = 2,
        Channel0 = 0,//was 7
        Channel3 = 3,//was 8
        Channel9 = 9,
        Channel4 = 4, //was 10
     
    }

    [Serializable]
    public class DrivingMotorOutputSettings
    {
        Devices device;
        DrivingMotorChannel channel;
        PWMMapping mapping;
        int stop_value;

        //Constructors
        public DrivingMotorOutputSettings()
        {
        }

        public DrivingMotorOutputSettings(Devices dev, int stop_value, PWMMapping pwm_map, DrivingMotorChannel channel)
        {
            if (pwm_map == null)
                throw new AccessViolationException("pwm_map");

            this.device = dev;
            this.stop_value = stop_value;
            this.mapping = pwm_map;
            this.channel = channel;
        }

        //Properties
        public Devices Device
        {
            get { return device; }
            set { device = value; }
        }

        public DrivingMotorChannel Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        public int StopValue
        {
            get { return stop_value; }
            set { stop_value = value; }
        }

        public PWMMapping PWM_Map
        {
            get { return mapping; }
            set { mapping = value; }
        }

    }
}
