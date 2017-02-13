using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;


namespace Mars_Rover_Configuration
{
    //These values match the corresponding maestro pins
    public enum Devices : byte
    {
        FrontLeftWheel = 14, //was 1
        FrontRightWheel = 16, //was 4\was 10, now mercury arm motor
        MidLeftWheel = 4, //was 0\was 7, Now Mercury left wheels
        MidRightWheel = 5, //was 3\was 8, Now Mercury right wheels
        RearRightWheel = 9,
        RearLeftWheel = 15,  //was 2

        FrontRightSteering = 0,  //was 5\arm servo, was 15
        FrontLeftSteering = 1,  //was 13
        RearRightSteering = 3,  //was 16
        RearLeftSteering = 2,  //was 14

        ControlSignal = 23

    }
}
