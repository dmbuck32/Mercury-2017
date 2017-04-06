using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Pololu.Usc;
using Pololu.UsbWrapper;
using System.Diagnostics;
using Mars_Rover_RCU.Utilities;
using System.IO.Ports;
using System.Management;
using System.Timers;

namespace Mars_Rover_RCU.Controllers
{
    public class PID
    {
        String[] SensorData;
        int[] error;
        int errorIndex = 0;
        float Kp = 1;
        float Ki = 0;
        float Kd = 0;
        float correction = 0;
        public Boolean enabled = false;


        public PID()
        {
            SensorData = new String[6];
            error = new int[10];
        }

        public void update()
        {
            //distance[1] = left sensor distance in mm
            //distance[2] = right sensor distance in mm
            int[] distance = new int[6];
            int i;
            int average = 0;
            float P=0;
            float I=0;
            float D=0;

            for(i=0;i<3;i++)
            {
                distance[i] = Int32.Parse(SensorData[i]);
            }

            error[errorIndex] = (distance[1] - distance[2])/2;


            //Calculating D
            if (errorIndex > 0)
            {
                average = 0;
                for (i = 1; i <= errorIndex; i++)
                {
                    average += error[i] - error[i - 1];
                }
                D = Kd*(average / i);
            }
           
            //Calculating I
            average = 0;
            for (i = 0; i <= errorIndex; i++)
            {
                average += error[i];
            }
            I = Ki*average;

            //Calculating P
            P = Kp * error[errorIndex];

            //Generating correction
            correction = (P + I + D);
            //correction = (P + I + D) + 1500;

            if (errorIndex < 10)
            {
                errorIndex++;
            }
            else
            {
                errorIndex = 0;
            }
        }
    }
    }