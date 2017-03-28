using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pololu.Usc;
using Pololu.UsbWrapper;
using System.Diagnostics;
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

        /// <summary>
        /// Opens the serial connection to the Arduino
        /// </summary>
        /// <returns>Returns the status of the connection attempt.</returns>
        public bool OpenConnection()
        {

            //Figuring out which port the Arduino is on and connecting
            try
            {
                this.Arduino = new SerialPort("COM4", 9600);
                this.Arduino.Open();
            }
            catch (System.IO.IOException)
            {
                this.Arduino = new SerialPort("COM5", 9600);
                this.Arduino.Open();
            }

            if (this.Arduino.IsOpen)
            {
                this.Arduino.DtrEnable = true;
                this.Arduino.DataReceived += DataReceived;
                this.Arduino.ErrorReceived += ErrorReceived;
                return true;
            }
            else
            {
                return false;
            }
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