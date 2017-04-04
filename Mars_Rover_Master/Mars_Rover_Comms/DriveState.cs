using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_Comms
{
    public class DriveState
    {
        [XmlAttribute("Control")]
        public short Control; //1 control; 0 no control

        [XmlAttribute("Radius")]
        public short Radius;

        [XmlAttribute("Speed")]
        public short Speed;

        [XmlAttribute("LeftSpeed")]
        public short LeftSpeed;

        [XmlAttribute("RightSpeed")]
        public short RightSpeed;

        [XmlAttribute("ArmSpeed")]
        public short ArmSpeed; //arm lift uses a driving motor
        /*
        [XmlAttribute("ElbowSpeed")]
        public short ElbowSpeed;

        [XmlAttribute("ShoulderSpeed")]
        public short ShoulderSpeed;
        */
        [XmlAttribute("ScoopIn")]
        public bool ScoopIn;

        [XmlAttribute("ScoopOut")]
        public bool ScoopOut;
        
        [XmlAttribute("Headlights")]
        public bool Headlights;

        [XmlAttribute("WallFollow")]
        public bool WallFollow;

        [XmlAttribute("FrontStopArmUp")]
        public bool FrontStopArmUp;

        [XmlAttribute("FrontStopArmDown")]
        public bool FrontStopArmDown;

        [XmlAttribute("ControllerControl")]
        public bool controllerControl;

        [XmlAttribute("PIDEnable")]
        public bool PIDEnable;

        public DriveState()
        {
        }
    }
}
