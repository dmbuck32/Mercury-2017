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
    public class ServoController
    {
        private SerialPort Arduino;

        // PWM Channels for Maestro
        private byte LeftMotors = 0;
        private byte RightMotors = 1;
        private byte RearRightServo = 2;
        private byte RearLeftServo = 3;
        private byte FrontRightServo = 4;
        private byte FrontLeftServo = 5;
        private byte LOS_LED = 6;
        private byte ShoulderServo = 8;
        private byte ElbowServo = 9;
        private byte WristServo = 10;
        private byte GripperServo = 11;

        // PWM multiplier for Pololu Maestro
        private readonly short PWM_Multiplier = 4;

        // Initial Values to make wheels straight
        private readonly short rearLeftInit = 1510;
        private readonly short rearRightInit = 1425;
        private readonly short frontLeftInit = 1441;
        private readonly short frontRightInit = 1520;

        // PWM Values for Gripper Open/Closed
        private const short gripperOpen = 1500;
        private const short gripperClosed = 1000;

        // PWM Values for Drive Channels
        private readonly short OFF = 1500;

        // PWM Values for LOS
        private short LOS_ON = 4000;
        private short LOS_OFF = 0;

        // PWM values for ARM Channels
        private short shoulderInit = 464;
        private short elbowInit = 1000;
        private short wristInit = 2000;

        public ServoController()
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
                Logger.WriteLine("Failed to connect to Servo Controller.");
                return false;
            }
            this.Arduino.Open();
            Logger.WriteLine("Servo Controller is open.");
            this.Arduino.ErrorReceived += ErrorReceived;
            return true;
        }

        public void initializeServos()
        {
            setTankMode();
            setLOS(false);
            armHomePos();
            closeGripper();
        }

        public void openGripper()
        {
            Arduino.Write("<" + GripperServo + "," + gripperOpen + ">");
        }

        public void closeGripper()
        {
            Arduino.Write("<" + GripperServo + "," + gripperClosed + ">");
        }

        public void setTankMode()
        {
            setTurningServos(frontLeftInit, frontRightInit, rearLeftInit, rearRightInit);
        }

        public void setTranslateMode()
        {
            setTurningServos(2416, 542, 533, 2400);
        }

        public void setRotateMode()
        {
            setTurningServos(1876, 1065, 1026, 1900);
        }

        public void armHomePos()
        {
            setArmServos(shoulderInit, elbowInit, wristInit);
        }

        public void setArmServos(short s, short e, short w)
        {
            Arduino.Write("<" + ShoulderServo + "," + s + ">");
            Arduino.Write("<" + ElbowServo + "," + e + ">");
            Arduino.Write("<" + WristServo + "," + w + ">");
        }

        public void setTurningServos(short fl, short fr, short rl, short rr)
        {
            Arduino.Write("<" + FrontLeftServo + "," + fl + ">");
            Arduino.Write("<" + FrontRightServo + "," + fr + ">");
            Arduino.Write("<" + RearLeftServo + "," + rl + ">");
            Arduino.Write("<" + RearRightServo + "," + rr + ">");
        }

        public void noControl()
        {
            Arduino.Write("<" + LOS_LED + "," + LOS_ON + ">");
            System.Threading.Thread.Sleep(10);
            Arduino.Write("<" + LOS_LED + "," + LOS_OFF + ">");
        }

        public void setLOS(bool LOS)
        {
            if (LOS)
            {
                Arduino.Write("<" + LOS_LED + "," + LOS_ON + ">");
            }
            else
            {
                Arduino.Write("<" + LOS_LED + "," + LOS_OFF + ">");
            }
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