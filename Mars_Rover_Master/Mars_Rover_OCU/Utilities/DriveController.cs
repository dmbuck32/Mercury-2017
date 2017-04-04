﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Mars_Rover_OCU.Properties;

namespace Mars_Rover_OCU.Utilities
{
    public static class DriveController
    {

        //Minimum radius (in cm) the robot can turn on based on dimensions and rotation limit.
        //This value is hard coded to safe computation time.  
        //If any of TRACK_WIDTH, WHEEL_BASE, or MAX_WHEEL_ROTATE change,
        //this value will have to be recalculated.
        public static readonly int MINIMUM_RADIUS = 18;

        private static readonly short closed = 0;
        private static readonly short open = 1;
        private static readonly short tank = 3;
        private static readonly short translate = 2;
        private static readonly short rotate = 1;
        private static readonly short normal = 0;
        private static readonly short STOP = 1500;
        private static readonly short shoulder = 0;
        private static readonly short elbow = 1;
        private static readonly short wrist = 2;

        // Static variables to store current state of Drivestate
        private static short mode = 0;
        private static short armState = shoulder;
        private static short gripper = closed;
        private static bool headlight = false;
        private static short RightSpeed = 1500;
        private static short LeftSpeed = 1500;
        private static bool usePID = false;

        private static double rTrigger;
        private static double lTrigger;
        private static double lStickX;
        private static double lStickY;
        private static double rStickX;
        private static double rStickY;

        public static Mars_Rover_Comms.DriveState getDriveState()
        {
            GamePadState state = GamePad.GetState(ControllerSettings.Default.DrivePlayer);
            
            if (!state.IsConnected)
                throw new Exception("Drive Controller Disconnected!");

            //process state and build output
            Mars_Rover_Comms.DriveState driveState = new Mars_Rover_Comms.DriveState();

            //-- 2017 Mercury Robot Controls --//

            // Robot Drive State
            if (state.Buttons.A == ButtonState.Pressed)// A Button Handler (Translate Mode)
            {
                mode = translate;
            }
            else if (state.Buttons.X == ButtonState.Pressed) // X Button Handler (Rotate Mode)
            {
                mode = rotate;
            }
            else if (state.Buttons.Y == ButtonState.Pressed) // Y Button Handler (Normal Mode)
            {
                mode = normal;
            }
            else if (state.Buttons.B == ButtonState.Pressed) // B Button Handler (Tank Mode)
            {
                mode = tank;
            }

            // Robot Arm State
            if (state.DPad.Down == ButtonState.Pressed) // D-Pad Down Handler (Shoulder Servo)
            {
                armState = shoulder;
            }
            else if (state.DPad.Up == ButtonState.Pressed) // D-Pad Up Handler (N/A)
            {

            }
            else if (state.DPad.Left == ButtonState.Pressed) // D-Pad Left Handler (Elbow Servo)
            {
                armState = elbow;
            }
            else if (state.DPad.Right == ButtonState.Pressed)// D-Pad Right Handler (Wrist Servo)
            {
                armState = wrist;
            }

            // Robot Gripper
            if (state.Buttons.LeftShoulder == ButtonState.Pressed) // Left Bumper Handler (Gripper Close)
            {
                gripper = closed;
            }
            else if (state.Buttons.RightShoulder == ButtonState.Pressed) // Right Bumper Handler (Gripper Open)
            {
                gripper = open;
            }

            // Headlights
            if (state.Buttons.Start == ButtonState.Pressed) // Start Button Handler (Headlight Toggle)
            {
                if (headlight)
                {
                    headlight = false;
                } else
                {
                    headlight = true;
                }
                
            }

            if (state.Buttons.Back == ButtonState.Pressed)
            {
                if (usePID)
                {
                    usePID = false;
                }
                else
                {
                    usePID = true;
                }
            }

            // Trigger Values
            rTrigger = state.Triggers.Right;
            lTrigger = state.Triggers.Left;

            // Right Stick Values
            rStickX = state.ThumbSticks.Right.X;
            rStickY = state.ThumbSticks.Right.Y;

            // Left Stick Values
            lStickX = state.ThumbSticks.Left.X;
            lStickY = state.ThumbSticks.Left.Y;

            // Robot Motor Speed

            if (mode == normal)
            {
                if (rTrigger == 0 && lTrigger == 0 && rStickY == 0) // No Movement
                {
                    RightSpeed = STOP;
                    LeftSpeed = STOP;
                }
                else if (rTrigger != 0 && lTrigger == 0) // Forewards
                {
                    RightSpeed = GetSpeed(rTrigger);
                    LeftSpeed = GetSpeed(rTrigger);
                }
                else if (lTrigger != 0 && rTrigger == 0) // Backwards
                {
                    RightSpeed = GetSpeed(-lTrigger);
                    LeftSpeed = GetSpeed(-lTrigger);
                }
                else if (rTrigger == 0 && lTrigger == 0 && rStickY != 0) // Right Stick
                {
                    RightSpeed = GetSpeed(rStickY);
                    LeftSpeed = GetSpeed(rStickY);
                }
                else if (lTrigger != 0 && rTrigger == 0) // Both down = stop
                {
                    RightSpeed = STOP;
                    LeftSpeed = STOP;
                }

            } else if (mode == rotate)
            {
                if (rTrigger == 0 && lTrigger == 0 && rStickX == 0) // No Movement
                {
                    RightSpeed = STOP;
                    LeftSpeed = STOP;
                }
                else if (rTrigger != 0 && lTrigger == 0) // Rotate Right
                {
                    RightSpeed = GetSpeed(-rTrigger);
                    LeftSpeed = GetSpeed(rTrigger);
                }
                else if (lTrigger != 0 && rTrigger == 0) // Rotate Left
                {
                    RightSpeed = GetSpeed(lTrigger);
                    LeftSpeed = GetSpeed(-lTrigger);
                }
                else if (rTrigger == 0 && lTrigger == 0 && rStickX == 0) // Rotate with Right Stick
                {
                    RightSpeed = GetSpeed(-rStickX);
                    LeftSpeed = GetSpeed(rStickX);
                }

            } else if (mode == translate)
            {
                if (rTrigger == 0 && lTrigger == 0 && rStickX == 0) // No Movement
                {
                    RightSpeed = STOP;
                    LeftSpeed = STOP;
                }
                else if (rTrigger != 0 && lTrigger == 0) // Rotate Right
                {
                    RightSpeed = GetSpeed(-rTrigger);
                    LeftSpeed = GetSpeed(rTrigger);
                }
                else if (lTrigger != 0 && rTrigger == 0) // Rotate Left
                {
                    RightSpeed = GetSpeed(lTrigger);
                    LeftSpeed = GetSpeed(-lTrigger);
                }
                else if (rTrigger == 0 && lTrigger == 0 && rStickX == 0) // Rotate with Right Stick
                {
                    RightSpeed = GetSpeed(-rStickX);
                    LeftSpeed = GetSpeed(rStickX);
                }
            } else if (mode == tank) // Only use Right stick
            {
                double tempX = -rStickX;
                double V = (2000 - Math.Abs(tempX)) * (rStickY / 2000) + rStickY;
                double W = (2000 - Math.Abs(rStickY)) * (tempX / 2000) + tempX;
                RightSpeed = (short)(Math.Max(Math.Min(Math.Round(((V + W) / 2) * 500) + 1500, 2000), 1000)); //Bounded between 1000 - 2000
                LeftSpeed = (short)(Math.Max(Math.Min(Math.Round(((V - W) / 2) * 500) + 1500, 2000), 1000)); //Bounded between 1000 - 2000
            }

            //TODO: Figure out Arm movement


            /*
            //Arm Stuff
            if (RStick != 0) //arm up or down
                driveState.ArmSpeed = Convert.ToInt16(Math.Round(50 * (4 * Math.Pow(Math.Abs(RStick) - 0.5, 3) + 0.5)) * Math.Sign(RStick));
                */

            // Drive State variable seting
            driveState.Mode = mode;
            driveState.ArmState = armState;
            driveState.gripperPos = gripper;
            driveState.Headlights = headlight;
            driveState.usePID = usePID;
            driveState.RightSpeed = RightSpeed;
            driveState.LeftSpeed = LeftSpeed;

            return driveState;
        }

        //Radius uses left-stick x-value
        internal static short GetRadius(double input)
        {
            if (input == 0)
                return 2047;
            
            double input_magnitude = (4 * Math.Pow(Math.Abs(input) - 0.5, 3) + 0.5);

            double rad = (1 - input_magnitude) * ControllerSettings.Default.SteeringSensitivity;

            rad += MINIMUM_RADIUS;

            return Convert.ToInt16(-1 * (Math.Min(2047, rad) * Math.Sign(input)));
            
        }

        internal static short GetSpeed(double input)
        {
            return (short)Math.Round(500 * input + 1500);
        }
    }
}
