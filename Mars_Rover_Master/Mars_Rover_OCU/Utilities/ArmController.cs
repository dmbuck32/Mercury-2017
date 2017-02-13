/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Mars_Rover_OCU.Properties;

namespace Mars_Rover_OCU.Utilities
{
    public static class ArmController
    {
        public enum ButtonStatus
        {
            Pressed = 1,
            NotPressed = 0
        }
        public enum StickStatus
        {
            Positive = 1,
            Negative = -1,
            Idle = 0,
        }
        public const float sensitivityThreshold = 0.5f;

        public static Mars_Rover_Comms.ArmState getArmState()
        {
            var state = GamePad.GetState(ControllerSettings.Default.ArmPlayer);
            if (!state.IsConnected)
                throw new Exception("Arm Controller Disconnected!");

            //process state and build output
            Mars_Rover_Comms.ArmState armState = new Mars_Rover_Comms.ArmState();

            armState.ArmData = CompressGamePad(state);

            return armState;

        }

        /*private static int CompressGamePad(GamePadState armGamePad)
        {
            int compressedGamePad = 0;

            // Consolidate all buttons into a single variable. Doin work, son.
            compressedGamePad |= Math.Abs(convertJoystick(armGamePad.ThumbSticks.Left.X));                  // Bit 1  = left stick x-axis pressed
            compressedGamePad |= (((convertJoystick(armGamePad.ThumbSticks.Left.X) - 1) / (-2)) << 1);      // Bit 2  = left stick x-axis direction 
            compressedGamePad |= (Math.Abs(convertJoystick(armGamePad.ThumbSticks.Left.Y)) << 2);           // Bit 3  = left stick y-axis pressed
            compressedGamePad |= (((convertJoystick(armGamePad.ThumbSticks.Left.Y) - 1) / (-2)) << 3);      // Bit 4  = left stick y-axis direction
            compressedGamePad |= (Math.Abs(convertJoystick(armGamePad.ThumbSticks.Right.X)) << 4);          // Bit 5  = right stick x-axis pressed
            compressedGamePad |= (((convertJoystick(armGamePad.ThumbSticks.Right.X) - 1) / (-2)) << 5);     // Bit 6  = right stick x-axis direction
            compressedGamePad |= (Math.Abs(convertJoystick(armGamePad.ThumbSticks.Right.Y)) << 6);          // Bit 7  = right stick y-axis pressed
            compressedGamePad |= (((convertJoystick(armGamePad.ThumbSticks.Right.Y) - 1) / (-2)) << 7);     // Bit 8  = right stick y-axis direction          
            compressedGamePad |= (convertJoystick(armGamePad.Triggers.Left) << 8);                          // Bit 9  = left trigger pressed
            compressedGamePad |= (convertJoystick(armGamePad.Triggers.Right) << 9);                         // Bit 10 = right trigger pressed
            compressedGamePad |= (convertButtonPress(armGamePad.Buttons.LeftShoulder) << 10);               // Bit 11 = left shoulder pressed
            compressedGamePad |= (convertButtonPress(armGamePad.Buttons.RightShoulder) << 11);              // Bit 12 = right shoulder pressed
            compressedGamePad |= (convertButtonPress(armGamePad.Buttons.A) << 12);                          // Bit 13 = A button pressed
            compressedGamePad |= (convertButtonPress(armGamePad.Buttons.B) << 13);                          // Bit 14 = B button pressed
            compressedGamePad |= (convertButtonPress(armGamePad.Buttons.X) << 14);                          // Bit 15 = X button pressed
            compressedGamePad |= (convertButtonPress(armGamePad.Buttons.Y) << 15);                          // Bit 16 = Y button pressed
            compressedGamePad |= (convertButtonPress(armGamePad.Buttons.Start) << 16);                      // Bit 17 = Start button pressed
            compressedGamePad |= (convertButtonPress(armGamePad.Buttons.Back) << 17);                       // Bit 18 = Back button pressed
            compressedGamePad |= (convertButtonPress(armGamePad.DPad.Up) << 18);                            // Bit 19 = Up button pressed
            compressedGamePad |= (convertButtonPress(armGamePad.DPad.Right) << 19);                         // Bit 20 = Right button presssed
            compressedGamePad |= (convertButtonPress(armGamePad.DPad.Down) << 20);                          // Bit 21 = Down button pressed
            compressedGamePad |= (convertButtonPress(armGamePad.DPad.Left) << 21);                          // Bit 22 = Left button pressed

            return compressedGamePad;
        }

        public static int convertJoystick(float stick)
        {
            if (stick >= sensitivityThreshold)
            {
                return (int)StickStatus.Positive;
            }
            else if (stick <= (sensitivityThreshold * -1))
            {
                return (int)StickStatus.Negative;
            }
            else
            {
                return (int)StickStatus.Idle;
            }
        }

        public static int convertButtonPress(ButtonState button)
        {
            if (button.ToString() == "Pressed")
            {
                return (int)ButtonStatus.Pressed;
            }
            else
            {
                return (int)ButtonStatus.NotPressed; ;
            }
        }

    }
}*/
