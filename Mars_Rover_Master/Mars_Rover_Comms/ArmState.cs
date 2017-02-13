using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_Comms
{
    [Serializable]
    public class ArmState
    {
        [XmlAttribute("ArmData")]
        public int ArmData;
   
        public ArmState()
        {
        }
    }
}
