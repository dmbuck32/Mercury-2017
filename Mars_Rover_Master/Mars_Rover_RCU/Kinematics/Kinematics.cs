/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Mars_Rover_RCU.Kinematics;
using Mars_Rover_Configuration;


namespace Mars_Rover_RCU.Kinematics
{
    public class Kinematics
    {
        private int FLOffset = 0;
        private int RLOffset = 20;
        private int FROffset = 0;
        private int RROffset = 20;
        private int gripperInterval = 50;
        private int MAX_VELOCITY;
        private int sensors;

        /// <summary>
        /// Creates an instance of Kinematics with the specified maximum velocity.
        /// </summary>
        /// <param name="max_vel"></param>
        public Kinematics(int max_vel)
        {
            if (max_vel <= 0)
                throw new ArgumentOutOfRangeException("Maximum velocity must be positive");
            MAX_VELOCITY = max_vel;
        }

        /// <summary>
        /// Performs calculation for the wheel states based on the input.
        /// </summary>
        /// <param name="radius">Radius of the circle to travel (center of robot as reference).
        /// Negative values are a counter-clockwise travel; positive value are clockwise travel.
        /// A radius of Infinity (either positive or negative) forces robot to travel straight.
        /// A radius less than MINIMUM_RADIUS will cause the robot to spin in place.</param>
        /// <param name="velocity">Velocity to travel (center of robot as reference)</param>
        /// <returns></returns>
        public Dictionary<Devices, int> GetWheelStates(int radius, int velocity, int armVelocity, bool ScoopIn, bool ScoopOut, bool autoStopArmUp, bool autoStopArmDown, bool wallFollow)
        {
            Dictionary<Devices, int> states = new Dictionary<Devices, int>();
            int adjustedArmVelocity = Convert.ToInt32(Math.Min(MAX_VELOCITY, Math.Abs(armVelocity))) * Math.Sign(armVelocity);

            //deal with scoop servo (NOTE: MAKE SETTING FOR SENSITIVITY)
            if (ScoopIn == true && ScoopOut == false) //scoop in
                states[Devices.FrontRightSteering] = gripperInterval;
            else if (ScoopIn == false && ScoopOut == true) //retract gripper
                states[Devices.FrontRightSteering] = gripperInterval * -1;
            else if (ScoopIn == false && ScoopOut == false) // gripper not in use - stay at last position
                states[Devices.FrontRightSteering] = 0;

            if (Program._Phidgets != null) //deal with arm motor
            {
                if (adjustedArmVelocity > 0 && Program._Phidgets.getUpperSwitch() == true) //arm is moving up (+ velocity value) and at max position
                    states[Devices.FrontRightWheel] = 0;
                else if (adjustedArmVelocity < 0 && Program._Phidgets.getLowerSwitch() == true) //arm is moving down (- velocity value) and at minimum position
                    states[Devices.FrontRightWheel] = 0;
                else //arm is operating between min and max position (no switch is pressed)
                    states[Devices.FrontRightWheel] = adjustedArmVelocity;
            }
            else
                states[Devices.FrontRightWheel] = 0;  //phidgets board not in use so do not use arm for the safety of the hardware (switches must be checked)
            

            if (Math.Abs(radius) >= 2047)
            { //simple enough, just drive straight at the given velocities.
                return GetStraightSystemState(states, velocity, autoStopArmUp, autoStopArmDown, wallFollow);

            }
            else
            { //general case
                return GetGeneralSystemState(states, radius, velocity, autoStopArmUp, autoStopArmDown);
            }
        }

        private Dictionary<Devices, int> GetStraightSystemState(Dictionary<Devices, int> states, double velocity, bool autoStopArmUp, bool autoStopArmDown, bool wallFollow)
        {
            int adjusted_velocity = Convert.ToInt32(Math.Min(MAX_VELOCITY, Math.Abs(velocity))) * Math.Sign(velocity); ;
            
            int frontSensor = Program._Phidgets.getFront();
            int leftSensor = Program._Phidgets.getLeft();
            int rightSensor = Program._Phidgets.getRight();
            
            int radiusVariation = 0;
            double val = 0;

            if (autoStopArmDown == true)
            {
                if (velocity < 0 && frontSensor <= 208 && frontSensor >= 80) //negative (going forward) sensor in range 80-208
                {
                    double factor = (double)(208 - frontSensor) / 128;
                    factor = Math.Pow(factor, 2);
                    adjusted_velocity = Convert.ToInt32(factor * adjusted_velocity);
                }
                else if (velocity < 0 && frontSensor > 208) // trying to go forward when not allowed
                {
                    adjusted_velocity = 0;
                }
            }

            else if (autoStopArmUp == true)
            {
                if (velocity < 0 && frontSensor <= 403 && frontSensor >= 80) //negative (going forward) sensor in range 80-423
                {
                    double factor = (double)(403 - frontSensor) / 323;
                    factor = Math.Pow(factor, 2);
                    adjusted_velocity = Convert.ToInt32(factor * adjusted_velocity);
                }
                else if (velocity < 0 && frontSensor > 403) // trying to go forward when not allowed
                {
                    adjusted_velocity = 0;
                }
            }

            if (wallFollow == true && leftSensor >= 112 && leftSensor <= 397)
            {
                if (leftSensor > 187 && leftSensor <= 397)//the robot is moving towards the left side of the track, correct right by slowing down the right side
                {
                    val = 1 - (double)(397 - leftSensor) / 209;  //between 0 and 1
                    radiusVariation = Convert.ToInt32(Math.Round(val * 30));
                    GetGeneralSystemState(states, (-1 * (100 - radiusVariation)), adjusted_velocity, autoStopArmUp, autoStopArmDown); //+radius go right
                }
                else if (leftSensor <= 187 && leftSensor >= 112)//robot moving towards the right side, correct by slowing down left motor
                {
                    val = (double)(187 - leftSensor) / 75;
                    radiusVariation = Convert.ToInt32(Math.Round(val * 30));
                    GetGeneralSystemState(states, (100 - radiusVariation), adjusted_velocity, autoStopArmUp, autoStopArmDown); //-radius go left
                }
            }
            else if (wallFollow == true && rightSensor >= 116 && rightSensor <= 400)
            {
                if (rightSensor > 191 && rightSensor <= 400)
                {
                    val = 1 - (double)(400 - rightSensor) / 208;  //between 0 and 1
                    radiusVariation = Convert.ToInt32(Math.Round(val * 30));
                    GetGeneralSystemState(states, (100 - radiusVariation), adjusted_velocity, autoStopArmUp, autoStopArmDown); //+radius go right
                }
                else if (rightSensor <= 191 && leftSensor >= 116)
                {
                    val = (double)(1 - leftSensor) / 75;
                    radiusVariation = Convert.ToInt32(Math.Round(val * 30));
                    GetGeneralSystemState(states, (-1 * (100 - radiusVariation)), adjusted_velocity, autoStopArmUp, autoStopArmDown); //-radius go left
                }
            }

            else
            {

                states[Devices.FrontLeftWheel] = adjusted_velocity;
                states[Devices.FrontLeftSteering] = FLOffset;
                //states[Devices.FrontRightSteering] = FROffset;

                states[Devices.MidLeftWheel] = adjusted_velocity; //mercury left motors
                states[Devices.MidRightWheel] = adjusted_velocity; //mercury right motors

                states[Devices.RearLeftWheel] = adjusted_velocity;
                states[Devices.RearLeftSteering] = RLOffset;

                states[Devices.RearRightWheel] = adjusted_velocity;
                states[Devices.RearRightSteering] = RROffset;
            }

            return states;
        }


        private Dictionary<Devices, int> GetGeneralSystemState(Dictionary<Devices, int> states, double radius, double velocity, bool autoStopArmUp, bool autoStopArmDown)
        {
            //first, calculate wheel angles --Not important for mercury rover
            double rad_magnitude = Math.Abs(radius);

            int inner_angle = 20;//Convert.ToInt32(90 - Math.Acos((Constants.WHEEL_BASE / 2) / (inner_radius+1)) * 180 / Math.PI);
            int outer_angle = 20;//Convert.ToInt32(90 - Math.Acos((Constants.WHEEL_BASE / 2) / outer_radius) * 180 / Math.PI);
  
            //calculate wheel velocities --> formula: Vinner = ((2R-width)/(2R+width)) * Vouter (Vouter is the velocity passed in)
            int outer_vel = Convert.ToInt32(Math.Min(MAX_VELOCITY, Math.Abs(velocity))) * Math.Sign(velocity);
            int inner_vel = Convert.ToInt32(((2 * rad_magnitude) - Constants.TRACK_WIDTH) / ((2 * rad_magnitude) + Constants.TRACK_WIDTH) * outer_vel);


            int frontSensor = Program._Phidgets.getFront();

            if (autoStopArmDown == true && radius != 0)
            {
                if (velocity < 0 && frontSensor <= 208 && frontSensor >= 80) //negative (going forward) sensor in range 80-208
                {
                    double factor = (double)(208 - frontSensor) / 128;
                    factor = Math.Pow(factor, 2);
                    outer_vel = Convert.ToInt32(factor * outer_vel);
                    inner_vel = Convert.ToInt32(factor * inner_vel);
                }
                else if (velocity < 0 && frontSensor > 208) // trying to go forward when not allowed
                {
                    outer_vel = 0;
                    inner_vel = 0;
                }
            }

            else if (autoStopArmUp == true && radius != 0)
            {
                if (velocity < 0 && frontSensor <= 403 && frontSensor >= 80) //negative (going forward) sensor in range 80-423
                {
                    double factor = (double)(403 - frontSensor) / 323;
                    factor = Math.Pow(factor, 2);
                    outer_vel = Convert.ToInt32(factor * outer_vel);
                    inner_vel = Convert.ToInt32(factor * inner_vel);
                }
                else if (velocity < 0 && frontSensor > 403) // trying to go forward when not allowed
                {
                    outer_vel = 0;
                    inner_vel = 0;
                }
            }

            if (radius < 0)
            { //turn left: left side inner, right side outer.  Front wheels negative rotate, rear wheels positive.
                states[Devices.FrontLeftWheel] = inner_vel;
                states[Devices.FrontLeftSteering] = -inner_angle - FLOffset;
                //states[Devices.FrontRightSteering] = -outer_angle + FROffset;
                states[Devices.MidLeftWheel] = inner_vel; //left mercury motors
                states[Devices.MidRightWheel] = outer_vel; //right mercury motors
                states[Devices.RearLeftWheel] = inner_vel;
                states[Devices.RearLeftSteering] = -inner_angle - RLOffset;
                states[Devices.RearRightWheel] = outer_vel;
                states[Devices.RearRightSteering] = -outer_angle - RROffset;
            }
            else
            { //turn right: left side outer, right side inner.  Front wheels positive rotate, rear wheels negative.
                states[Devices.FrontLeftWheel] = outer_vel;
                states[Devices.FrontLeftSteering] = outer_angle - FLOffset;
                //states[Devices.FrontRightSteering] = inner_angle + FROffset;
                states[Devices.MidLeftWheel] = outer_vel; //left mercury motors
                states[Devices.MidRightWheel] = inner_vel; //right mercury motors
                states[Devices.RearLeftWheel] = outer_vel;
                states[Devices.RearLeftSteering] = outer_angle + RLOffset;
                states[Devices.RearRightWheel] = inner_vel;
                states[Devices.RearRightSteering] = inner_angle + RROffset;
            }
            return states;
        }
    }
}*/
