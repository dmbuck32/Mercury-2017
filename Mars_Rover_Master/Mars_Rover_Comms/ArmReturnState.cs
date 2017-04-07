using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_Comms
{
    public class ArmReturnState
    {
        [XmlAttribute("ArmFeedback")]
        public short ArmFeedback;

        [XmlAttribute("shoulderPos")]
        public short shoulderPos; //464 - 2496

        [XmlAttribute("elbowPos")]
        public short elbowPos; //464 - 2496

        [XmlAttribute("wristPos")]
        public short wristPos; //800 - 2000

        [XmlAttribute("gripperPos")]
        public short gripperPos; // 0 or 1

        public ArmReturnState()
        {
        }
    }
}
