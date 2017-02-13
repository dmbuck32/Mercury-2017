using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pololu.UsbWrapper;
using Pololu.Usc;

namespace MaestroTest
{
    class Program
    {
        
        static void Main(string[] args)
        {
            String userInput = "-1";
            String serialNum;
            Usc myDevice = null;

            Console.WriteLine("This is a test to control the Pololu mux...");
            Console.Write("Enter 1 to start the test or 'quit' to exit: ");
            userInput = Console.ReadLine();
            while (!userInput.Equals("quit"))
            {
                Console.WriteLine("Searching for devices...");

                // Get a list of all connected devices of this type.
                List<DeviceListItem> connectedDevices = Usc.getConnectedDevices();

                short cntr = -1;
                foreach (DeviceListItem device in connectedDevices)
                {
                    cntr++;
                    Console.WriteLine(cntr + ": " + device.serialNumber);                  
                }

                if (cntr == -1)
                {
                    Console.WriteLine("No devices found!");
                    break;
                }
                else
                {
                    Console.WriteLine(cntr+1 + " devices found. Select a device to connect to: ");
                }
                
                userInput = Console.ReadLine();
                serialNum = connectedDevices.ElementAt(Int32.Parse(userInput)).serialNumber;
               
                myDevice = new Usc(connectedDevices.ElementAt(Int32.Parse(userInput))); // Connect to the device.   
                Console.WriteLine("Connected!");
                    
                
                Console.Write("Enter a channel: ");
                String channel = Console.ReadLine();

                Console.WriteLine();

                Console.WriteLine("Enter a target value: ");
                String target = Console.ReadLine();

                myDevice.setTarget(Byte.Parse(channel), (UInt16)(UInt16.Parse(target) * 4));

                userInput = Console.ReadLine();

            }

            if(myDevice != null)
                myDevice.Dispose();   
        }

    }
}
