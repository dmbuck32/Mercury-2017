using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_Comms
{
    public class LogState
    {
        [XmlAttribute("Data")]
        public string Data;

        public LogState()
        {
        }
    }
}
