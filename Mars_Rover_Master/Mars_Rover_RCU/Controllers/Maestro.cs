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

        // PWM Channels for Maestro
        private byte LeftMotors = 0;
        private byte RightMotors = 1;
        private byte RearRightServo = 2;
        private byte RearLeftServo = 3;
        private byte FrontRightServo = 4;
        private byte FrontLeftServo = 5;
        private byte LOS_LED = 6;
        private byte HeadLights = 7;
        private byte ShoulderServo = 8;
        private byte ElbowServo = 9;
        private byte WristServo = 10;
        private byte GripperServo = 11;

        // PWM multiplier for Pololu Maestro
        private byte PWM_Multiplier = 4;

        // Initial Values to make wheels straight
        private int RearLeftInit = 1500;
        private int RearRightInit = 1500;
        private int FrontLeftInit = 1500;
        private int FrontRightInit = 1500;

        private int GripperOpen = 1500;
        private int GripperClosed = 1100;

        public Maestro()
        {
            var devicelist = Usc.getConnectedDevices();
            foreach (DeviceListItem d in Usc.getConnectedDevices())
            {
                if (d.serialNumber == devicelist[0].serialNumber)
                {
                    usc = new Usc(d);
                    initializeDrive();
                    /*
                    usc.setTarget(clawChannel, clawTargetValues[clawTargetIndex]);
                    usc.setTarget(elbowChannel, elbowTargetValues[elbowTargetIndex]);
                    usc.setTarget(shoulderChannel, elbowTargetValues[elbowTargetIndex]);
                    usc.setTarget(triggerChannel, off);
                    */
                }
            }
        }

        // Initialize turning servos to straight positions
        public void initializeDrive()
        {
            usc.setTarget(RearLeftServo, (ushort)(PWM_Multiplier * RearLeftInit));
            usc.setTarget(RearRightServo, (ushort)(PWM_Multiplier * RearRightInit));
            usc.setTarget(FrontLeftServo, (ushort)(PWM_Multiplier * FrontLeftInit));
            usc.setTarget(FrontRightServo, (ushort)(PWM_Multiplier * FrontRightInit));
        }

        // Open Gripper
        public void openGripper()
        {
            usc.setTarget(GripperServo, (ushort)(PWM_Multiplier * GripperOpen));
        }

        // Close Gripper
        public void closeGripper()
        {
            usc.setTarget(GripperServo, (ushort)(PWM_Multiplier * GripperClosed));
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
