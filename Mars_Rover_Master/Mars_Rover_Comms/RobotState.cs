using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Comms
{
    public class RobotState
    {
        public DriveState DriveState;
        public CmdLineState CmdLineState;
        public LogState LogState;
        public ArmState ArmState;

        public RobotState()
        {       
            DriveState = new DriveState();
            CmdLineState = new CmdLineState();
            LogState = new LogState();
            ArmState = new ArmState();
        }
    }
}
