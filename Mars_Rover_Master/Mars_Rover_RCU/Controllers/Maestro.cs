using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pololu.Usc;
using Pololu.UsbWrapper;
using System.Diagnostics;
using Mars_Rover_RCU.Utilities;

namespace Mars_Rover_RCU.Controllers
{
    public class Maestro
    {
        private Usc usc = null;
        /*
        //Claw Controls
        private byte clawChannel = 2;
        private ushort[] clawTargetValues = {7000, 8500}; //Closed to open
        private int clawTargetIndex = 0;

        //Elbow Controls
        private byte elbowChannel = 1;
        //private ushort[] elbowTargetValues = { 8000, 7750, 7500, 7250, 7000, 6750, 6500, 6250, 6000, 5750, 5500, 5250, 5000, 4750, 4500, 4250 }; //down to up
        private ushort[] elbowTargetValues = { 8100, 7000, 6000, 5000, 4400 };
        private int elbowTargetIndex = 0;
        
        //Shoulder Controls
        private byte shoulderChannel = 0;
        //private ushort[] shoulderTargetValues = { 4250, 4500, 4750, 5000, 5250, 5500, 5750, 6000, 6250, 6500, 6750, 7000, 7250, 7500, 7750, 8000 }; //down to up
        private ushort[] shoulderTargetValues = { 4300, 5000, 6000, 7000, 7700 };
        private int shoulderTargetIndex = 0;
        
        //Trigger Controls
        private byte triggerChannel = 3;
        private ushort off = 5045;
        private ushort on = 6000;
        */
        private byte gripperChannel = 0;
        private byte elbowChannel = 1;
        private byte shoulderChannel = 2;
        private byte leftDriveChannel = 6;
        private byte rightDriveChannel = 7;
        private byte frontLeftSteeringChannel = 8;
        private byte backLeftSteeringChannel = 9;
        private byte frontRightSteeringChannel = 10;
        private byte backRightSteeringChannel = 11;
        public Maestro()
        {
            var devicelist = Usc.getConnectedDevices();
            foreach (DeviceListItem d in Usc.getConnectedDevices())
            {
                if (d.serialNumber == devicelist[0].serialNumber)
                {
                    usc = new Usc(d);
                    /*
                    usc.setTarget(clawChannel, clawTargetValues[clawTargetIndex]);
                    usc.setTarget(elbowChannel, elbowTargetValues[elbowTargetIndex]);
                    usc.setTarget(shoulderChannel, elbowTargetValues[elbowTargetIndex]);
                    usc.setTarget(triggerChannel, off);
                    */
                }
            }
        }
        /*
        public void moveClaw(int direction)
        {
            if (direction == 0 && clawTargetIndex != (clawTargetValues.Length - 1))
            {
                usc.setTarget(clawChannel, clawTargetValues[clawTargetIndex + 1]);
                clawTargetIndex++;
            }
            else if (direction == 3 && clawTargetIndex != 0)
            {
                usc.setTarget(clawChannel, clawTargetValues[clawTargetIndex - 1]);
                clawTargetIndex--;
            }
        }

        public void pauseClaw()
        {
            usc.setTarget(clawChannel, 7645);
        }

        public void moveElbow(int direction)
        {
            if (direction == 0 && elbowTargetIndex != (elbowTargetValues.Length - 1))
            {
                usc.setTarget(elbowChannel, elbowTargetValues[elbowTargetIndex + 1]);
                elbowTargetIndex++;
            }
            else if (direction == 3 && elbowTargetIndex != 0)
            {
                usc.setTarget(elbowChannel, elbowTargetValues[elbowTargetIndex - 1]);
                elbowTargetIndex--;
            }
        }

        public void moveShoulder(int direction)
        {
            if (direction == 0 && shoulderTargetIndex != (shoulderTargetValues.Length - 1))
            {
                usc.setTarget(shoulderChannel, shoulderTargetValues[shoulderTargetIndex + 1]);
                shoulderTargetIndex++;
            }
            else if (direction == 3 && shoulderTargetIndex != 0)
            {
                usc.setTarget(shoulderChannel, shoulderTargetValues[shoulderTargetIndex - 1]);
                shoulderTargetIndex--;
            }
        }
        
        public void launch()
        {
            usc.setTarget(triggerChannel, on);

        }

        public void resetLaunch()
        {
            usc.setTarget(triggerChannel, off);
        }
        */
        public void TryToDisconnect()
        {
            if (usc != null)
            {
                try
                {
                    usc.Dispose();
                }
                #pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception e)
                #pragma warning restore CS0168 // Variable is declared but never used
                {
                    Logger.WriteLine("Failed to Disconnect");
                }
                finally
                {
                    usc = null;
                    Logger.WriteLine("Disconnected from Maestro");
                }
            }
         }
        /*
        public void elbowMovementTest()
        {
            do
            {
                usc.setTarget(elbowChannel,4250);//up
                Logger.WriteLine("4250");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(elbowChannel, 4500);
                Logger.WriteLine("4500");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(elbowChannel, 5000);
                Logger.WriteLine("5000");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(elbowChannel, 5500);
                Logger.WriteLine("5500");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(elbowChannel, 6000);
                Logger.WriteLine("6000");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(elbowChannel, 6500);
                Logger.WriteLine("6500");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(elbowChannel, 7000);
                Logger.WriteLine("7000");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(elbowChannel, 7500);
                Logger.WriteLine("7500");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(elbowChannel, 8000);
                Logger.WriteLine("8000");
                System.Threading.Thread.Sleep(1000);//down
            }
            while(false);
        }

        public void shoulderMovementTest()
        {
            do
            {
                usc.setTarget(shoulderChannel, 4250);//Down
                Logger.WriteLine("4250");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(shoulderChannel, 4500);
                Logger.WriteLine("4500");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(shoulderChannel, 5000);
                Logger.WriteLine("5000");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(shoulderChannel, 5500);
                Logger.WriteLine("5500");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(shoulderChannel, 6000);
                Logger.WriteLine("6000");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(shoulderChannel, 6500);
                Logger.WriteLine("6500");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(shoulderChannel, 7000);
                Logger.WriteLine("7000");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(shoulderChannel, 7500);
                Logger.WriteLine("7500");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(shoulderChannel, 8000);//Up
                Logger.WriteLine("8000");
                System.Threading.Thread.Sleep(1000);
            }
            while (false);
        }
        
        public void gripperMovementTest()
        {
            do
            {
                usc.setTarget(clawChannel, 7000);//closed
                Logger.WriteLine("7000");
                System.Threading.Thread.Sleep(1000);
                usc.setTarget(clawChannel, 8500);
                Logger.WriteLine("8500");
                System.Threading.Thread.Sleep(1000);
                
            }
            while (false);
        }

        public void resetTrigger()
        {
            do
            {
                usc.setTarget(triggerChannel, 5045); //off 
            }
            while (false);
        }
        */
    }
}
