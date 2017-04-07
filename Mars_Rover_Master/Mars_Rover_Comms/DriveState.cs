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
        [XmlAttribute("LeftSpeed")]
        public short LeftSpeed; // 1000 - 2000

        [XmlAttribute("RightSpeed")]
        public short RightSpeed; // 1000 - 2000

        [XmlAttribute("radius")]
        public double radius; // -1 to 1

        [XmlAttribute("Mode")]
        public short Mode; // 0 -> Normal, 1 -> Rotate, 2 -> Translate, 3 -> Tank

        [XmlAttribute("ArmState")]
        public short ArmState; // 0 -> Shoulder, 1 -> Elbow, 2 -> wrist

        [XmlAttribute("shoulderPos")]
        public short shoulderPos; //464 - 2496

        [XmlAttribute("elbowPos")]
        public short elbowPos; //464 - 2496

        [XmlAttribute("wristPos")]
        public short wristPos; //800 - 2000

        [XmlAttribute("gripperPos")]
        public short gripperPos; // 0 or 1

        [XmlAttribute("Control")]
        public bool Control; //Tells if the OCU has taken control

        [XmlAttribute("Headlights")]
        public bool Headlights; // Headlight On or Off

        [XmlAttribute("goToHome")]
        public bool goToHome; // Macro for stowing away arm

        [XmlAttribute("goToSample")]
        public bool goToSample; // Macro for positioning arm to aquire sample

        [XmlAttribute("goToDeposit")]
        public bool goToDeposit; // Macro for positioning arm to deposit sample

        [XmlAttribute("usePID")]
        public bool usePID;

        [XmlAttribute("ControllerControl")]
        public bool controllerControl;  

        public DriveState()
        {
        }
    }
}
