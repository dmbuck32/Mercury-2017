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
    public class Sensors
    {
        private SerialPort Arduino;
        private String[] sensorData;

        public Sensors()
        {
            sensorData = new String[6];
        }

        // Method to find arduino port (haven't tested yet)
        private string AutodetectArduinoPort()
        {
            ManagementScope connectionScope = new ManagementScope();
            SelectQuery serialQuery = new SelectQuery("SELECT * FROM Win32_SerialPort");
            ManagementObjectSearcher searcher = new ManagementObjectSearcher(connectionScope, serialQuery);

            try
            {
                foreach (ManagementObject item in searcher.Get())
                {
                    string desc = item["Description"].ToString();
                    string deviceId = item["DeviceID"].ToString();

                    if (desc.Contains("Arduino"))
                    {
                        return deviceId;
                    }
                }
            }
            catch (ManagementException e)
            {
                /* Do Nothing */
            }

            return null;
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
                Logger.WriteLine("Failed to connect to Arduino.");
                return false;
            }
            this.Arduino.Open();
            Logger.WriteLine("Arduino is open.");
            this.Arduino.DtrEnable = true;
            this.Arduino.DataReceived += DataReceived;
            this.Arduino.ErrorReceived += ErrorReceived;
            return true;
        }

        /// <summary>
        /// Gets the current value of all the sensors.
        /// </summary>
        /// <returns>A 6 element array of each sensor's ambient and range measurement.</returns>
        public String[] getData()
        {
            return sensorData;
        }

        /// <summary>
        /// Handles the event of a comm error occuring.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            //print error received
            this.sensorData = null;
            this.Arduino.Close();
        }

        /// <summary>
        /// Parses the data received from the Arduino.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            String input = Arduino.ReadLine();

            //This will take the data string 
            sensorData = input.Split(',');
        }
    }
    }