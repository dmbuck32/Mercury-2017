using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{   
         [Serializable]
    public class ConfigureArm
    {
             private ArmSettings armSettings;

             ConfigureArm() //Only for serialization
             {
             }

             ConfigureArm(ArmSettings settings)
             {
                 armSettings = settings;
             }

             public ArmSettings ArmSettings
             {
                 get { return armSettings; }
                 set { armSettings = value; }
             }
    }
}
