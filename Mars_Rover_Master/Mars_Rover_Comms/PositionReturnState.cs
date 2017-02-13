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
        [XmlAttribute("Lat")]
        public string Lat;

        [XmlAttribute("Lon")]
        public string Lon;

        [XmlAttribute("Heading")]
        public string Heading;

        public PositionReturnState()
        {
        }

    }
}
