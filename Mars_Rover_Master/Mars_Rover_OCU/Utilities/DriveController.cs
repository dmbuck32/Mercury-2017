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


        public static Mars_Rover_Comms.DriveState getDriveState()
        {
            GamePadState state = GamePad.GetState(ControllerSettings.Default.DrivePlayer);
            
            if (!state.IsConnected)
                throw new Exception("Drive Controller Disconnected!");

            //process state and build output
            Mars_Rover_Comms.DriveState driveState = new Mars_Rover_Comms.DriveState();
            
            //speed adjustment upward
            if (state.DPad.Up == ButtonState.Pressed && ControllerSettings.Default.SpeedSensitivity <= 48)
                ControllerSettings.Default.SpeedSensitivity += 2;
            else if (state.DPad.Up == ButtonState.Pressed && (ControllerSettings.Default.SpeedSensitivity + 2) >= 50)
                ControllerSettings.Default.SpeedSensitivity = 50;
            
            //speed adjustment downward
            if (state.DPad.Down == ButtonState.Pressed && ControllerSettings.Default.SpeedSensitivity >= 2)
                ControllerSettings.Default.SpeedSensitivity -= 2;
            else if (state.DPad.Down == ButtonState.Pressed && (ControllerSettings.Default.SpeedSensitivity - 2) <= 0)
                ControllerSettings.Default.SpeedSensitivity = 0;


            //driving stuff
            double LTrigger = state.Triggers.Left;
            double RTrigger = state.Triggers.Right;
            double LStick = state.ThumbSticks.Left.X;
            double RStick = state.ThumbSticks.Right.Y;

            if (LTrigger == 0 && RTrigger != 0 && LStick == 0) //Both wheels forward
            {
                //driveState.Radius = 2047;
                driveState.LeftSpeed = Convert.ToInt16(-1 * Math.Round(ControllerSettings.Default.SpeedSensitivity * (4 * Math.Pow(RTrigger - 0.5, 3) + 0.5)));
                driveState.RightSpeed = Convert.ToInt16(-1 * Math.Round(ControllerSettings.Default.SpeedSensitivity * (4 * Math.Pow(RTrigger - 0.5, 3) + 0.5)));
            }
            
            else if (RTrigger == 0 && LTrigger != 0 && LStick == 0) //Both wheels backward
            {
                //driveState.Radius = 2047;
                driveState.LeftSpeed = Convert.ToInt16(Math.Round(ControllerSettings.Default.SpeedSensitivity * (4 * Math.Pow(LTrigger - 0.5, 3) + 0.5)));
                driveState.RightSpeed = Convert.ToInt16(Math.Round(ControllerSettings.Default.SpeedSensitivity * (4 * Math.Pow(LTrigger - 0.5, 3) + 0.5)));
            }
            else if (LTrigger == 0 && RTrigger == 0 && LStick != 0) //tank steering
            {
                //driveState.Radius = 0;
                driveState.LeftSpeed = Convert.ToInt16(Math.Round(ControllerSettings.Default.SpeedSensitivity * (4 * Math.Pow(Math.Abs(LStick) - 0.5, 3) + 0.5)) * Math.Sign(LStick));
                driveState.RightSpeed = Convert.ToInt16(Math.Round(ControllerSettings.Default.SpeedSensitivity * (4 * Math.Pow(Math.Abs(LStick) - 0.5, 3) + 0.5)) * Math.Sign(LStick));
            } 
            else if (LTrigger == 0 && RTrigger != 0 && LStick != 0) // forward correction steering
            {
                driveState.Radius = GetRadius(LStick);
                driveState.Speed = Convert.ToInt16(-1 * Math.Round(ControllerSettings.Default.SpeedSensitivity * (4 * Math.Pow(RTrigger - 0.5, 3) + 0.5)));
            }
            else if (LTrigger != 0 && RTrigger == 0 && LStick != 0) // reverse correction steering
            {
                driveState.Radius = GetRadius(LStick);
                driveState.Speed = Convert.ToInt16(Math.Round(ControllerSettings.Default.SpeedSensitivity * (4 * Math.Pow(LTrigger - 0.5, 3) + 0.5)));
            }
            else
            {
                driveState.Radius = 2047;
                driveState.Speed = 0;
            }

            //Arm Stuff
            if (RStick != 0) //arm up or down
                driveState.ArmSpeed = Convert.ToInt16(Math.Round(50 * (4 * Math.Pow(Math.Abs(RStick) - 0.5, 3) + 0.5)) * Math.Sign(RStick));

            
            //Gripper Stuff
            driveState.ScoopIn = state.IsButtonDown(Buttons.RightStick);
            driveState.ScoopOut = state.IsButtonDown(Buttons.LeftStick);

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
    }
}
