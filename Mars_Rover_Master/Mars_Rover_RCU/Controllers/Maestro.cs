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
        private const short gripperClosed = 1100;

        // PWM Values for Drive Channels
        private const short leftOFF = 1500;
        private const short rightOFF = 1500;

        // PWM Values for LOS
        private short LOS_ON = 4000;
        private short LOS_OFF = 0;

        // PWM values for ARM Channels
        private short shoulderInit = 464;
        private short elbowInit = 1000;
        private short wristInit = 2000;

        // Maestro Stuff
        private const String DriveMaestro = "00109387";
        //private const String ArmMaestro = "00137085";
        //private const String DriveMaestro = "00159606"; // Testing board
        private const String ArmMaestro = "00159606"; // Testing board

        private Usc Drive = null;
        private Usc Arm = null;

        public Maestro()
        {

            Logger.WriteLine("Searching for maestros...");
            List<DeviceListItem> connectedDevices = Usc.getConnectedDevices();

            foreach (DeviceListItem Device in connectedDevices)
            {
                if (Device.serialNumber == DriveMaestro)
                {
                    Drive = new Usc(Device);
                    initializeDrive();
                    Logger.WriteLine("Found Drive Maestro");
                }
                if (Device.serialNumber == ArmMaestro)
                {
                    Arm = new Usc(Device);
                    initializeArm();
                    Logger.WriteLine("Found Arm Maestro");
                }
            }
        }

        // Initialize Drive Channels to stop
        public void initializeDrive()
        {
            Drive.setTarget(LeftMotors, (ushort)(PWM_Multiplier * leftOFF));
            Drive.setTarget(RightMotors, (ushort)(PWM_Multiplier * rightOFF));
        }

        // Initialize All servos to initial positions
        public void initializeArm()
        {
            Arm.setTarget(RearRightServo, MaestroMultiplier(rearRightInit));
            Arm.setTarget(RearLeftServo, MaestroMultiplier(rearLeftInit));
            Arm.setTarget(FrontRightServo, MaestroMultiplier(frontRightInit));
            Arm.setTarget(FrontLeftServo, MaestroMultiplier(frontLeftInit));
            Arm.setTarget(LOS_LED, MaestroMultiplier(LOS_OFF));
            Arm.setTarget(ShoulderServo, MaestroMultiplier(shoulderInit));
            Arm.setTarget(ElbowServo, MaestroMultiplier(elbowInit));
            Arm.setTarget(WristServo, MaestroMultiplier(wristInit));
            Arm.setTarget(GripperServo, MaestroMultiplier(gripperClosed));
        }

        // Open Gripper
        public void openGripper()
        {
            Arm.setTarget(GripperServo, MaestroMultiplier(gripperOpen));
        }

        // Close Gripper
        public void closeGripper()
        {
            Arm.setTarget(GripperServo, MaestroMultiplier(gripperClosed));
        }

        public void setDriveServos(short channelLeft, short channelRight)
        {
            Drive.setTarget(LeftMotors, MaestroMultiplier(channelLeft));
            Drive.setTarget(RightMotors, MaestroMultiplier(channelRight));
        }

        public void setTankMode()
        {
            setTurningServos(frontLeftInit, frontRightInit, rearLeftInit, rearRightInit);
        }

        public void setTranslateMode()
        {
            setTurningServos(500, 2000, 2000, 500);
        }

        public void setRotateMode()
        {
            setTurningServos(1500, 1500, 1500, 1500);
        }

        public void setTurningServos(short frontLeft, short frontRight, short rearLeft, short rearRight)
        {
            Arm.setTarget(FrontLeftServo, MaestroMultiplier(frontLeft));
            Arm.setTarget(FrontRightServo, MaestroMultiplier(frontRight));
            Arm.setTarget(RearLeftServo, MaestroMultiplier(rearLeft));
            Arm.setTarget(RearRightServo, MaestroMultiplier(rearRight));
        }

        public void setLOS(bool LOS)
        {
            if (LOS)
            {
                Arm.setTarget(LOS_LED, MaestroMultiplier(LOS_ON));
            }
            else
            {
                Arm.setTarget(LOS_LED, MaestroMultiplier(LOS_OFF));
            }
        }

        private ushort MaestroMultiplier(short input)
        {
            return (ushort)(PWM_Multiplier * input);
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
            if (Arm != null)
            {
                try
                {
                    Arm.Dispose();
                }
                #pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception e)
                #pragma warning restore CS0168 // Variable is declared but never used
                {
                    Logger.WriteLine("Failed to Disconnect");
                }
                finally
                {
                    Arm = null;
                    Logger.WriteLine("Disconnected from Arm Maestro");
                }
            }

            if (Drive != null)
            {
                try
                {
                    Drive.Dispose();
                }
                #pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception e)
                #pragma warning restore CS0168 // Variable is declared but never used
                {
                    Logger.WriteLine("Failed to Disconnect");
                }
                finally
                {
                    Drive = null;
                    Logger.WriteLine("Disconnected from Arm Maestro");
                }
            }
        }
    }
}
