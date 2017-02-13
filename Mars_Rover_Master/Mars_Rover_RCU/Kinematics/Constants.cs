using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Mars_Rover_RCU.Kinematics
{
    public static class Constants
    {
        /// <summary>
        /// Axle width in centimeters.
        /// </summary>
        public static readonly int TRACK_WIDTH = 24;

        /// <summary>
        /// Distance between front/rear 'axles' in centimeters.
        /// </summary>
        public static readonly int WHEEL_BASE = 21;

        /// <summary>
        /// Maximum number of degrees each wheel servo is allowed to rotate.
        /// </summary>
        public static readonly int MAX_WHEEL_ROTATE = 0;

        /// <summary>
        /// Minimum radius (in cm) the robot can turn on based on dimensions and rotation limit.
        /// </summary>
        /// <remarks>
        /// This value is hard coded to safe computation time.  
        /// If any of TRACK_WIDTH, WHEEL_BASE, or MAX_WHEEL_ROTATE change,
        /// this value will have to be recalculated.
        /// </remarks>
        public static readonly int MINIMUM_RADIUS = 0;
    }
}
