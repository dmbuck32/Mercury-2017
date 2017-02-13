using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{

    public enum ControlSignalChannel
    {
        Channel23 = 23
    }

     [Serializable]
    public class ControlSignalOutputSettings
    {

        Devices device;
        ControlSignalChannel channel;
        PWMMapping mapping;
        int stop_value;

        //Constructors
        public ControlSignalOutputSettings()
        {
        }

        
        public ControlSignalOutputSettings(Devices dev, int stop_value, PWMMapping pwm_map, ControlSignalChannel channel)
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

        public ControlSignalChannel Channel
        {
            get { return channel; }
            set { channel = value; }
        }

        public int ControlValue
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
