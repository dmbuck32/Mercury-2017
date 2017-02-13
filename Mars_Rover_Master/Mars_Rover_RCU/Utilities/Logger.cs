using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO;

namespace Mars_Rover_RCU.Utilities
{
    public static class Logger {
            
       private static object loglock = new object();
       //private static ASCIIEncoding asciiencoding = new ASCIIEncoding();
       //private static StringBuilder log = new StringBuilder("Logging started at: " + DateTime.Now); //TODO save to file??
       private static StringBuilder outgoing = new StringBuilder("");

    public static void WriteLine(string msg) {

        //First, output the message on robot screen
        Console.Out.WriteLine(msg);
        

        lock (loglock)
        {
            outgoing.AppendLine(msg);
        }

        //Program.client.SendToOCUServer(msg);
    }

    public static void WriteLine(string msg, int i, ushort target, ushort speed, byte accel, ushort position)
    {

        //First, output the message on robot screen
        Console.Out.WriteLine(msg, i, target, speed, accel, position);

        
        lock (loglock)
        {
            outgoing.AppendLine(msg);
        }

       // Program.client.SendToOCUServer(msg);
    }

    public static string getOutgoing()
    {
        lock (loglock)
        {
            string msg = outgoing.ToString();
            clearOutgoing();
            return msg;
        }
    }

    private static void clearOutgoing()
    {
        outgoing.Clear();
    }
}
}
