using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pololu.Usc;
using Pololu.UsbWrapper;
using System.Diagnostics;
using System.Management;
using Mars_Rover_RCU.Utilities;
using System.IO.Ports;
namespace Mars_Rover_RCU.Controllers
{
    public class DriveController
    {
        private SerialPort Arduino;

        public DriveController()
        {
        }

        /// <summary>
        /// Opens the serial connection to the Arduino
        /// </summary>
        /// <returns>Returns the status of the connection attempt.</returns>
        public bool OpenConnection(string port)
        {
            try
            {
                this.Arduino = new SerialPort(port, 9600);
            }
            catch (System.IO.IOException)
            {
                Logger.WriteLine("Failed to connect to Drive Controller.");
                return false;
            }
            this.Arduino.Open();
            Logger.WriteLine("Drive Controller is open.");
            this.Arduino.ErrorReceived += ErrorReceived;
            return true;
        }

        public void setMotors(short leftSpeed, short rightSpeed)
        {
            Arduino.Write(leftSpeed.ToString());
            Arduino.Write(rightSpeed.ToString());
        }

        public void stopMotors()
        {
            Arduino.Write("1500");
            Arduino.Write("1500");
        }

        /// <summary>
        /// Handles the event of a comm error occuring.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            //print error received
            this.Arduino.Close();
        }
    }
}