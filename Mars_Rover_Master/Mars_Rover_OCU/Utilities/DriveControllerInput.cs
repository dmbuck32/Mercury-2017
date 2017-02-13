using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Xna.Framework.Input;


namespace Mars_Rover_OCU.Utilities
{
    public static class DriveControllerInput
    {
        
        public static Mars_Rover_Comms.DriveState GetDriveState()
        {
            var state = GamePad.GetState(ControllerSettings.Default.DrivePlayer);
            if (!state.IsConnected)
                throw new Exception("Drive Controller Disconnected!");

            //process state and build output
            Mars_Rover_Comms.DriveState driveState = new Mars_Rover_Comms.DriveState();

            if ((int)Math.Round(state.Triggers.Left * 1000) == 1000 && (int)Math.Round(state.Triggers.Right * 1000) == 1000)
            { //rotate
                driveState.Radius = 0;
                driveState.Speed = Convert.ToInt16(maxVelocity * GetRelativeSpeed(state.ThumbSticks.Right.Y));
            }
            else
            {
                //speed
                driveState.Speed = Convert.ToInt16(Settings.Default.MaxVelocity * GetRelativeSpeed(state.ThumbSticks.Right.Y));

                //steering
                driveState.Radius = GetRadius(state.ThumbSticks.Left.X);

            }
            return driveState;
        }

        internal static double GetRelativeSpeed(double input)
        {
            return (Math.Pow(Math.E, ControllerSettings.Default.SpeedSensitivity * input) - 1) /
                (Math.Pow(Math.E, ControllerSettings.Default.SpeedSensitivity * input) + 1);
        }

        //Radius uses left-stick x-value
        internal static short GetRadius(double input)
        {
            if (input == 0)
                return 2047;
            double input_magnitude = Math.Abs(input);

            double rad = (1 - input_magnitude) / (ControllerSettings.Default.SteeringSensitivity / 100.0 * input_magnitude);

            rad += Kinematics.Constants.MINIMUM_RADIUS;

            return Convert.ToInt16((Math.Min(2047,rad)*Math.Sign(input)));
        }
    }
}
