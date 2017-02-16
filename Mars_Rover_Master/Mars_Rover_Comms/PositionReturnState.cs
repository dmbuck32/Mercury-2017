using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_Comms
{
    public class PositionReturnState
    {
        [XmlAttribute("frontDistance")]
        public string frontDistance;

        [XmlAttribute("leftDistance")]
        public string leftDistance;

        [XmlAttribute("rightDistance")]
        public string rightDistance;

        [XmlAttribute("rearDistance")]
        public string rearDistance;

        [XmlAttribute("frontAmbient")]
        public string frontAmbient;

        [XmlAttribute("leftAmbient")]
        public string leftAmbient;

        [XmlAttribute("rightAmbient")]
        public string rightAmbient;

        [XmlAttribute("rearAmbient")]
        public string rearAmbient;

        public PositionReturnState()
        {
        }

    }
}
