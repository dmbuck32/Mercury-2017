/*using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Phidgets;
using Phidgets.Events;

namespace Mars_Rover_RCU.Controllers
{
    public class PhidgetsController
    {
        private int[] sensorArray = new int[4];
        static InterfaceKit ikit;
        /*Creates a new InterfaceKit object, opens Phidgets controller
         *and awaits for a connection
         **/
/*
        public PhidgetsController()
        {
            ikit = new InterfaceKit();
            ikit.open();
            ikit.waitForAttachment();
        }

        /*Gets a boolean value from a switch
         *Used to determine if robotic arm is at max height
         returns true if switch is closed
         * */
/*        public bool getUpperSwitch()
        {
            return ikit.inputs[0];
        }
        /*Get a boolean value from a switch
         * Used to determine if robotic arm is at min height
         * returns true if switch is closed
         * */
/*        public bool getLowerSwitch()
        {
            return ikit.inputs[7];
        }

        /*Gets an array of intergers (from 0-1000)
         *used to determine distance away from objects
         **/
 /*       public int[] getSensorArray()
        {
            sensorArray[0] = ikit.sensors[7].Value;
            sensorArray[1] = ikit.sensors[6].Value;
            sensorArray[2] = ikit.sensors[5].Value;
            sensorArray[3] = ikit.sensors[4].Value;
            return sensorArray;
        }

        public int getFront()
        {
            return ikit.sensors[5].Value;
        }

        public int getLeft()
        {
            return ikit.sensors[7].Value;
        }

        public int getRight()
        {
            return ikit.sensors[6].Value;
        }

        public void turnOnLights()
        {
            ikit.outputs[0] = true;
        }
        
        public void turnOffLights()
        {
            ikit.outputs[0] = false;
        }

        public void lossOfSignalOn()
        {
            ikit.outputs[1] = true;
        }

        public void lossOfSignalOff()
        {
            ikit.outputs[1] = false;
        }

        public void closePhidgetsController()
        {
            ikit.close();
        }
    }
}*/
