using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.IO.Ports;
using System.Diagnostics;

namespace Mars_Rover_RCU.Controllers
{
    public class Roomba
    {
        private string COM_Port;
        private int Baud_Rate = 115200;
        private SerialPort serial_comms;
        private int headlights = 0;
        private ushort[] sensors = new ushort[10];
        private Boolean autobrake = false;

        public Roomba(String RoombaCom)
        {
            COM_Port = RoombaCom;
            serial_comms = new SerialPort(COM_Port, Baud_Rate);
            serial_comms.Open();
            byte[] initializeMessage = { 128, 132, 144, 0, 0, 0 };
            serial_comms.Write(initializeMessage, 0, initializeMessage.Length);
        }

        public void drive(int direction, int velocity)
        {
            readSensors();
            byte[] driveMessage = new byte[5];
            int radius = 0;
            
            if (direction == 0) //forward
            {
                radius = 32768;
                driveMessage[3] = ConvertToHighByte(radius);
                driveMessage[4] = ConvertToLowByte(radius);
            }
            else if (direction == 1) //right
            {
                radius = -1;
                driveMessage[3] = ConvertToHighByte(radius);
                driveMessage[4] = ConvertToLowByte(radius);
            }
            else if (direction == 2) //left
            {
                radius = 1;
                driveMessage[3] = ConvertToHighByte(radius);
                driveMessage[4] = ConvertToLowByte(radius);
            }
            else if (direction == 3) //backwards
            {
                velocity *= -1;
                radius = 32768;
                driveMessage[3] = ConvertToHighByte(radius);
                driveMessage[4] = ConvertToLowByte(radius);
            }
            else if (direction == 4) //veer left forwards
            {
                driveMessage[3] = 00;
                driveMessage[4] = 20;
            }
            else if (direction == 5) //veer right forwards
            {
                driveMessage[3] = 255;
                driveMessage[4] = 236;
            }
            else if (direction == 6) //veer left backwards
            {
                velocity *= -1;
                driveMessage[3] = 00;
                driveMessage[4] = 20;
            }
            else if (direction == 7) //veer right backwards
            {
                velocity *= -1;
                driveMessage[3] = 255;
                driveMessage[4] = 236;
            }
            
            driveMessage[0] = 137;
            driveMessage[1] = ConvertToHighByte(velocity);
            driveMessage[2] = ConvertToLowByte(velocity);
            
            serial_comms.Write(driveMessage, 0, driveMessage.Length);
        }

        private byte ConvertToHighByte(int IntValue)
        {
            return (byte)(IntValue >> 8);
        }

        private byte ConvertToLowByte(int IntValue)
        {
            return (byte)(IntValue & 255);
        }



        public void directDrive(int leftSpeed, int rightSpeed)
        {
            byte[] driveMessage = new byte[5];
            driveMessage[0] = 145;
            if (rightSpeed < 255)
            {
                driveMessage[1] = 0;
                driveMessage[2] = (byte)rightSpeed;
            }
            else if (rightSpeed > 255 && rightSpeed < 500)
            {
                driveMessage[1] = 1;
                driveMessage[2] = (byte)(rightSpeed - 256);
            }
            if (leftSpeed < 255)
            {
                driveMessage[3] = 0;
                driveMessage[4] = (byte)leftSpeed;
            }
            else if (leftSpeed > 255 && leftSpeed < 500)
            {
                driveMessage[3] = 1;
                driveMessage[4] = (byte)(leftSpeed - 256);
            }
        }
        
        public void displayText()
        {
            byte[] message = { 164, 48, 49, 50, 51 };
            serial_comms.Write(message, 0, message.Length);
        }

        public void LOS()
        {
            byte[] LOS_LED = { 139, 4, 255, 200, 164, 32, 32, 32, 32 };
            byte[] beep = { 140, 0, 1, 62, 32, 141, 0 };
            byte[] LOS_Display = { 164, 76, 79, 83, 32 };
            serial_comms.Write(LOS_LED, 0, LOS_LED.Length);
            serial_comms.Write(LOS_Display, 0, LOS_Display.Length);
            serial_comms.Write(beep, 0, beep.Length);
            stop();
        }

        public void connected()
        {
           byte[] message = { 164, 54, 48, 48, 68, 139, 4, 0, 0 };
           serial_comms.Write(message, 0, message.Length);
        }

        public void shutdownRoomba()
        {
            byte[] message = { 128 };
            serial_comms.Write(message, 0, message.Length);
            serial_comms.Close();
        }

        public void stop()
        {
            byte[] stopMessage = { 137, 0, 0, 80, 0 };
            serial_comms.Write(stopMessage, 0, stopMessage.Length);
        }

        public void powerHeadlights(int light)
        {
            if (light == 1 && headlights == 0)
            {
                byte[] PowerMessage = { 144, 0, 80, 0 };
                serial_comms.Write(PowerMessage, 0, PowerMessage.Length);
                headlights = 1;
            }
            else if (light == 0 && headlights == 1)
            {
                byte[] PowerMessage = { 144, 0, 0, 0 };
                serial_comms.Write(PowerMessage, 0, PowerMessage.Length);
                headlights = 0;
            }
        }

        public void readSensors()
        {
            if (serial_comms.IsOpen)
            {
                byte[] sensorMessage = { 149, 10, 46, 47, 48, 49, 50, 51, 28, 29, 30, 31 };
                serial_comms.Write(sensorMessage, 0, sensorMessage.Length);
                byte[] readSensors = new byte[20];
                serial_comms.Read(readSensors, 0, 20);
                Array.Reverse(readSensors);

                sensors[0] = BitConverter.ToUInt16(readSensors, 8);//Right
                sensors[1] = BitConverter.ToUInt16(readSensors, 10);//Right Front
                sensors[2] = BitConverter.ToUInt16(readSensors, 12);//Right Center
                sensors[3] = BitConverter.ToUInt16(readSensors, 14);//Left Center
                sensors[4] = BitConverter.ToUInt16(readSensors, 16);//Left Front
                sensors[5] = BitConverter.ToUInt16(readSensors, 18);//Left

                sensors[6] = BitConverter.ToUInt16(readSensors, 0);//Cliff right
                sensors[7] = BitConverter.ToUInt16(readSensors, 2);//Cliff Front right
                sensors[8] = BitConverter.ToUInt16(readSensors, 4);//Cliff Front left
                sensors[9] = BitConverter.ToUInt16(readSensors, 6);//Cliff left

            }
        }

        public void closeRoomba()
        {
            byte[] closeMessage = { 128 };
            serial_comms.Write(closeMessage, 0, closeMessage.Length);
            serial_comms.Close();
        }

        public ushort[] getSensors()
        {
            return sensors;
        }

        public Boolean getAutobrake()
        {
            return this.autobrake;
        }

        public void setAutobrake(Boolean val)
        {
            this.autobrake = val;
        }
    }
}
