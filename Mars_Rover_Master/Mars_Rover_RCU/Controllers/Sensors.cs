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
        private Boolean HeadlightsEnabled = false;

        public Sensors()
        {
            sensorData = new String[6];
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
        /// Returns the state of the headlights
        /// </summary>
        public bool headlightsEnabled()
        {
            return HeadlightsEnabled;
        }

        /// <summary>
        /// Sends the command to the arduino to enable the headlights
        /// </summary>
        public void enableHeadlights()
        {
            try
            {
                Arduino.Write("1");
                HeadlightsEnabled = true;
            }
            catch(Exception E)
            {
                Logger.WriteLine("Error occurred attempting to enable headlight." + E.Message);
            }
        }

        /// <summary>
        /// Sends the command to the arduino to disable the headlights
        /// </summary>
        public void disableHeadlights()
        {
            try
            {
                Arduino.Write("0");
                HeadlightsEnabled = false;
            }
            catch (Exception E)
            {
                Logger.WriteLine("Error occurred attempting to disable headlight." + E.Message);
            }
        }

        /// <summary>
        /// Handles the event of a comm error occuring.
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void ErrorReceived(object sender, SerialErrorReceivedEventArgs e)
        {
            
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
            try
            {
                String input = Arduino.ReadLine();

                //This will fill the array with the sensor data
                sensorData = input.Split(',');

                //Calling the PID to update
                if (Program._PID.enabled)
                {
                    Program._PID.update();
                }
            }
            catch(Exception E)
            {
                Logger.WriteLine("The following error occurred attempting to read sensor data:" + E.Message);
            }
        }
    }
    }