using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{
    [Serializable]
    public class ArmSettings
    {
        short _COMPort;

        //Constructors
        public ArmSettings()
        {
        }

        //Constructors
        public ArmSettings(short comport)
        {
            _COMPort = comport;
        }

        //Properties
        public short COMPort
        {
            get { return _COMPort; }
            set { _COMPort = value; }
        }

    }
}
