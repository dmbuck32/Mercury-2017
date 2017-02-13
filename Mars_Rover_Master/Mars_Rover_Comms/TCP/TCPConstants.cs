using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mars_Rover_Comms.TCP
{
    public static class TCPConstants
    {
        public static readonly byte StartToken = 0xFE;
        public static readonly byte StopToken = 0xFF;
    }
}
