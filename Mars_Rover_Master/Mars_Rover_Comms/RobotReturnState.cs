﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Comms
{
    public class RobotReturnState
    {
        public PositionReturnState PositionReturnState;
        public ArmReturnState ArmReturnState;
        public ErrorReturnState ErrorReturnState;
        public TemperatureReturnState TemperatureReturnState;
        public LogState LogState;

          public RobotReturnState()
        {
            PositionReturnState = new PositionReturnState();
            ArmReturnState = new ArmReturnState();
            ErrorReturnState = new ErrorReturnState();
            TemperatureReturnState = new TemperatureReturnState();
            LogState = new LogState();
        }
    }
}
