using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{
  
        public enum MotorChannel
        {
            Channel0 = 0,
            Channel1,
            Channel2,
            Channel3,
            Channel4,
            Channel5
        }


        [Serializable]
        public class RoboteqOutputSettings
        {
            Devices device;
            MotorChannel channel;
            PWMMapping mapping;
            int stop_value;

            //Constructors
            public RoboteqOutputSettings()
            {
            }

            public RoboteqOutputSettings(Devices dev, int stop_value, PWMMapping pwm_map, MotorChannel channel)
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

            public MotorChannel Channel
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


