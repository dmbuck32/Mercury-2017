using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mars_Rover_Configuration
{
    [Serializable]
    public class ConfigureMaestro
    {

        private ControlSignalOutputSettings controlSettings;

        private SteeringServoOutputSettings steeringSettings1;
        private SteeringServoOutputSettings steeringSettings2;
        private SteeringServoOutputSettings steeringSettings3;
        private SteeringServoOutputSettings steeringSettings4;

        private DrivingMotorOutputSettings drivingSettings6;
        private DrivingMotorOutputSettings drivingSettings7;
        private DrivingMotorOutputSettings drivingSettings8;
        private DrivingMotorOutputSettings drivingSettings9;
        private DrivingMotorOutputSettings drivingSettings10;
        private DrivingMotorOutputSettings drivingSettings11;

        private int timeout;

        private ConfigureMaestro() //Only for serialization
        {
        }

        public ConfigureMaestro(int timeout)
        {
            this.timeout = timeout;
            this.steeringSettings1 = new SteeringServoOutputSettings();

        }

        public ConfigureMaestro(int timeout,ControlSignalOutputSettings controlSettings, SteeringServoOutputSettings steeringSettings1, SteeringServoOutputSettings steeringSettings2,
            SteeringServoOutputSettings steeringSettings3, SteeringServoOutputSettings steeringSettings4, DrivingMotorOutputSettings drivingSettings6,
            DrivingMotorOutputSettings drivingSettings7, DrivingMotorOutputSettings drivingSettings8, DrivingMotorOutputSettings drivingSettings9,
            DrivingMotorOutputSettings drivingSettings10, DrivingMotorOutputSettings drivingSettings11)
        {
            if (timeout <= 0)
                throw new ArgumentOutOfRangeException("timeout");
            if (steeringSettings1 == null)
                throw new ArgumentOutOfRangeException("steeringSettings");

            this.timeout = timeout;

            this.controlSettings = controlSettings;

            this.steeringSettings1 = steeringSettings1;
            this.steeringSettings2 = steeringSettings2;
            this.steeringSettings3 = steeringSettings3;
            this.steeringSettings4 = steeringSettings4;

            this.drivingSettings6 = drivingSettings6;
            this.drivingSettings7 = drivingSettings7;
            this.drivingSettings8 = drivingSettings8;
            this.drivingSettings9 = drivingSettings9;
            this.drivingSettings10 = drivingSettings10;
            this.drivingSettings11 = drivingSettings11;
        }


        public ControlSignalOutputSettings ControlOutput
        {
            get { return controlSettings; }
            set { controlSettings = value; }
        }
       
        public SteeringServoOutputSettings SteeringServoOutput1
        {
            get { return steeringSettings1; }
            set { steeringSettings1 = value; }
        }

        public SteeringServoOutputSettings SteeringServoOutput2
        {
            get { return steeringSettings2; }
            set { steeringSettings2 = value; }
        }

        public SteeringServoOutputSettings SteeringServoOutput3
        {
            get { return steeringSettings3; }
            set { steeringSettings3 = value; }
        }

        public SteeringServoOutputSettings SteeringServoOutput4
        {
            get { return steeringSettings4; }
            set { steeringSettings4 = value; }
        }

        public DrivingMotorOutputSettings DrivingMotorOutput6
        {
            get { return drivingSettings6; }
            set { drivingSettings6 = value; }
        }

        public DrivingMotorOutputSettings DrivingMotorOutput7
        {
            get { return drivingSettings7; }
            set { drivingSettings7 = value; }
        }

        public DrivingMotorOutputSettings DrivingMotorOutput8
        {
            get { return drivingSettings8; }
            set { drivingSettings8 = value; }
        }

        public DrivingMotorOutputSettings DrivingMotorOutput9
        {
            get { return drivingSettings9; }
            set { drivingSettings9 = value; }
        }

        public DrivingMotorOutputSettings DrivingMotorOutput10
        {
            get { return drivingSettings10; }
            set { drivingSettings10 = value; }
        }

        public DrivingMotorOutputSettings DrivingMotorOutput11
        {
            get { return drivingSettings11; }
            set { drivingSettings11 = value; }
        }

        public int Timeout
        {
            get { return timeout; }
            set { timeout = value; }
        }


    }
}
