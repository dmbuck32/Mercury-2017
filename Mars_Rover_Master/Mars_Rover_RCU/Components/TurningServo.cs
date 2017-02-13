using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_RCU.Components
{
    class TurningServo : Servo
    {

        private Byte _channel;

        public TurningServo()
        {
            _channel = 1;
        }

        public TurningServo(Mars_Rover_Configuration.Devices device)
        {
            _channel = (byte)device;
  
        }

        //Methods

        public override Byte getChannel()
        {
            return _channel;
        }

    }
}
