using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_Comms
{
    public class CmdLineState
    {
        [XmlAttribute("CmdLine")]
        public string CmdLine;

        public CmdLineState()
        {
        }
    }
}
