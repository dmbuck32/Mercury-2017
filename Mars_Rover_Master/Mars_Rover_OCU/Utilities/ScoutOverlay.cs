using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace Mars_Rover_OCU.Utilities
{

    public class ScoutOverlay
    {
        [XmlArray("Markers")]
       public GMapMarkerSample[] ScoutSamples;

        public ScoutOverlay(){}
    }
}
