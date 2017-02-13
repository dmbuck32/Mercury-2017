using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Input;
using Mars_Rover_OCU.Properties;
using System.Runtime.InteropServices;

namespace Mars_Rover_OCU.Utilities
{
    public static class DriveKeyboard
    {

        //Minimum radius (in cm) the robot can turn on based on dimensions and rotation limit.
        //This value is hard coded to safe computation time.  
        //If any of TRACK_WIDTH, WHEEL_BASE, or MAX_WHEEL_ROTATE change,
        //this value will have to be recalculated.
        public static readonly int MINIMUM_RADIUS = 76;
       

        public static Mars_Rover_Comms.DriveState getDriveState()
        {
            Mars_Rover_Comms.DriveState driveState = new Mars_Rover_Comms.DriveState();

            //int key = _getch();
            KeyboardState keyState = Keyboard.GetState();
            driveState.Radius = 0;
            driveState.Speed = 0;

            if (keyState.IsKeyDown(Keys.Up))
            {
                driveState.Radius = 2047;
                driveState.Speed = Convert.ToInt16(1);          
            }

            return driveState;
        }

        public static void setDriveState(System.Windows.Input.Key key)
        {

        }

    }
}
