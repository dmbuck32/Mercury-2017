using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mars_Rover_Comms
{
    public class DataArgs : EventArgs
    {
        private byte[] data;

        public DataArgs(byte[] data)
        {
            this.data = data;
        }

        public byte[] Data
        {
            get { return this.data; }
        }
    }
}
