using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars_Rover_Configuration;

namespace Mars_Rover_RCU.Components
{
    abstract class Servo
    {
        abstract public Byte getChannel();      
    }

}
