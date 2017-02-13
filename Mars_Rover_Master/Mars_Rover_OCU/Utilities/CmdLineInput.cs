using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Mars_Rover_Comms;

namespace Mars_Rover_OCU.Utilities
{
    public static class CmdLineInput
    {
        public static CmdLineState newState = new CmdLineState() { CmdLine = "" };

        public static void CmdLineEvent(string newCmd)
        {
            newState = new CmdLineState() { CmdLine = newCmd };
        }

        public static CmdLineState GetCmdLineState()
        {
            Console.WriteLine(newState.CmdLine);
            return newState;
        }

        public static CmdLineState loadAPMSettings()
        {
            newState = new CmdLineState() { CmdLine = "welcome"};
            return newState;
        }
    }
}
