using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_Comms
{
    public class TemperatureReturnState
    {
        [XmlAttribute("Temperature")]
        public short Temperature;

        [XmlAttribute("LeftIRSensor")]
        public short LeftIRSensor;

        [XmlAttribute("RightIRSensor")]
        public short RightIRSensor;

        [XmlAttribute("FrontLeftIRSensor")]
        public short FrontLeftIRSensor;

        [XmlAttribute("FrontRightIRSensor")]
        public short FrontRightIRSensor;

        [XmlAttribute("barRightIRSensor")]
        public short barRightIRSensor;

        [XmlAttribute("barFrontRightIRSensor")]
        public short barFrontRightIRSensor;

        [XmlAttribute("barCenterRightIRSensor")]
        public short barCenterRightIRSensor;

        [XmlAttribute("barCenterLeftIRSensor")]
        public short barCenterLeftIRSensor;

        [XmlAttribute("barFrontLeftIRSensor")]
        public short barFrontLeftIRSensor;

        [XmlAttribute("barLeftIRSensor")]
        public short barLeftIRSensor;

        public TemperatureReturnState()
        {
        }
    }  
}
