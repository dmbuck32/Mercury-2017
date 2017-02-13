using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{

    public enum SteeringServoChannel
    {
        Channel13 = 1,
        Channel14 = 2,
        Channel5 = 0, //arm servo was 15
        Channel16 = 3,
    }

    [Serializable]
    public class SteeringServoOutputSettings
    {
        Devices device;
        SteeringServoChannel channel;
        PWMMapping mapping;
        int stop_value;

        //Constructors
        public SteeringServoOutputSettings()
        {
        }

        public SteeringServoOutputSettings(Devices dev, int stop_value, PWMMapping pwm_map, SteeringServoChannel channel)
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

        public SteeringServoChannel Channel
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
