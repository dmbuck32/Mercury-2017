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

        // Maestro Stuff
       // private const String DriveMaestro = "00109387";
        private const String ArmMaestro = "00137085";
        //private const String ArmMaestro = "00159606"; // Large Testing board
        //private const String DriveMaestro = "00135614"; // Small Testing board

        private Usc maestro = null;

        public Maestro()
        {

            Logger.WriteLine("Searching for maestros...");
            List<DeviceListItem> connectedDevices = Usc.getConnectedDevices();

            foreach (DeviceListItem Device in connectedDevices)
            { 
                if (Device.serialNumber == ArmMaestro)
                {
                    maestro = new Usc(Device);
                    initializeArmMaestro();
                    Logger.WriteLine("Found Arm Maestro");
                }
            }
            setArmSpeed(100, 100, 100);
        }

        // Initialize Drive Channels to stop
        public void initializeDriveMaestro()
        {
            setDriveServos(OFF, OFF);
        }

        // Initialize All servos to initial positions
        public void initializeArmMaestro()
        {
            setTankMode();
            maestro.setTarget(LOS_LED, MaestroMultiplier(LOS_OFF));
            armHomePos();
            closeGripper();
        }

        // Open Gripper
        public void openGripper()
        {
            maestro.setTarget(GripperServo, MaestroMultiplier(gripperOpen));
        }

        // Close Gripper
        public void closeGripper()
        {
            maestro.setTarget(GripperServo, MaestroMultiplier(gripperClosed));
        }

        public void setDriveServos(short channelLeft, short channelRight)
        {
            maestro.setTarget(LeftMotors, MaestroMultiplier(channelLeft));
            maestro.setTarget(RightMotors, MaestroMultiplier(channelRight));
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

        public void setArmServos(short shoulder, short elbow, short wrist)
        {
            maestro.setTarget(ShoulderServo, MaestroMultiplier(shoulder));
            maestro.setTarget(ElbowServo, MaestroMultiplier(elbow));
            maestro.setTarget(WristServo, MaestroMultiplier(wrist));
        }

        public void setTurningServos(short frontLeft, short frontRight, short rearLeft, short rearRight)
        {
            maestro.setTarget(FrontLeftServo, MaestroMultiplier(frontLeft));
            maestro.setTarget(FrontRightServo, MaestroMultiplier(frontRight));
            maestro.setTarget(RearLeftServo, MaestroMultiplier(rearLeft));
            maestro.setTarget(RearRightServo, MaestroMultiplier(rearRight));
        }

        public void noControl()
        {
            maestro.setTarget(LOS_LED, MaestroMultiplier(LOS_ON));
            System.Threading.Thread.Sleep(10);
            maestro.setTarget(LOS_LED, MaestroMultiplier(LOS_OFF));
        }

        public void setLOS(bool LOS)
        {
            if (LOS)
            {
                maestro.setTarget(LOS_LED, MaestroMultiplier(LOS_ON));
            }
            else
            {
                maestro.setTarget(LOS_LED, MaestroMultiplier(LOS_OFF));
            }
        }

        public void setArmAcceleration(short shoulderAcceleraton, short elbowAcceleration, short wristAcceleration)
        {
            maestro.setAcceleration(ShoulderServo, (ushort)shoulderAcceleraton);
            maestro.setAcceleration(ElbowServo, (ushort)elbowAcceleration);
            maestro.setAcceleration(WristServo, (ushort)wristAcceleration);
        }

        public void setArmSpeed(short shoulderSpeed, short elbowSpeed, short wristSpeed)
        {
            maestro.setSpeed(ShoulderServo, (ushort)shoulderSpeed);
            maestro.setSpeed(ElbowServo, (ushort)elbowSpeed);
            maestro.setSpeed(WristServo, (ushort)wristSpeed);
        }

        private ushort MaestroMultiplier(short input)
        {
            return (ushort)(PWM_Multiplier * input);
        }

        public void TryToDisconnect()
        {
            if (maestro != null)
            {
                try
                {
                    maestro.Dispose();
                }
                #pragma warning disable CS0168 // Variable is declared but never used
                catch (Exception e)
                #pragma warning restore CS0168 // Variable is declared but never used
                {
                    Logger.WriteLine("Failed to Disconnect");
                }
                finally
                {
                    maestro = null;
                    Logger.WriteLine("Disconnected from Maestro");
                }
            }
        }
    }
}
