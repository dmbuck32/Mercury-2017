﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Timers;
using System.Xml.Serialization;
using Mars_Rover_Comms;
using Mars_Rover_RCU.Utilities;
using Mars_Rover_RCU.Controllers;

namespace Mars_Rover_RCU.Comms
{
    public sealed class RCUComms
    {
        private static RCUComms _instance;
        private static readonly object _instanceSync = new object();

        private XmlSerializer serializer;

        private System.Timers.Timer _worker;
        private readonly object _workerSync = new object();

        private XmlSerializer _serializer;

        //“The factory pattern is used to replace class constructors, abstracting the process of object generation 
        //so that the type of the object instantiated can be determined at run-time.” Factory method is just like 
        //regular method but when we are talking about patterns it just returns the instance of a class at run-time. 
        public static RCUComms Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_instanceSync)
                    {
                        if (_instance == null)
                            _instance = new RCUComms();
                    }
                }

                return _instance;
            }

        }

        private RCUComms()
        {
            serializer = new System.Xml.Serialization.XmlSerializer(typeof(RobotReturnState));

            _serializer = new System.Xml.Serialization.XmlSerializer(typeof(RobotReturnState));

            _worker = new System.Timers.Timer(1000);

            _worker.Elapsed += new ElapsedEventHandler(worker_Elapsed);

            _worker.Start();

        }

        //Responsible for returing arm feedback and onboard sensor data
        void worker_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (System.Threading.Monitor.TryEnter(_workerSync))
            {
                try
                {
                                     
                    DateTime lastdata = DateTime.MinValue;

                    RobotReturnState returnState = new RobotReturnState();

                    returnState.LogState.Data = Logger.getOutgoing();

                    returnState.ArmReturnState.ArmFeedback = 1;
                    returnState.ErrorReturnState.ErrorCode = 0xF;

                    returnState.PositionReturnState.frontDistance = Program.sensorData[0];
                    returnState.PositionReturnState.frontAmbient = Program.sensorData[3];

                    returnState.PositionReturnState.leftDistance = Program.sensorData[1];
                    returnState.PositionReturnState.leftAmbient = Program.sensorData[4];

                    returnState.PositionReturnState.rightDistance = Program.sensorData[2];
                    returnState.PositionReturnState.rightAmbient = Program.sensorData[5];

                    returnState.ArmReturnState.shoulderPos = Program.shoulderPos;
                    returnState.ArmReturnState.elbowPos = Program.elbowPos;
                    returnState.ArmReturnState.wristPos = Program.wristPos;
                    returnState.ArmReturnState.gripperPos = Program.gripperPos;
                    
                    // all UI control received, now send to robot clients
                    using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                    {
                        serializer.Serialize(ms, returnState);
                        Program.client.SendToOCUServer(ms);
                        //Console.WriteLine("UI Control Serialized and Sent to Robot Clients");
                    }
                   
                }
                catch (Exception ex)
                {
                    //log it or something!
                    //Console.WriteLine("Test");
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    System.Threading.Monitor.Exit(_workerSync);
                }
            }
        }
    }
}
