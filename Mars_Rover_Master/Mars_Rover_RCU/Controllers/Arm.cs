using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Runtime.InteropServices;
using System.IO;
using Mars_Rover_RCU.Components;
using Mars_Rover_Configuration;
using Mars_Rover_RCU.Utilities;

namespace Mars_Rover_RCU.Controllers
{
   class Arm
    {
        #region Arm Properties
        private const int comPort = 5;

        private ConfigureArm configuration;
        private volatile bool isActive = false;
       
        private CancellationTokenSource tokenSource;
        private Thread update_thread;
        public Utility.UpdateQueue<int> queue;

        private bool sequenceInProgress = false; 
        private SequenceStep currentSequenceStep = SequenceStep.None; 
        private ArmPosition armPosition;        
        private SequenceName sequenceName = SequenceName.None; 

        int leftStickX;
        int leftStickY;
        int rightStickX;
        int rightStickY;
        int leftTrigger;
        int rightTrigger;
        int leftShoulder;
        int rightShoulder;
        int aButton;
        int bButton;
        int xButton;
        int ybutton;
        int startButton;
        int backButton;
        int upButton;
        int rightButton;
        int downButton;
        int leftButton;

        private enum ButtonStatus
        {
            Pressed = 1,
            NotPressed = 0
        }
        
        private enum StickStatus
        {
            Positive = 1,
            Negative = -1,
            Idle = 0,
        }

        private enum ArmPosition
        {
            None = 0,
            InitialStow = 1,
            Stowed = 2,
            Ready = 3,
            Deposit = 4,
            Capture = 5
        }

        private enum SequenceStep
        {
            None = 0,
            RPFS_Prep = 1,
            RPFS_RaiseShoulder = 2,
            RPFS_RotateBase = 3,
            RPFS_AssumePose = 4,

            RPFIS_Prep = 5,
            RPFIS_RaiseShoulder = 6,
            RPFIS_TuckWrist = 7,
            RPFIS_RaiseShoulder2 = 8,
            RPFIS_LowerElbow = 9,
            RPFIS_RotateBase = 10,
            RPFIS_LowerShoulder = 11,

            DPFC_LiftShoulder = 12,
            DPFC_OrientArm = 13,
            DPFC_RotateBase = 15,
            DPFC_OrientArm2 = 16,
            DPFC_ManipulateClaw = 17,
            DPFC_RotateBase2 = 18,
            DPFC_LowerShoulder = 19,
            DPFC_Prep = 20,

            CPFR_Prep = 21,
            CPFR_LowerShoulder = 22
        }

        private enum SequenceName
        {
            None = 0,
            RPFIS = 1,
            RPFS = 2,
            CPFR = 3,
            DPFC = 4,
            SPFA = 5
        }

        // Servos that make up the arm
        private static MXServo baseServo;
        private static MXServo shoulderServo;
        private static MXServo elbowServo;
        private static AXServo wristServo;
        private static AXServo clawServo;

        #endregion

        #region Arm Constructors
        public Arm()
        {
            
        }

        public Arm(ConfigureArm config)
        {
            if (config == null)
            {
                throw new Exception("Arm: Null Config");
            }

            if (isActive)
            {
                throw new Exception("Arm already active.");
            }

            configuration = config;
            queue = new Utility.UpdateQueue<int>();
            isActive = true;
            tokenSource = new CancellationTokenSource();

            try
            {
                update_thread = new Thread(new ParameterizedThreadStart(p => UpdateWorker()));
                update_thread.Name = "Arm Worker Thread";
                update_thread.IsBackground = true;
                isActive = true;
                update_thread.Start();
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Arm Activate: " + ex.Message);
                CleanupUpdateWorker();
                isActive = false;
            }
        }

        private void UpdateWorker()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    int state = queue.Dequeue(tokenSource.Token);
                    ProcessCommand(state);
                }
                catch
                { }
            }
        }

        private void CleanupUpdateWorker()
        {
            tokenSource.Cancel();
            if (update_thread != null && update_thread.ThreadState != ThreadState.Unstarted)
            {
                update_thread.Join();
                update_thread = null;
            }
        }

        public void EnqueueState(int state)
        {
            try
            {
                queue.Enqueue(state);
            }
            catch (Exception ex)
            {
                Logger.WriteLine("Arm Enqueue: " + ex.Message);
            }
        }


        #endregion

        #region Scripted Motions

        public void ClearSequenceControl()
        {
            sequenceInProgress = false;
            sequenceName = SequenceName.None;
            currentSequenceStep = SequenceStep.None;
            armPosition = ArmPosition.None;
        }

        #region "Ready Position From Stow" Sequence (RPFS)

        public void RPFSSorter()
        {
            if (currentSequenceStep == SequenceStep.None)
            {
                RPFSPrep();
            }
            else if (currentSequenceStep == SequenceStep.RPFS_Prep)
            {
                RPFSRaiseShoulder();
            }
            else if (currentSequenceStep == SequenceStep.RPFS_RaiseShoulder)
            {
                RPFSRotateBase();
            }
            else if(currentSequenceStep == SequenceStep.RPFS_RotateBase)
            {
                RPFSAssumePose();
            }
            else if (currentSequenceStep == SequenceStep.RPFS_AssumePose)
            {
                ClearSequenceControl();
                armPosition = ArmPosition.Ready;
            }
            else
            {
                ClearSequenceControl();
            }
        }

        public void RPFSPrep()
        {
            currentSequenceStep = SequenceStep.RPFS_Prep;

            // Make sure all servos are set to move at the default speed with the default PID profile
            SetPID();
            SetDefaultSpeeds();
            SetDefaultStepSizes();

            // Enable torque on all motors and hold in place
            HoldPosition();
        }

        public void RPFSRaiseShoulder()
        {
            currentSequenceStep = SequenceStep.RPFS_RaiseShoulder;
            armPosition = ArmPosition.None;

            // Try to lift the shoulder, if it hasn't finished within 3 seconds then something went really wrong. Kill torque and pray.
            MoveShoulder(0, 1800);
            if (WaitOnShoulderLift(10) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void RPFSRotateBase()
        {
            currentSequenceStep = SequenceStep.RPFS_RotateBase;

            // Try to rotate the base
            RotateBase(0, 2000);
            if (WaitOnBaseRotate(10) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void RPFSAssumePose()
        {
            currentSequenceStep = SequenceStep.RPFS_AssumePose;

            MoveWrist(0, 460);
            MoveElbow(0, 1250);
            MoveShoulder(0, 1400);
            MoveClaw(0, clawServo.CWAngleLimit + 5);

            if ((WaitOnWrist(5) == false) || (WaitOnShoulderLift(5) == false) || (WaitOnElbow(5) == false) || WaitOnClaw(5) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        #endregion

        #region "Ready Position From Initial Stow" Sequence (RPFIS)
        public void RPFISSorter()
        {
            if (currentSequenceStep == SequenceStep.None)
            {
                RPFISPrep();
            }
            else if (currentSequenceStep == SequenceStep.RPFIS_Prep)
            {
                RPFISRaiseShoulder();
            }
            else if (currentSequenceStep == SequenceStep.RPFIS_RaiseShoulder)
            {
                RPFISTuckWrist();
            }
            else if (currentSequenceStep == SequenceStep.RPFIS_TuckWrist)
            {
                RPFISRaiseShoulder2();
            }
            else if (currentSequenceStep == SequenceStep.RPFIS_RaiseShoulder2)
            {
                RPFISLowerElbow();
            }
            else if (currentSequenceStep == SequenceStep.RPFIS_LowerElbow)
            {
                RPFISRotateBase();
            }
            else if (currentSequenceStep == SequenceStep.RPFIS_RotateBase)
            {
                RPFISLowerShoulder();
            }
            else if (currentSequenceStep == SequenceStep.RPFIS_LowerShoulder)
            {
                ClearSequenceControl();
                armPosition = ArmPosition.Stowed;
            }
            else
            {
                ClearSequenceControl();
            }
        }

        public void RPFISPrep()
        {
            currentSequenceStep = SequenceStep.RPFIS_Prep;

            // Make sure all servos are set to move at the default speed with the default PID profile
            SetPID();
            SetDefaultSpeeds();
            SetDefaultStepSizes();

            // Enable torque on all motors and hold in place
            HoldPosition();
        }

        public void RPFISRaiseShoulder()
        {
            currentSequenceStep = SequenceStep.RPFIS_RaiseShoulder;
            armPosition = ArmPosition.None;

            // Try to lift the shoulder, if it hasn't finished within 3 seconds then something went really wrong. Kill torque and pray.
            MoveShoulder(0, 1400);
            if (WaitOnShoulderLift(7) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void RPFISTuckWrist()
        {
            currentSequenceStep = SequenceStep.RPFIS_TuckWrist;

            // Try to lift the shoulder, if it hasn't finished within 3 seconds then something went really wrong. Kill torque and pray.
            MoveClaw(0, 600);
            MoveWrist(0, 115);
            if (WaitOnWrist(7) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void RPFISRaiseShoulder2()
        {
            currentSequenceStep = SequenceStep.RPFIS_RaiseShoulder2;

            // Try to lift the shoulder, if it hasn't finished within 3 seconds then something went really wrong. Kill torque and pray.
            MoveShoulder(0, 1700);
            if (WaitOnShoulderLift(7) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void RPFISLowerElbow()
        {
            currentSequenceStep = SequenceStep.RPFIS_LowerElbow;

            // Try to lift the shoulder, if it hasn't finished within 3 seconds then something went really wrong. Kill torque and pray.
            MoveElbow(0, 800);
            if (WaitOnElbow(7) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void RPFISRotateBase()
        {
            currentSequenceStep = SequenceStep.RPFIS_RotateBase;

            // Try to lift the shoulder, if it hasn't finished within 3 seconds then something went really wrong. Kill torque and pray.
            RotateBase(0, 665);
            if (WaitOnBaseRotate(7) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void RPFISLowerShoulder()
        {
            currentSequenceStep = SequenceStep.RPFIS_LowerShoulder;

            // Try to lift the shoulder, if it hasn't finished within 3 seconds then something went really wrong. Kill torque and pray.
            MoveShoulder(0, 1495);
            if (WaitOnShoulderLift(7) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }

            GoLimp();
        }
        
        #endregion

        #region "Capture Position From Ready" Sequence (CPFR)

        public void CPFRSorter()
        {
            if (currentSequenceStep == SequenceStep.None)
            {
                CPFRPrep();
            }
            else if (currentSequenceStep == SequenceStep.CPFR_Prep)
            {
                CPFRLowerShoulder();
            }
            else if (currentSequenceStep == SequenceStep.CPFR_LowerShoulder)
            {
                ClearSequenceControl();
                armPosition = ArmPosition.Capture;
            }
            else
            {
                ClearSequenceControl();
            }
        }

        public void CPFRPrep()
       {
           currentSequenceStep = SequenceStep.CPFR_Prep;

           SetPID();
           SetDefaultSpeeds();
           SetDefaultStepSizes();
       }

        public void CPFRLowerShoulder()
       {
           currentSequenceStep = SequenceStep.CPFR_LowerShoulder;

           armPosition = ArmPosition.None;

           MoveShoulder(0, 1000);
           if (WaitOnShoulderLift(7) == false)
           {
               HoldPosition();
               ClearSequenceControl();
           }
       }
        #endregion

        #region "Deposit Position From Capture" Sequence (DPFC)

        public void DPFCSorter()
        {
            Logger.WriteLine("At Sorter...");

            if (currentSequenceStep == SequenceStep.None)
            {
                Logger.WriteLine("Prep...");
                DPFCPrep();
            }
            if (currentSequenceStep == SequenceStep.DPFC_Prep)
            {
                Logger.WriteLine("Lift Shoulder...");
                DPFC_LiftShoulder();
            }
            else if (currentSequenceStep == SequenceStep.DPFC_LiftShoulder)
            {
                Logger.WriteLine("Orient Arm...");
                DPFC_OrientArm();
            }
            else if (currentSequenceStep == SequenceStep.DPFC_OrientArm)
            {
                Logger.WriteLine("Rotate Base...");
                DPFC_RotateBase();
            }
            else if (currentSequenceStep == SequenceStep.DPFC_RotateBase)
            {
                Logger.WriteLine("Orient Arm 2...");
                DPFC_OrientArm2();
            }
            else if (currentSequenceStep == SequenceStep.DPFC_OrientArm2)
            {
                Logger.WriteLine("Drop from claw...");
                DPFC_ManipulateClaw();
            }
            else if (currentSequenceStep == SequenceStep.DPFC_ManipulateClaw)
            {
                Logger.WriteLine("Rotate Base 2...");
                DPFC_RotateBase2();
            }
            else if (currentSequenceStep == SequenceStep.DPFC_RotateBase2)
            {
                Logger.WriteLine("Lower Shoulder...");
                DPFC_LowerShoulder();
            }
            else if (currentSequenceStep == SequenceStep.DPFC_LowerShoulder)
            {
                ClearSequenceControl();
                armPosition = ArmPosition.Stowed;
            }
            else
            {
                ClearSequenceControl();
            }
        }

        public void DPFCPrep()
        {
            currentSequenceStep = SequenceStep.DPFC_Prep;

            // Make sure all servos are set to move at the default speed with the default PID profile
            SetPID();
            SetDefaultSpeeds();
            SetDefaultStepSizes();

            // Enable torque on all motors and hold in place
            HoldPosition();
        }

        public void DPFC_LiftShoulder()
        {
            currentSequenceStep = SequenceStep.DPFC_LiftShoulder;
            armPosition = ArmPosition.None;

            // Try to lift the shoulder, if it hasn't finished within 3 seconds then something went really wrong. Kill torque and pray.
            MoveShoulder(0, shoulderServo.PresentPosition + 300);
            if (WaitOnShoulderLift(10) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void DPFC_OrientArm()
        {
            currentSequenceStep = SequenceStep.DPFC_OrientArm;

            MoveWrist(0, 188);
            MoveElbow(0, 803);
            MoveShoulder(0, 2170);

            if ((WaitOnWrist(10) == false) || (WaitOnShoulderLift(10) == false) || (WaitOnElbow(10) == false))
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void DPFC_RotateBase()
        {
            currentSequenceStep = SequenceStep.DPFC_RotateBase;

            // Try to rotate the base
            RotateBase(0, 820);
            if (WaitOnBaseRotate(10) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void DPFC_OrientArm2()
        {
            currentSequenceStep = SequenceStep.DPFC_OrientArm2;

            MoveWrist(0, 123);
            MoveShoulder(0, 1625);

            if ((WaitOnWrist(5) == false) || (WaitOnShoulderLift(5) == false))
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void DPFC_ManipulateClaw()
        {
            currentSequenceStep = SequenceStep.DPFC_ManipulateClaw;

            MoveClaw(0, 375);
            WaitOnClaw(7);

            RotateBase(-1, 50);
            WaitOnBaseRotate(2);
            RotateBase(1, 50);
            WaitOnBaseRotate(2);
            RotateBase(-1, 50);
            WaitOnBaseRotate(2);
            RotateBase(1, 50);
            WaitOnBaseRotate(2);
            
            MoveClaw(0, 600);
            WaitOnClaw(7);
        }

        public void DPFC_RotateBase2()
        {
            currentSequenceStep = SequenceStep.DPFC_RotateBase2;

            // Try to rotate the base
            RotateBase(0, 665);
            if (WaitOnBaseRotate(7) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }
        }

        public void DPFC_LowerShoulder()
        {
            currentSequenceStep = SequenceStep.DPFC_LowerShoulder;

            MoveShoulder(0, 1495);
            
            if (WaitOnShoulderLift(7) == false)
            {
                HoldPosition();
                ClearSequenceControl();
            }

            GoLimp();
        }

        #endregion

        #region "Stow Position From Anywhere" Sequence (SPFA)

        #endregion

        #endregion

        #region Command Processing

        public bool ProcessCommand(int compressedInput)
        {
            // The input is in a compressed format. Break it back out into individual game pad variables.
            leftStickX = (compressedInput & 1) + ((-2) * (compressedInput & 2) >> 1);
            leftStickY = ((compressedInput & 4) >> 2) + ((-2) * (compressedInput & 8) >> 3);
            rightStickX = ((compressedInput & 16) >> 4) + ((-2) * (compressedInput & 32) >> 5);
            rightStickY = ((compressedInput & 64) >> 6) + ((-2) * (compressedInput & 128) >> 7);
            leftTrigger = (compressedInput & 256) >> 8;
            rightTrigger = (compressedInput & 512) >> 9;
            leftShoulder = (compressedInput & 1024) >> 10;
            rightShoulder = (compressedInput & 2048) >> 11;
            aButton = (compressedInput & 4096) >> 12;
            bButton = (compressedInput & 8192) >> 13;
            xButton = (compressedInput & 16384) >> 14;
            ybutton = (compressedInput & 32768) >> 15;
            startButton = (compressedInput & 65536) >> 16;
            backButton = (compressedInput & 131072) >> 17;
            upButton = (compressedInput & 262144) >> 18;
            rightButton = (compressedInput & 524288) >> 19;
            downButton = (compressedInput & 1048576) >> 20;
            leftButton = (compressedInput & 2097152) >> 21;



            // Look for the combination that will terminate the command processing routine immediately.
            if ((backButton == (int)ButtonStatus.Pressed) && (startButton == (int)ButtonStatus.Pressed))
            {
                ClearSequenceControl();
                GoLimp();
                Logger.WriteLine("Released all torque. Arm is now limp.");
                return true;
            }

            if (sequenceInProgress)
            {
                // Start + Y button will terminate the sequence.
                if ((startButton == (int)ButtonStatus.Pressed) && (ybutton == (int)ButtonStatus.Pressed))
                {
                    // Cancelling that sequence
                    sequenceInProgress = false;
                    currentSequenceStep = SequenceStep.None;
                    armPosition = ArmPosition.None;
                    sequenceName = SequenceName.None;

                    Logger.WriteLine("Sequence cancelled.");
                }
                // Didn't cancel, so send control to the sequence sorter
                else
                {
                    if (sequenceName == SequenceName.RPFS)
                    {
                        Logger.WriteLine("Moving from Stowed to Ready Position...");
                        RPFSSorter();
                    }
                    else if (sequenceName == SequenceName.RPFIS)
                    {
                        Logger.WriteLine("Moving from Initial Stow to Ready Position...");
                        RPFISSorter();
                    }
                    else if (sequenceName == SequenceName.DPFC)
                    {
                        Logger.WriteLine("Depositing Rock Sample...");
                        DPFCSorter();
                    }
                    else if (sequenceName == SequenceName.CPFR)
                    {
                        Logger.WriteLine("Moving to Capture Position...");
                        CPFRSorter();
                    }
                    // Unrecognized sequence name, clear all sequence variables
                    else
                    {
                        sequenceInProgress = false;
                        currentSequenceStep = SequenceStep.None;
                        armPosition = ArmPosition.None;
                        sequenceName = SequenceName.None;
                        Logger.WriteLine("Sequence name was not recognized. Aborted sequence initiation.");
                    }
                }

                return true;
            }

            // Diagnostic stuff
            if ((backButton == (int)ButtonStatus.Pressed) && (upButton == (int)ButtonStatus.Pressed))
            {
                ShoulderRotateVitals();   
            }
            else if ((backButton == (int)ButtonStatus.Pressed) && (downButton == (int)ButtonStatus.Pressed)) 
            {
                ShoulderLiftVitals(); 
            }
            else if ((backButton == (int)ButtonStatus.Pressed) && (leftButton == (int)ButtonStatus.Pressed))
            {
                ElbowVitals(); 
            }
            else if ((backButton == (int)ButtonStatus.Pressed) && (rightButton == (int)ButtonStatus.Pressed))
            {
                WristVitals();
                ClawVitals();
            }
            else if ((startButton == (int)ButtonStatus.Pressed) && (upButton == (int)ButtonStatus.Pressed))
            {
                Logger.WriteLine("Arm position: " + armPosition.ToString());
                Logger.WriteLine("Sequence active: " + sequenceInProgress.ToString());
                Logger.WriteLine("Sequence name: " + sequenceName.ToString());
                Logger.WriteLine("Sequence step: " + currentSequenceStep.ToString());
            }
            else if ((startButton == (int)ButtonStatus.Pressed) && (downButton == (int)ButtonStatus.Pressed))
            {
                Logger.WriteLine("Base: " + baseServo.PresentPosition + ", " + baseServo.PresentVoltage + "V, " + baseServo.PresentTemperature + "C");
                Logger.WriteLine("Shoulder: " + shoulderServo.PresentPosition + ", " + shoulderServo.PresentVoltage + "V, " + shoulderServo.PresentTemperature + "C");
                Logger.WriteLine("Elbow: " + elbowServo.PresentPosition + ", " + elbowServo.PresentVoltage + "V, " + elbowServo.PresentTemperature + "C");
                Logger.WriteLine("Wrist: " + wristServo.PresentPosition + ", " + wristServo.PresentVoltage + "V, " + wristServo.PresentTemperature + "C");
                Logger.WriteLine("Claw: " + clawServo.PresentPosition + ", " + clawServo.PresentVoltage + "V, " + clawServo.PresentTemperature + "C");
            }

            // Manually override arm positions
            if ((startButton == (int)ButtonStatus.Pressed) && (xButton == (int)ButtonStatus.Pressed))
            {
                armPosition = ArmPosition.Stowed;
                Logger.WriteLine("Arm position override: " + armPosition.ToString());
            }
            else if ((startButton == (int)ButtonStatus.Pressed) && (bButton == (int)ButtonStatus.Pressed))
            {
                armPosition = ArmPosition.Ready;
                Logger.WriteLine("Arm position override: " + armPosition.ToString());
            }
            else if ((startButton == (int)ButtonStatus.Pressed) && (aButton == (int)ButtonStatus.Pressed))
            {
                armPosition = ArmPosition.InitialStow;
                Logger.WriteLine("Arm position override: " + armPosition.ToString());
            }

            // No sequences going on, so do some manual stuff
            // Reduces step size and increases gain for base so we can have shorter, more precise movements (but possibly jumpy)
            if (leftShoulder == (int)ButtonStatus.Pressed)
            {
                baseServo.StepSize = 10;
                baseServo.PGain = 32;
                baseServo.MovingSpeed = 36;

                shoulderServo.StepSize = 10;
                shoulderServo.PGain = 16;
                shoulderServo.MovingSpeed = 20;

                elbowServo.stepSize = 10;
                elbowServo.PGain = 16;
                elbowServo.MovingSpeed = 20;

                wristServo.stepSize = 10;
                wristServo.MovingSpeed = 50;

                clawServo.stepSize = 6;
                clawServo.MovingSpeed = 75;
            }
            // Increases step size, increases base servo's gain, and increases moving speed. Good for powerful, manly movements.
            else if (leftTrigger == (int)ButtonStatus.Pressed)
            {
                baseServo.StepSize = 120;
                baseServo.PGain = 32;
                baseServo.MovingSpeed = 50;

                shoulderServo.StepSize = 160;
                shoulderServo.PGain = 32;
                shoulderServo.MovingSpeed = 100;

                elbowServo.stepSize = 160;
                elbowServo.PGain = 32;
                elbowServo.MovingSpeed = 100;

                wristServo.stepSize = 60;

                clawServo.stepSize = 60;
            }
            // If no modifiers then restore the default movement profiles
            else
            {
                baseServo.stepSize = baseServo.DefaultStepSize;
                baseServo.PGain = 10;
                baseServo.MovingSpeed = 36;

                shoulderServo.stepSize = shoulderServo.DefaultStepSize;
                shoulderServo.PGain = 32;
                shoulderServo.MovingSpeed = 36;

                elbowServo.stepSize = elbowServo.DefaultStepSize;
                elbowServo.PGain = 32;
                elbowServo.MovingSpeed = 36;

                wristServo.stepSize = wristServo.DefaultStepSize;
                wristServo.MovingSpeed = 83;

                clawServo.stepSize = clawServo.DefaultStepSize;
                clawServo.MovingSpeed = 112;
            }

            // Right stick X-direction rotates the base
            if (rightStickX != (int)StickStatus.Idle)
            {
                RotateBase(rightStickX);
                armPosition = ArmPosition.None;
            }

            // Right stick Y-direction controls shoulder servo, unless right trigger is pressed, then controls wrist servo.
            if (rightStickY != (int)StickStatus.Idle)
            {
                if (rightShoulder == (int)ButtonStatus.Pressed)
                {
                    MoveWrist(rightStickY);
                    armPosition = ArmPosition.None;
                }
                else
                {
                    MoveShoulder(rightStickY);
                    armPosition = ArmPosition.None;
                }
            }

            // Left stick X-direction controls the gripper
            if (leftStickX != (int)StickStatus.Idle)
            {
                MoveClaw(leftStickX);
                armPosition = ArmPosition.None;
            }

            // Left stick Y-direction controls the elbow
            if (leftStickY != (int)StickStatus.Idle)
            {
                MoveElbow(leftStickY);
                armPosition = ArmPosition.None;
            }

            // Let the arm sag to the ground rather than driving it into the ground.
            if (downButton == (int)ButtonStatus.Pressed)
            {
                HoldPosition();
                armPosition = ArmPosition.None;
            }

            // Close the claw, or open it if the right trigger modifier is pressed
            if (rightTrigger == (int)ButtonStatus.Pressed)
            {
                if (rightShoulder == (int)ButtonStatus.Pressed)
                {
                    OpenClaw();
                    armPosition = ArmPosition.None;
                }
                else
                {
                    CloseClaw();
                    armPosition = ArmPosition.None;
                }
            }

            // Look for the "Ready Position From Stowed" (RPFS) combination = Back button + Y
            if ((backButton == (int)ButtonStatus.Pressed) && (ybutton == (int)ButtonStatus.Pressed))
            {
                // Check if we are in the inital stow position
                if (armPosition == ArmPosition.InitialStow)
                {
                    sequenceInProgress = true;
                    sequenceName = SequenceName.RPFIS;
                    currentSequenceStep = SequenceStep.None;
                }

                // Check to make sure we are actually in the stowed position first
                else if (armPosition == ArmPosition.Stowed)
                {
                    sequenceInProgress = true;
                    sequenceName = SequenceName.RPFS;
                    currentSequenceStep = SequenceStep.None;
                }
                else
                {
                    Logger.WriteLine("Tried to enter RPFS or RPFIS sequence, but arm position is: " + armPosition.ToString());
                }
            }

            // Look for the "Deposit Position From Capture" (DPFC) combinaton = Back Button + a
            if ((backButton == (int)ButtonStatus.Pressed) && (aButton == (int)ButtonStatus.Pressed))
            {
                if ((armPosition != ArmPosition.Stowed) && (armPosition != ArmPosition.InitialStow))
                {
                    sequenceInProgress = true;
                    sequenceName = SequenceName.DPFC;
                    currentSequenceStep = SequenceStep.None;
                }
                else
                {
                    Logger.WriteLine("Tried to enter DPFC sequence, but arm position is: " + armPosition.ToString());
                }
            }

            // Look for the "Capture Position From Ready" (CPFR) combination = Back Button + X
            if ((backButton == (int)ButtonStatus.Pressed) && (xButton == (int)ButtonStatus.Pressed))
            {
                if (armPosition == ArmPosition.Ready)
                {
                    sequenceInProgress = true;
                    sequenceName = SequenceName.CPFR;
                    currentSequenceStep = SequenceStep.None;
                }
                else
                {
                    Logger.WriteLine("Tried to enter CPFR sequence, but arm position is: " + armPosition.ToString());
                }
            }

            return true;
        }

        #endregion

        #region Joint Control

        #region Base Rotating
        public void RotateBase(int direction, int position = 0)
        {
            // Position is a goal position to move to
            if (direction == 0)
            {
                baseServo.GoalPosition = position;
            }

            // Move CW or CCW by the default step size amount
            else if (position == 0)
            {
                if (direction == 1)
                {
                    baseServo.GoalPosition = baseServo.PresentPosition - baseServo.StepSize;
                }
                else if (direction == -1)
                {
                    baseServo.GoalPosition = baseServo.PresentPosition + baseServo.StepSize;
                }
            }

            // Use "position" as a custom step size to move CW or CCW
            else
            {
                if (direction == 1)
                {
                    baseServo.GoalPosition = baseServo.PresentPosition - position;
                }
                else if (direction == -1)
                {
                    baseServo.GoalPosition = baseServo.PresentPosition + position;
                }
            }
        }

        public bool BaseRotating()
        {
            return baseServo.IsMoving();
        }

        public bool WaitOnBaseRotate(int timeout = 0)
        {
            if (timeout != 0)
            {
                DateTime startTime = new DateTime();
                DateTime currentTime = new DateTime();

                startTime = DateTime.Now;

                while (BaseRotating())
                {
                    currentTime = DateTime.Now;

                    if ((currentTime - startTime).Seconds > timeout)
                    {
                        return false;
                    }
                }
            }
            else
            {
                while (BaseRotating()) ;
            }

            return true;
        }

        #endregion

        #region Shoulder Movement
        public void MoveShoulder(int direction, int position = 0)
        {
            // Position is a goal position to move to
            if (direction == 0)
            {
                shoulderServo.GoalPosition = position;
            }

            // Move CW or CCW by the default step size amount
            else if (position == 0)
            {
                if (direction == 1)
                {
                    shoulderServo.GoalPosition = shoulderServo.PresentPosition + shoulderServo.StepSize;
                }
                else if (direction == -1)
                {
                    shoulderServo.GoalPosition = shoulderServo.PresentPosition - shoulderServo.StepSize;
                }

            // Use "position" as a custom step size to move CW or CCW    
            }
            else
            {
                if (direction == 1)
                {
                    shoulderServo.GoalPosition = shoulderServo.PresentPosition + position;
                }
                else if (direction == -1)
                {
                    shoulderServo.GoalPosition = shoulderServo.PresentPosition - position;
                }
            }
        }

        public bool ShoulderMoving()
        {
            return shoulderServo.IsMoving();
        }

        public bool WaitOnShoulderLift(int timeout = 0)
        {
            if (timeout != 0)
            {
                DateTime startTime = new DateTime();
                DateTime currentTime = new DateTime();

                startTime = DateTime.Now;

                while (ShoulderMoving())
                {
                    currentTime = DateTime.Now;

                    if ((currentTime - startTime).Seconds > timeout)
                    {
                        return false;
                    }
                }
            }
            else
            {
                while (ShoulderMoving()) ;
            }

            return true;
        }

        #endregion

        #region Elbow
        public void MoveElbow(int direction, int position = 0)
        {
            // Position is a goal position to move to
            if (direction == 0)
            {
                elbowServo.GoalPosition = position;
            }

            // Move CW or CCW by the default step size amount
            else if (position == 0)
            {
                if (direction == 1)
                {
                    elbowServo.GoalPosition = elbowServo.PresentPosition + elbowServo.StepSize;
                }
                else if (direction == -1)
                {
                    elbowServo.GoalPosition = elbowServo.PresentPosition - elbowServo.StepSize;
                }
            }
            // Use "position" as a custom step size to move CW or CCW
            else
            {
                if (direction == 1)
                {
                    elbowServo.GoalPosition = elbowServo.PresentPosition + position;
                }
                else if (direction == -1)
                {
                    elbowServo.GoalPosition = elbowServo.PresentPosition - position;
                }
            }
        }

        public bool ElbowMoving()
        {
            return elbowServo.IsMoving();
        }

        public bool WaitOnElbow(int timeout = 0)
        {
            if (timeout != 0)
            {
                DateTime startTime = new DateTime();
                DateTime currentTime = new DateTime();

                startTime = DateTime.Now;

                while (ElbowMoving())
                {
                    currentTime = DateTime.Now;

                    if ((currentTime - startTime).Seconds > timeout)
                    {
                        return false;
                    }
                }
            }
            else
            {
                while (ElbowMoving()) ;
            }

            return true;
        }

        #endregion

        #region Wrist
        public void MoveWrist(int direction, int position = 0)
        {
            // Position is a goal position to move to
            if (direction == 0)
            {
                wristServo.GoalPosition = position;
            }

            // Move CW or CCW by the default step size amount
            else if (position == 0)
            {
                if (direction == 1)
                {
                    wristServo.GoalPosition = wristServo.PresentPosition + wristServo.StepSize;
                }
                else if (direction == -1)
                {
                    wristServo.GoalPosition = wristServo.PresentPosition - wristServo.StepSize;
                }
            }
            // Use "position" as a custom step size to move CW or CCW
            else
            {
                if (direction == 1)
                {
                    wristServo.GoalPosition = wristServo.PresentPosition + position;
                }
                else if (direction == -1)
                {
                    wristServo.GoalPosition = wristServo.PresentPosition - position;
                }
            }
        }

        public bool WristMoving()
        {
            return wristServo.IsMoving();
        }

        public bool WaitOnWrist(int timeout = 0)
        {
            if (timeout != 0)
            {
                DateTime startTime = new DateTime();
                DateTime currentTime = new DateTime();

                startTime = DateTime.Now;

                while (WristMoving())
                {
                    currentTime = DateTime.Now;

                    if ((currentTime - startTime).Seconds > timeout)
                    {
                        return false;
                    }
                }
            }
            else
            {
                while (WristMoving()) ;
            }

            return true;
        }

        #endregion

        #region Claw
        public void MoveClaw(int direction, int position = 0)
        {
            // Position is a goal position to move to
            if (direction == 0)
            {
                clawServo.GoalPosition = position;
            }

            // Move CW or CCW by the default step size amount
            else if (position == 0)
            {
                if (direction == 1)
                {
                    clawServo.GoalPosition = clawServo.PresentPosition - clawServo.StepSize;
                }
                else if (direction == -1)
                {
                    clawServo.GoalPosition = clawServo.PresentPosition + clawServo.StepSize;
                }
            }
            // Use "position" as a custom step size to move CW or CCW
            else
            {
                if (direction == 1)
                {
                    clawServo.GoalPosition = clawServo.PresentPosition - position;
                }
                else if (direction == -1)
                {
                    clawServo.GoalPosition = clawServo.PresentPosition + position;
                }
            }
        }

        public bool ClawMoving()
        {
            return clawServo.IsMoving();
        }

        public bool WaitOnClaw(int timeout = 0)
        {
            if (timeout != 0)
            {
                DateTime startTime = new DateTime();
                DateTime currentTime = new DateTime();

                startTime = DateTime.Now;

                while (ClawMoving())
                {
                    currentTime = DateTime.Now;

                    if ((currentTime - startTime).Seconds > timeout)
                    {
                        return false;
                    }
                }
            }
            else
            {
                while (ClawMoving()) ;
            }

            return true;
        }

        public void CloseClaw(int timeout = 0, int speed = 0)
        {
            clawServo.MovingSpeed = 0;
            MoveClaw(0, clawServo.CCWAngleLimit - 5);
            WaitOnClaw(1);
            clawServo.HoldPosition();
            clawServo.MovingSpeed = 112;    
        }

        public void OpenClaw(int timeout = 0, int speed = 0)
        {
            clawServo.MovingSpeed = speed;
            MoveClaw(0, clawServo.CWAngleLimit + 5);
            WaitOnClaw(timeout);
            clawServo.MovingSpeed = 112;
        }

        #endregion

        #region All at Once Functions
        public void GoLimp()
        {
            clawServo.ReleaseTorque();
            wristServo.ReleaseTorque();
            elbowServo.ReleaseTorque();
            shoulderServo.ReleaseTorque();
            baseServo.ReleaseTorque();
        }

        public void HoldPosition()
        {
            shoulderServo.HoldPosition();
            baseServo.HoldPosition();
            elbowServo.HoldPosition();
            wristServo.HoldPosition();
            clawServo.HoldPosition();
        }

        #endregion

        #endregion
        
        #region Servo Initialization
        /// <summary>
        /// Creates servo objects for each joint.
        /// </summary>
        public void InitializeServos(bool startRigid = true, bool showResults = false)
        {
            MakeServoObjects();
            SetAngleLimits();
            SetDefaultSpeeds();
            SetPID();
            SetDefaultStepSizes();
            HoldPosition();

            EstablishStartingPosition();

            GoLimp();

            Logger.WriteLine("Arm Initialized. \nStarting Position: " + armPosition.ToString());

            if (showResults)
            {
                DumpStats(true);
            }
        }

        public void MakeServoObjects()
        {
            baseServo = new MXServo(1);
            shoulderServo = new MXServo(2);
            elbowServo = new MXServo(3);
            wristServo = new AXServo(4);
            clawServo = new AXServo(5);
        }

        /// <summary>
        /// Prevents a goal position command from moving the servo past it's safe range (won't try to push past where the brackets allow).
        /// </summary>
        public void SetAngleLimits()
        {
            // Shoulder rotating limits
            baseServo.CWAngleLimit = 0;
            baseServo.CCWAngleLimit = 4096;

            // Shoulder lifting limits
            shoulderServo.CWAngleLimit = 700;
            shoulderServo.CCWAngleLimit = 3150;

            // Elbow Limits
            elbowServo.CWAngleLimit = 800;
            elbowServo.CCWAngleLimit = 3275;

            // Wrist Limits
            wristServo.CWAngleLimit = 110;
            wristServo.CCWAngleLimit = 900;

            // Gripper Limits
            clawServo.CWAngleLimit = 80;
            clawServo.CCWAngleLimit = 605;
        }

        /// <summary>
        /// 
        /// </summary>
        public void SetDefaultSpeeds()
        {
            baseServo.MovingSpeed = 36;
            shoulderServo.MovingSpeed = 36;
            elbowServo.MovingSpeed = 36;
            wristServo.MovingSpeed = 83;
            wristServo.ReleaseTorque();
            clawServo.MovingSpeed = 112;
            clawServo.ReleaseTorque();
        }

        public void SetDefaultStepSizes()
        {
            baseServo.DefaultStepSize = 80;
            shoulderServo.DefaultStepSize = 80;
            elbowServo.DefaultStepSize = 80;
            wristServo.DefaultStepSize = 30;
            clawServo.DefaultStepSize = 30;
        }

        public void SetPID()
        {
            baseServo.PGain = 24;
            baseServo.IGain = 0;
            baseServo.DGain = 0;

            shoulderServo.PGain = 32;
            shoulderServo.IGain = 0;
            shoulderServo.DGain = 0;

            elbowServo.PGain = 32;
            elbowServo.IGain = 0;
            elbowServo.DGain = 0;
        }

        public void EstablishStartingPosition()
        {            
            if (shoulderServo.PresentPosition > 1150)
            {
                armPosition = ArmPosition.Stowed;
            }
            else
            {
                armPosition = ArmPosition.InitialStow;
            }
        }

        #endregion
        
        #region Servo Classes
        /// <summary>
        /// Contains the core functionality for communicating with a network of Dyamixel servos through USB
        /// Implementation contained in library: dynamixel.dll
        /// Library is built from C-source files: dxl_hal.c, dxl_hal.h, dynamixel.c
        /// Must allow unsafe code in build configuration: Project -> Properties -> Build -> Allow unsafe code (checkbox)
        /// </summary>
        public unsafe class Dynamixel
        {
            #region Dynamixel Constants
            public const int MAXNUM_TXPARAM = 150;
            public const int MAXNUM_RXPARAM = 60;
            public const int BROADCAST_ID = 254;

            public const int INST_PING = 1;
            public const int INST_READ = 2;
            public const int INST_WRITE = 3;
            public const int INST_REG_WRITE = 4;
            public const int INST_ACTION = 5;
            public const int INST_RESET = 6;
            public const int INST_SYNC_WRITE = 131;

            public const int ERRBIT_VOLTAGE = 1;
            public const int ERRBIT_ANGLE = 2;
            public const int ERRBIT_OVERHEAT = 4;
            public const int ERRBIT_RANGE = 8;
            public const int ERRBIT_CHECKSUM = 16;
            public const int ERRBIT_OVERLOAD = 32;
            public const int ERRBIT_INSTRUCTION = 64;

            public const int COMM_TXSUCCESS = 0;
            public const int COMM_RXSUCCESS = 1;
            public const int COMM_TXFAIL = 2;
            public const int COMM_RXFAIL = 3;
            public const int COMM_TXERROR = 4;
            public const int COMM_RXWAITING = 5;
            public const int COMM_RXTIMEOUT = 6;
            public const int COMM_RXCORRUPT = 7;
            #endregion

            #region Imported Dynamixel Functions
            [DllImport("dynamixel.dll")]
            public static extern int dxl_initialize(int devIndex, int baudnum);

            [DllImport("dynamixel.dll")]
            public static extern void dxl_terminate();

            [DllImport("dynamixel.dll")]
            public static extern int dxl_get_result();

            [DllImport("dynamixel.dll")]
            public static extern void dxl_tx_packet();

            [DllImport("dynamixel.dll")]
            public static extern void dxl_rx_packet();

            [DllImport("dynamixel.dll")]
            public static extern void dxl_txrx_packet();

            [DllImport("dynamixel.dll")]
            public static extern void dxl_set_txpacket_id(int id);

            [DllImport("dynamixel.dll")]
            public static extern void dxl_set_txpacket_instruction(int instruction);

            [DllImport("dynamixel.dll")]
            public static extern void dxl_set_txpacket_parameter(int index, int value);

            [DllImport("dynamixel.dll")]
            public static extern void dxl_set_txpacket_length(int length);

            [DllImport("dynamixel.dll")]
            public static extern int dxl_get_rxpacket_error(int errbit);

            [DllImport("dynamixel.dll")]
            public static extern int dxl_get_rxpacket_length();

            [DllImport("dynamixel.dll")]
            public static extern int dxl_get_rxpacket_parameter(int index);

            [DllImport("dynamixel.dll")]
            public static extern int dxl_makeword(int lowbyte, int highbyte);

            [DllImport("dynamixel.dll")]
            public static extern int dxl_get_lowbyte(int word);

            [DllImport("dynamixel.dll")]
            public static extern int dxl_get_highbyte(int word);

            [DllImport("dynamixel.dll")]
            public static extern void dxl_ping(int id);

            [DllImport("dynamixel.dll")]
            public static extern int dxl_read_byte(int id, int address);

            [DllImport("dynamixel.dll")]
            public static extern void dxl_write_byte(int id, int address, int value);

            [DllImport("dynamixel.dll")]
            public static extern int dxl_read_word(int id, int address);

            [DllImport("dynamixel.dll")]
            public static extern void dxl_write_word(int id, int address, int value);
            #endregion
        }

        // Encapsulates the functionality of a Dynamixel servo (base class for AX and MX series)
        public class DynamixelServo
        {
            #region DynamixelServo Properties
            // Control table entries that are common between the AX-series and MX-series dynamixels
            public enum ServoField
            {
                ModelNumber = 0,
                FirmwareVersion = 2,
                ID = 3,
                BaudRate = 4,
                ReturnDelayTime = 5,
                CWAngleLimit = 6,
                CCWAngleLimit = 8,
                MaxTemperature = 11,
                MinVoltage = 12,
                MaxVoltage = 13,
                MaxTorque = 14,
                StatusReturnLevel = 16,
                AlarmLED = 17,
                AlarmShutdown = 18,
                TorqueEnable = 24,
                LED = 25,
                GoalPosition = 30,
                MovingSpeed = 32,
                TorqueLimit = 34,
                PresentPosition = 36,
                PresentSpeed = 38,
                PresentLoad = 40,
                PresentVoltage = 42,
                PresentTemperature = 43,
                Registered = 44,
                Moving = 46,
                LockEEPROM = 47,
                Punch = 48
            }

            public int servoID;
            public int stepSize;
            public int defaultStepSize;
            #endregion

            #region DynamixelServo Constructors
            /// <summary>
            /// Default constructor.
            /// </summary>
            public DynamixelServo()
            {

            }
            #endregion

            #region DynamixelServo Accessors
            /// <summary>
            /// Model number of the Dynamixel servo. Value is read-only and is retrieved from Dynamixel at initialization.
            /// </summary>
            public int ModelNumber
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.ModelNumber);
                }
            }

            /// <summary>
            /// Version of firmware installed. Value is read-only and is retrieved from Dynamixel at initialization.
            /// </summary>
            public int Firmware
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.FirmwareVersion);
                }
            }

            /// <summary>
            /// ID associated with the Dynamixel. Must be unique across the Dynamixel network.
            /// </summary>
            public int ServoID
            {
                get
                {
                    return this.servoID;
                }
                set
                {
                    if ((value >= 0) && (value <= 254))
                    {
                        WriteByte(this.servoID, (int)ServoField.ID, value);
                        this.servoID = value;
                    }
                }
            }

            /// <summary>
            /// Baud rate used for coummunicating through the COM port.
            /// </summary>
            public int BaudRate
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.BaudRate);
                }
                set
                {
                    if ((value >= 0) && (value <= 254))
                    {
                        do { WriteByte(this.servoID, (int)ServoField.BaudRate, value); }
                        while (ReadByte(this.servoID, (int)ServoField.BaudRate) != value);
                    }
                }
            }

            /// <summary>
            /// The amount of time the Dynamixel delays before sending a return packet in response to a read or write operation.
            /// </summary>
            public int ReturnDelay
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.ReturnDelayTime);
                }
                set
                {
                    if ((value >= 0) && (value < 255))
                    {
                        do { WriteByte(this.servoID, (int)ServoField.ReturnDelayTime, value); }
                        while (ReadByte(this.servoID, (int)ServoField.ReturnDelayTime) != value);
                    }
                }
            }

            /// <summary>
            /// The maximum temperature the Dynamixel is allowed to reach before an automatic shutdown occurs. Value can technically be overwritten, but making read-only for safety reasons.
            /// </summary>
            public int MaxTemperature
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.MaxTemperature);
                }
            }

            /// <summary>
            /// The minimum operating voltage. If the voltage drops below this value, an alarm is triggered.
            /// </summary>
            public int MinVoltage
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.MinVoltage);
                }
                set
                {
                    if (value >= 50)
                    {
                        do { WriteByte(this.servoID, (int)ServoField.MinVoltage, value); }
                        while (ReadByte(this.servoID, (int)ServoField.MinVoltage) != value);
                    }
                }
            }

            /// <summary>
            /// The maximum allowable operating voltage. If the voltage is above this value, an alarm is triggered.
            /// </summary>
            public int MaxVoltage
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.MaxVoltage);
                }
                set
                {
                    if (value <= 140)
                    {
                        do { WriteByte(this.servoID, (int)ServoField.MaxVoltage, value); }
                        while (ReadByte(this.servoID, (int)ServoField.MaxVoltage) != value);
                    }
                }
            }

            /// <summary>
            /// The maximum torque that can be applied by the motor. Values range from 0 to 1023 (0% to 100%)
            /// </summary>
            public int MaxTorque
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.MaxTorque);
                }
                set
                {
                    if ((value >= 0) && (value <= 1023))
                    {
                        do { WriteWord(this.servoID, (int)ServoField.MaxTorque, value); }
                        while (ReadWord(this.servoID, (int)ServoField.MaxTorque) != value);
                    }
                }
            }

            /// <summary>
            /// Controls how the Dynamixel returns status packets. 0: Return only for ping, 1: Return only for reads, 2: Return for all commands
            /// </summary>
            public int StatusReturnLevel
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.StatusReturnLevel);
                }
                set
                {
                    if ((value >= 0) && (value <= 2))
                    {
                        do { WriteByte(this.servoID, (int)ServoField.StatusReturnLevel, value); }
                        while (ReadByte(this.servoID, (int)ServoField.StatusReturnLevel) != value);
                    }
                }
            }

            /// <summary>
            /// Controls which errors will cause the LED to blink.
            /// Bit 0: Input Voltage Error
            /// Bit 1: Angle Limit Error
            /// Bit 2: Overheating Error
            /// Bit 3: Range Error
            /// Bit 4: Checksum Error
            /// Bit 5: Overload Error
            /// Bit 6: Instruction Error
            /// Bit 7: Blank
            /// </summary>
            public int AlarmLED
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.AlarmLED);
                }
                set
                {
                    if ((value >= 0) && (value <= 64))
                    {
                        do { WriteByte(this.servoID, (int)ServoField.AlarmLED, value); }
                        while (ReadByte(this.servoID, (int)ServoField.AlarmLED) != value);
                    }
                }
            }

            /// <summary>
            /// Controls which errors will cause the Dynamixel to shutdown. Same bit layout as AlarmLED
            /// </summary>
            public int AlarmShutdown
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.AlarmShutdown);
                }
                set
                {
                    if ((value >= 0) && (value <= 64))
                    {
                        do { WriteByte(this.servoID, (int)ServoField.AlarmShutdown, value); }
                        while (ReadByte(this.servoID, (int)ServoField.AlarmShutdown) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the motors ability to apply torque. 0: Disabled, 1: Enabled
            /// </summary>
            public int TorqueEnable
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.TorqueEnable);
                }
                set
                {
                    if ((value == 0) || (value == 1))
                    {
                        do { WriteByte(this.servoID, (int)ServoField.TorqueEnable, value); }
                        while (ReadByte(this.servoID, (int)ServoField.TorqueEnable) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the status LED (1: LED on, 0: LED off)
            /// </summary>
            public int LED
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.LED);
                }
                set
                {
                    if ((value >= 0) && (value <= 4))
                    {
                        do { WriteByte(this.servoID, (int)ServoField.LED, value); }
                        while (ReadByte(this.servoID, (int)ServoField.LED) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the speed at which the Dynamixel will move to the goal position. 
            /// Values - 0: Full speed, 1: Min speed, 1023: Max speed
            /// </summary>
            public int MovingSpeed
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.MovingSpeed);
                }
                set
                {
                    if ((value >= 0) && (value <= 1023))
                    {
                        do { WriteWord(this.servoID, (int)ServoField.MovingSpeed, value); }
                        while (ReadWord(this.servoID, (int)ServoField.MovingSpeed) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the amount of torque that can be used based on the MaxTorque property.
            /// Values range from 0 to 1023 (0% to 100%).
            /// </summary>
            public int TorqueLimit
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.TorqueLimit);
                }
                set
                {
                    if ((value >= 0) && (value <= 1023))
                    {
                        do { WriteWord(this.servoID, (int)ServoField.TorqueLimit, value); }
                        while (ReadWord(this.servoID, (int)ServoField.TorqueLimit) != value);

                    }
                }
            }

            /// <summary>
            /// The present motor position of the Dynamixel (read-only).
            /// The value is retrieved from the Dynamixel each time it is referenced.
            /// </summary>
            public int PresentPosition
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.PresentPosition);
                }
            }

            /// <summary>
            /// The present speed the motor is moving at, which may be different than the desired speed (read-only).
            /// Speeds of 0-1023 represent the range of no movement to full speed counter-clockwise.
            /// Speeds of 1024-2047 represent the range of no movement to full speed clockwise.
            /// The value is retrieved from the Dynamixel each time it is referenced.
            /// </summary>
            public int PresentSpeed
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.PresentSpeed);
                }
            }

            /// <summary>
            /// The present load applied being applied to the motor. Infered from internal torque value, so can't be used for accurately measuring weight or applied torque (read-only).
            /// Can be used to determine the direction in which the force is working.
            /// Values range from 0-1023 for a load working the CCW direction and 1024-2047 for loads working in the CW direction. 
            /// The value is retrieved from the Dynamixel each time it is referenced.
            /// </summary>
            public int PresentLoad
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.PresentLoad);
                }
            }

            /// <summary>
            /// The voltage level of the servo. Value is 10x the actual voltage (read-only).
            /// The value is retrieved from the Dynamixel each time it is referenced.
            /// </summary>
            public int PresentVoltage
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.PresentVoltage);
                }
            }

            /// <summary>
            /// The present internal temperature of the Dynamixel. Value is in Centigrade.
            /// The value is retrieved from the Dynamixel each time it is referenced.
            /// </summary>
            public int PresentTemperature
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.PresentTemperature);
                }
            }

            /// <summary>
            /// Indicates if there are instructions waiting to be written synchronously (read-only).
            /// Values: 1 = instructions registered and waiting, 0 = no instructions are registered.
            /// The value is retrieved from the Dynamixel each time it is referenced.
            /// </summary>
            public int Registered
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.Registered);
                }
            }

            /// <summary>
            /// Indicates if the Dynamixel is currently in the process of performing a goal position command, i.e. if it is still trying to move (read-only).
            /// Values: 0 indicates all goal position commands are complete, 1 indicates a goal position command is currently being performed.
            /// The value is retrieved from the Dynamixel each time it is referenced.
            /// </summary>
            public int Moving
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.Moving);
                }
            }

            /// <summary>
            /// Locks the EEPROM, which stops any further writes to the EEPROM area. Must power-cycle the servo to unlock the EEPROM again.
            /// Values: 0 = unlocked, 1 = locked.
            /// </summary>
            public int LockEEPROM
            {
                get
                {
                    return ReadByte(this.servoID, (int)ServoField.LockEEPROM);
                }
                set
                {
                    if ((value == 0) || (value == 1))
                    {
                        WriteByte(this.servoID, (int)ServoField.LockEEPROM, value);
                    }
                }
            }

            /// <summary>
            /// Controls current used to drive motor.
            /// Values: 0 = minimum current required, 1023 = maximum current possible.
            /// </summary>
            public int Punch
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.Punch);
                }
                set
                {
                    if ((value >= 0) && (value <= 1023))
                    {
                        do { WriteWord(this.servoID, (int)ServoField.Punch, value); }
                        while (ReadWord(this.servoID, (int)ServoField.Punch) != value);
                    }
                }
            }

            public int StepSize
            {
                get
                {
                    return this.stepSize;
                }
                set
                {
                    this.stepSize = value;
                }
            }

            public int DefaultStepSize
            {
                get
                {
                    return this.defaultStepSize;
                }
                set
                {
                    this.defaultStepSize = value;
                }
            }

            #endregion

            #region DynamixelServo Functions
            public bool IsMoving()
            {
                return (Moving == 1);
            }

            #endregion
        }

        // Extends the base DynamixelServo class to include features specific to the MX-series
        public class MXServo : DynamixelServo
        {
            #region MXServo Properties
            // Control table entries exclusive to the MX-series dynamixels
            public enum MXServoField
            {
                DGain = 26,
                IGain = 27,
                PGain = 28,
                GoalPosition = 30,
                Current = 68
            }
            #endregion

            #region MXServo Constructors
            /// <summary>
            /// Constructor that takes in servo's ID and configures initial servo properties. All Dynamixels are configured from the factory with an ID of 1.
            /// This implementation assumes all Dynamixels have already been reassigned an appropriate ID using the Dynamixel Wizard. 
            /// </summary>
            /// <param name="ID">ID of the Dynamixel.</param>
            public MXServo(int ID)
            {
                // Have to establish this value first so we know which Dynamixel to talk to.
                this.servoID = ID;

                // Only set default values here that aren't likely to change. Others are set in other initialization functions specific to each servo.
                BaudRate = 1;
                ReturnDelay = 0;
                MinVoltage = 50;
                MaxVoltage = 140;
                MaxTorque = 1023;
                StatusReturnLevel = 2;
                AlarmLED = 36;
                AlarmShutdown = 36;
                TorqueLimit = 1023;
                Punch = 0;
                StepSize = 0;
                DefaultStepSize = 0;
            }

            #endregion

            #region MXServo Accessors
            /// <summary>
            /// Constrains the movement of the servo in the clockwise direction by imposing this software limit.
            /// </summary>
            public int CWAngleLimit
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.CWAngleLimit);
                }
                set
                {
                    if ((value >= 0) && (value <= 4095))
                    {
                        do { WriteWord(this.servoID, (int)ServoField.CWAngleLimit, value); }
                        while (ReadWord(this.servoID, (int)ServoField.CWAngleLimit) != value);
                    }
                }
            }

            /// <summary>
            /// Constrains the movement of the servo in the counter-clockwise direction by imposing this software limit.
            /// </summary>
            public int CCWAngleLimit
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.CCWAngleLimit);
                }
                set
                {
                    if ((value >= 0) && (value <= 4095))
                    {
                        do { WriteWord(this.servoID, (int)ServoField.CCWAngleLimit, value); }
                        while (ReadWord(this.servoID, (int)ServoField.CCWAngleLimit) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the Derivative Gain used in the PID control loop.
            /// </summary>
            public int DGain
            {
                get
                {
                    return ReadByte(this.servoID, (int)MXServoField.DGain);
                }
                set
                {
                    if ((value >= 0) && (value <= 254))
                    {
                        do { WriteByte(this.servoID, (int)MXServoField.DGain, value); }
                        while (ReadByte(this.servoID, (int)MXServoField.DGain) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the Integral Gain used in the PID control loop.
            /// </summary>
            public int IGain
            {
                get
                {
                    return ReadByte(this.servoID, (int)MXServoField.IGain);
                }
                set
                {
                    if ((value >= 0) && (value <= 254))
                    {
                        do { WriteByte(this.servoID, (int)MXServoField.IGain, value); }
                        while (ReadByte(this.servoID, (int)MXServoField.IGain) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the Proportional Gain used in the PID control loop.
            /// </summary>
            public int PGain
            {
                get
                {
                    return ReadByte(this.servoID, (int)MXServoField.PGain);
                }
                set
                {
                    if ((value >= 0) && (value <= 254))
                    {
                        do { WriteByte(this.servoID, (int)MXServoField.PGain, value); }
                        while (ReadByte(this.servoID, (int)MXServoField.PGain) != value);
                    }
                }
            }

            /// <summary>
            /// Sets the goal position of the MX-series Dynamixel.
            /// Values range from 0 (full right) to 4095 (full left).
            /// Resolution of 0.088 degrees.
            /// </summary>
            public int GoalPosition
            {
                get
                {
                    return ReadWord(this.servoID, (int)MXServoField.GoalPosition);
                }
                set
                {
                    if ((value >= this.CWAngleLimit) && (value <= CCWAngleLimit))
                    {
                        do { WriteWord(this.servoID, (int)MXServoField.GoalPosition, value); }
                        while (ReadWord(this.servoID, (int)MXServoField.GoalPosition) != value);

                    }
                }
            }

            /// <summary>
            /// The amount of current the servo is currently drawing (read-only).
            /// Values: 2048 = idle, less than 2048 = negative current flow, greater than 2048 = positive current flow.
            /// Calculate using: I = (4.5mA) * (ConsumingCurrent - 2048)
            /// Value is retrieved directly from Dynamixel (not cache) at each reference.
            /// </summary>
            public int ConsumingCurrent
            {
                get
                {
                    return ReadWord(this.servoID, (int)MXServoField.Current);
                }
            }

            #endregion

            #region MXServo Functions
            public void HoldPosition()
            {
                this.GoalPosition = this.PresentPosition;
            }

            public void ReleaseTorque()
            {
                this.TorqueEnable = 0;
            }

            #endregion

        }

        // Extends the base DynamixelServo class to include AX -series-specific functions
        public class AXServo : DynamixelServo
        {
            #region AXServo Properties

            // Control table entries exclusive to the AX-series dynamixels
            public enum AXServoField
            {
                CWComplianceMargin = 26,
                CCWComplianceMargin = 27,
                CWComplianceSlope = 28,
                CCWComplianceSlope = 29,
                GoalPosition = 30
            }

            #endregion

            #region AXServo Constructors
            /// <summary>
            /// Constructor that takes in servo's ID and configures initial servo properties. All Dynamixels are configured from the factory with an ID of 1.
            /// This implementation assumes all Dynamixels have already been reassigned an appropriate ID using the Dynamixel Wizard. 
            /// </summary>
            /// <param name="ID">ID of the Dynamixel.</param>
            public AXServo(int ID)
            {
                this.servoID = ID;

                // Default values
                BaudRate = 1;
                ReturnDelay = 0;
                MinVoltage = 50;
                MaxVoltage = 140;
                MaxTorque = 1023;
                StatusReturnLevel = 2;
                AlarmLED = 36;
                AlarmShutdown = 36;
                TorqueLimit = 1023;
                Punch = 32;
                StepSize = 0;
                DefaultStepSize = 0;

            }
            #endregion

            #region AXServo Accessors
            /// <summary>
            /// Constrains the movement of the servo in the clockwise direction by imposing this software limit.
            /// </summary>
            /// 
            public int CWAngleLimit
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.CWAngleLimit);
                }
                set
                {
                    if ((value >= 0) && (value <= 1023))
                    {
                        do { WriteWord(this.servoID, (int)ServoField.CWAngleLimit, value); }
                        while (ReadWord(this.servoID, (int)ServoField.CWAngleLimit) != value);
                    }
                }
            }

            /// <summary>
            /// Constrains the movement of the servo in the counter-clockwise direction by imposing this software limit.
            /// </summary>
            public int CCWAngleLimit
            {
                get
                {
                    return ReadWord(this.servoID, (int)ServoField.CCWAngleLimit);
                }
                set
                {
                    if ((value >= 0) && (value <= 1023))
                    {
                        do { WriteWord(this.servoID, (int)ServoField.CCWAngleLimit, value); }
                        while (ReadWord(this.servoID, (int)ServoField.CCWAngleLimit) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the allowable error between the goal position and the present position when moving in a clockwise direction. 
            /// Lower values mean more torque near goal position.
            /// </summary>
            public int CWComplianceMargin
            {
                get
                {
                    return ReadByte(this.servoID, (int)AXServoField.CWComplianceMargin);
                }
                set
                {
                    if ((value >= 0) && (value <= 255))
                    {
                        do { WriteByte(this.servoID, (int)AXServoField.CWComplianceMargin, value); }
                        while (ReadByte(this.servoID, (int)AXServoField.CWComplianceMargin) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the allowable error between the goal position and the present position when moving in a counter-clockwise direction
            /// Lower values mean servo control will be tighter but more demanding.
            /// </summary>
            public int CCWComplianceMargin
            {
                get
                {
                    return ReadByte(this.servoID, (int)AXServoField.CCWComplianceMargin);
                }
                set
                {
                    if ((value >= 0) && (value <= 255))
                    {
                        do { WriteByte(this.servoID, (int)AXServoField.CCWComplianceMargin, value); }
                        while (ReadByte(this.servoID, (int)AXServoField.CCWComplianceMargin) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the torque near the goal position when moving in a clockwise direction.
            /// Lower values mean more torque near the goal position, but less flexibility.
            /// </summary>
            public int CWComplianceSlope
            {
                get
                {
                    return ReadByte(this.servoID, (int)AXServoField.CWComplianceSlope);
                }
                set
                {
                    if ((value >= 0) && (value <= 254))
                    {
                        do { WriteByte(this.servoID, (int)AXServoField.CWComplianceSlope, value); }
                        while (ReadByte(this.servoID, (int)AXServoField.CWComplianceSlope) != value);
                    }
                }
            }

            /// <summary>
            /// Controls the torque near the goal position when moving in a counter-clockwise direction.
            /// Lower values mean more torque near the goal position, but less flexibility.
            /// </summary>
            public int CCWComplianceSlope
            {
                get
                {
                    return ReadByte(this.servoID, (int)AXServoField.CCWComplianceSlope);
                }
                set
                {
                    if ((value >= 0) && (value <= 254))
                    {
                        do { WriteByte(this.servoID, (int)AXServoField.CCWComplianceSlope, value); }
                        while (ReadByte(this.servoID, (int)AXServoField.CCWComplianceMargin) != value);
                    }
                }
            }

            /// <summary>
            /// Sets the goal position of the AX-series motor.
            /// Values range from 0 (full right) to 1023 (full left).
            /// Resolution of 0.29 degrees.
            /// </summary>
            public int GoalPosition
            {
                get
                {
                    return ReadWord(this.servoID, (int)AXServoField.GoalPosition);
                }
                set
                {
                    if ((value >= this.CWAngleLimit) && (value <= this.CCWAngleLimit))
                    {
                        do { WriteWord(this.servoID, (int)AXServoField.GoalPosition, value); }
                        while (ReadWord(this.servoID, (int)AXServoField.GoalPosition) != value);
                    }
                }
            }
            #endregion

            #region AXServo Functions
            public void HoldPosition()
            {
                this.GoalPosition = this.PresentPosition;
                this.TorqueEnable = 1;
            }

            public void ReleaseTorque()
            {
                this.TorqueEnable = 0;
            }

            #endregion
        }

        #endregion
        
        #region Arm-Level Dynamixel Reading and Writing
        /// <summary>
        /// Reads a byte-value from a single memory location of a Dynamixel servo.
        /// </summary>
        /// <param name="servoID">The ID of the Dynamixel.</param>
        /// <param name="address">The memory address.</param>
        /// <returns>The value stored at the memory address, returned as an integer.</returns>
        public static int ReadByte(int servoID, int address)
        {
            int commStatus;
            int result;
            int attempts = 5;

            // Perform the read, store the result, and get the return status of the operation (success or failure)
            result = Dynamixel.dxl_read_byte(servoID, address);
            commStatus = Dynamixel.dxl_get_result();

            //If the read fails, continue with brute force. Failure is not an option. (This blocks)
            while (commStatus != Dynamixel.COMM_RXSUCCESS)
            {
                if (--attempts == 0)
                {
                    Logger.WriteLine("Stuck reconnecting...");
                    Dynamixel.dxl_terminate();
                    Dynamixel.dxl_initialize(comPort, 1);
                    attempts = 5;
                }

                // Reattempt the read operation.
                result = Dynamixel.dxl_read_byte(servoID, address);
                commStatus = Dynamixel.dxl_get_result();
            }

            return result;
        }
        
        /// <summary>
        /// Reads a word-value from two contiguous memory locations of a Dynamixel servo.
        /// </summary>
        /// <param name="servoID">The ID of the Dynamixel.</param>
        /// <param name="address">The first memory address.</param>
        /// <returns>The valued stored at the two memory addresses, returned as an integer.</returns>
        public static int ReadWord(int servoID, int address)
        {
            int commStatus;
            int result;
            int attempts = 5;

            // Perform the read, store the result, and get the return status of the operation (success or failure)
            result = Dynamixel.dxl_read_word(servoID, address); 
            commStatus = Dynamixel.dxl_get_result();

            //If the read failed, continue with brute force. Failure is not an option. (This blocks)
            while (commStatus != Dynamixel.COMM_RXSUCCESS)
            {
                if (--attempts == 0)
                {
                    Logger.WriteLine("Stuck reconnecting...");
                    Dynamixel.dxl_terminate();
                    Dynamixel.dxl_initialize(comPort, 1);
                    attempts = 5;
                }

                // Reattempt the read operation.
                result = Dynamixel.dxl_read_word(servoID, address); 
                commStatus = Dynamixel.dxl_get_result();
            }

            return result;
        }

        /// <summary>
        /// Writes a byte-value to a single memory location of a Dynamixel servo.
        /// </summary>
        /// <param name="servoID">The ID of the Dynamixel.</param>
        /// <param name="address">The memory location.</param>
        /// <param name="value">The value being written to memory.</param>
        public static void WriteByte(int servoID, int address, int value)
        {
            int commStatus;
            int attempts = 5;

            // Perform the write and then get the returned status of the write operation (success or failure)
            Dynamixel.dxl_write_byte(servoID, address, value);
            commStatus = Dynamixel.dxl_get_result(); 

            //If the write failed, continue with brute force. Failure is not an option. (This blocks)
            while(commStatus != Dynamixel.COMM_RXSUCCESS)
            {
                if (--attempts == 0)
                {
                    Logger.WriteLine("Stuck reconnecting...");
                    Dynamixel.dxl_terminate();
                    Dynamixel.dxl_initialize(comPort, 1);
                    attempts = 5;
                }

                // Reattempt the write operation
                Dynamixel.dxl_write_byte(servoID, address, value);
                commStatus = Dynamixel.dxl_get_result();
            }
        }

        /// <summary>
        /// Writes a word-value to two contiguous memory locations of a Dynamixel servo.
        /// </summary>
        /// <param name="servoID">The ID of the Dynamixel.</param>
        /// <param name="address">The memory address.</param>
        /// <param name="value">The value being written to memory.</param>
        public static void WriteWord(int servoID, int address, int value)
        {
            int commStatus;
            int attempts = 5;

            // Perform the write and then get the returned status of the write operation (success or failure)
            Dynamixel.dxl_write_word(servoID, address, value);
            commStatus = Dynamixel.dxl_get_result();

            //If the write failed, continue with brute force. Failure is not an option. (This blocks)
            while (commStatus != Dynamixel.COMM_RXSUCCESS)
            {
                if (--attempts == 0)
                {
                    Logger.WriteLine("Stuck reconnecting...");
                    Dynamixel.dxl_terminate();
                    Dynamixel.dxl_initialize(comPort, 1);
                    attempts = 5;
                }

                // Reattempt the write operation.
                Dynamixel.dxl_write_word(servoID, address, value);
                commStatus = Dynamixel.dxl_get_result();
            }
        }

        #endregion
 
        #region USB Functions
        /// <summary>
        /// Attempts to connect to a USB2Dynamixel device.
        /// </summary>
        /// <param name="comPort">COM port of the device.</param>
        /// <param name="baudRate">Baud rate for communicating with the device.</param>
        /// <returns></returns>
        public bool ConnectArm(int comPort, int baudRate = 1)
        {
            while (Dynamixel.dxl_initialize(comPort, baudRate) != 1)
            {
                Logger.WriteLine("Stuck on initial connect.");
            }
            this.isActive = true;

            return this.isActive;
        }

        /// <summary>
        /// Disconnects the USB2Dynamixel device.
        /// </summary>
        /// <param name="releaseTorque">Releases the torque on all motors if true.</param>
        public void DisconnectArm(bool disableTorque = true)
        {
            // todo: Implement function to release torque on all motors.
            if (disableTorque)
            {
                GoLimp();
            }

            // Disconnects the USB2Dynamixel device.
            Dynamixel.dxl_terminate();
            this.isActive = false;
        }

        #endregion
        
        #region Servo Utilities

        public void DumpStats(bool pause = false)
        {
            Console.WriteLine("Let's see how the servos look!");

            // Shoulder rotating servo stats
            Console.WriteLine();
            Console.WriteLine("Servo Configuration Stats: ");
            Console.WriteLine("ID: " + baseServo.ServoID.ToString());
            Console.WriteLine("Model: " + baseServo.ModelNumber.ToString());
            Console.WriteLine("Firmware:" + baseServo.Firmware.ToString());
            Console.WriteLine("Baud Rate:" + baseServo.BaudRate.ToString());
            Console.WriteLine("Return Delay:" + baseServo.ReturnDelay.ToString());
            Console.WriteLine("CW Angle Limit:" + baseServo.CWAngleLimit.ToString());
            Console.WriteLine("CCW Angle Limit:" + baseServo.CCWAngleLimit.ToString());
            Console.WriteLine("Max Temperature: " + baseServo.MaxTemperature.ToString());
            Console.WriteLine("Min Voltage: " + baseServo.MinVoltage.ToString());
            Console.WriteLine("Max Voltage: " + baseServo.MaxVoltage.ToString());
            Console.WriteLine("Max Torque: " + baseServo.MaxTorque.ToString());
            Console.WriteLine("Status Return Level: " + baseServo.StatusReturnLevel.ToString());
            Console.WriteLine("Alarm LED: " + baseServo.AlarmLED.ToString());
            Console.WriteLine("Alarm Shutdown: " + baseServo.AlarmShutdown.ToString());
            Console.WriteLine("Torque Enable: " + baseServo.TorqueEnable.ToString());
            Console.WriteLine("LED value: " + baseServo.LED.ToString());
            Console.WriteLine("Derivative Gain: " + baseServo.DGain.ToString());
            Console.WriteLine("Integral Gain: " + baseServo.IGain.ToString());
            Console.WriteLine("Proportional Gain: " + baseServo.PGain.ToString());
            Console.WriteLine("Moving Speed: " + baseServo.MovingSpeed.ToString());
            Console.WriteLine("Torque Limit: " + baseServo.TorqueLimit.ToString());
            Console.WriteLine("Goal Position: " + baseServo.GoalPosition.ToString());
            Console.WriteLine("Present Position: " + baseServo.PresentPosition.ToString());
            Console.WriteLine("Present Speed: " + baseServo.PresentSpeed.ToString());
            Console.WriteLine("Present Load: " + baseServo.PresentLoad.ToString());
            Console.WriteLine("Present Voltage: " + baseServo.PresentVoltage.ToString());
            Console.WriteLine("Present Temperature: " + baseServo.PresentTemperature.ToString());
            Console.WriteLine("Instruction Registered: " + baseServo.Registered.ToString());
            Console.WriteLine("Moving Status: " + baseServo.Moving.ToString());
            Console.WriteLine("EEPROM Status: " + baseServo.LockEEPROM.ToString());
            Console.WriteLine("Punch: " + baseServo.Punch.ToString());
            Console.WriteLine("Current Draw: " + baseServo.ConsumingCurrent.ToString());
            Console.WriteLine();

            // Shoulder lifting servo stats
            Console.WriteLine();
            Console.WriteLine("Servo Configuration Stats: ");
            Console.WriteLine("ID: " + shoulderServo.ServoID.ToString());
            Console.WriteLine("Model: " + shoulderServo.ModelNumber.ToString());
            Console.WriteLine("Firmware:" + shoulderServo.Firmware.ToString());
            Console.WriteLine("Baud Rate:" + shoulderServo.BaudRate.ToString());
            Console.WriteLine("Return Delay:" + shoulderServo.ReturnDelay.ToString());
            Console.WriteLine("CW Angle Limit:" + shoulderServo.CWAngleLimit.ToString());
            Console.WriteLine("CCW Angle Limit:" + shoulderServo.CCWAngleLimit.ToString());
            Console.WriteLine("Max Temperature: " + shoulderServo.MaxTemperature.ToString());
            Console.WriteLine("Min Voltage: " + shoulderServo.MinVoltage.ToString());
            Console.WriteLine("Max Voltage: " + shoulderServo.MaxVoltage.ToString());
            Console.WriteLine("Max Torque: " + shoulderServo.MaxTorque.ToString());
            Console.WriteLine("Status Return Level: " + shoulderServo.StatusReturnLevel.ToString());
            Console.WriteLine("Alarm LED: " + shoulderServo.AlarmLED.ToString());
            Console.WriteLine("Alarm Shutdown: " + shoulderServo.AlarmShutdown.ToString());
            Console.WriteLine("Torque Enable: " + shoulderServo.TorqueEnable.ToString());
            Console.WriteLine("LED value: " + shoulderServo.LED.ToString());
            Console.WriteLine("Derivative Gain: " + shoulderServo.DGain.ToString());
            Console.WriteLine("Integral Gain: " + shoulderServo.IGain.ToString());
            Console.WriteLine("Proportional Gain: " + shoulderServo.PGain.ToString());
            Console.WriteLine("Moving Speed: " + shoulderServo.MovingSpeed.ToString());
            Console.WriteLine("Torque Limit: " + shoulderServo.TorqueLimit.ToString());
            Console.WriteLine("Goal Position: " + shoulderServo.GoalPosition.ToString());
            Console.WriteLine("Present Position: " + shoulderServo.PresentPosition.ToString());
            Console.WriteLine("Present Speed: " + shoulderServo.PresentSpeed.ToString());
            Console.WriteLine("Present Load: " + shoulderServo.PresentLoad.ToString());
            Console.WriteLine("Present Voltage: " + shoulderServo.PresentVoltage.ToString());
            Console.WriteLine("Present Temperature: " + shoulderServo.PresentTemperature.ToString());
            Console.WriteLine("Instruction Registered: " + shoulderServo.Registered.ToString());
            Console.WriteLine("Moving Status: " + shoulderServo.Moving.ToString());
            Console.WriteLine("EEPROM Status: " + shoulderServo.LockEEPROM.ToString());
            Console.WriteLine("Punch: " + shoulderServo.Punch.ToString());
            Console.WriteLine("Current Draw: " + shoulderServo.ConsumingCurrent.ToString());
            Console.WriteLine();

            // Elbow servo stats
            Console.WriteLine();
            Console.WriteLine("Servo Configuration Stats: ");
            Console.WriteLine("ID: " + elbowServo.ServoID.ToString());
            Console.WriteLine("Model: " + elbowServo.ModelNumber.ToString());
            Console.WriteLine("Firmware:" + elbowServo.Firmware.ToString());
            Console.WriteLine("Baud Rate:" + elbowServo.BaudRate.ToString());
            Console.WriteLine("Return Delay:" + elbowServo.ReturnDelay.ToString());
            Console.WriteLine("CW Angle Limit:" + elbowServo.CWAngleLimit.ToString());
            Console.WriteLine("CCW Angle Limit:" + elbowServo.CCWAngleLimit.ToString());
            Console.WriteLine("Max Temperature: " + elbowServo.MaxTemperature.ToString());
            Console.WriteLine("Min Voltage: " + elbowServo.MinVoltage.ToString());
            Console.WriteLine("Max Voltage: " + elbowServo.MaxVoltage.ToString());
            Console.WriteLine("Max Torque: " + elbowServo.MaxTorque.ToString());
            Console.WriteLine("Status Return Level: " + elbowServo.StatusReturnLevel.ToString());
            Console.WriteLine("Alarm LED: " + elbowServo.AlarmLED.ToString());
            Console.WriteLine("Alarm Shutdown: " + elbowServo.AlarmShutdown.ToString());
            Console.WriteLine("Torque Enable: " + elbowServo.TorqueEnable.ToString());
            Console.WriteLine("LED value: " + elbowServo.LED.ToString());
            Console.WriteLine("Derivative Gain: " + elbowServo.DGain.ToString());
            Console.WriteLine("Integral Gain: " + elbowServo.IGain.ToString());
            Console.WriteLine("Proportional Gain: " + elbowServo.PGain.ToString());
            Console.WriteLine("Moving Speed: " + elbowServo.MovingSpeed.ToString());
            Console.WriteLine("Torque Limit: " + elbowServo.TorqueLimit.ToString());
            Console.WriteLine("Goal Position: " + elbowServo.GoalPosition.ToString());
            Console.WriteLine("Present Position: " + elbowServo.PresentPosition.ToString());
            Console.WriteLine("Present Speed: " + elbowServo.PresentSpeed.ToString());
            Console.WriteLine("Present Load: " + elbowServo.PresentLoad.ToString());
            Console.WriteLine("Present Voltage: " + elbowServo.PresentVoltage.ToString());
            Console.WriteLine("Present Temperature: " + elbowServo.PresentTemperature.ToString());
            Console.WriteLine("Instruction Registered: " + elbowServo.Registered.ToString());
            Console.WriteLine("Moving Status: " + elbowServo.Moving.ToString());
            Console.WriteLine("EEPROM Status: " + elbowServo.LockEEPROM.ToString());
            Console.WriteLine("Punch: " + elbowServo.Punch.ToString());
            Console.WriteLine("Current Draw: " + elbowServo.ConsumingCurrent.ToString());
            Console.WriteLine();

            // Wrist servo stats
            Console.WriteLine();
            Console.WriteLine("Servo Configuration Stats: ");
            Console.WriteLine("ID: " + wristServo.ServoID.ToString());
            Console.WriteLine("Model: " + wristServo.ModelNumber.ToString());
            Console.WriteLine("Firmware:" + wristServo.Firmware.ToString());
            Console.WriteLine("Baud Rate:" + wristServo.BaudRate.ToString());
            Console.WriteLine("Return Delay:" + wristServo.ReturnDelay.ToString());
            Console.WriteLine("CW Angle Limit:" + wristServo.CWAngleLimit.ToString());
            Console.WriteLine("CCW Angle Limit:" + wristServo.CCWAngleLimit.ToString());
            Console.WriteLine("Max Temperature: " + wristServo.MaxTemperature.ToString());
            Console.WriteLine("Min Voltage: " + wristServo.MinVoltage.ToString());
            Console.WriteLine("Max Voltage: " + wristServo.MaxVoltage.ToString());
            Console.WriteLine("Max Torque: " + wristServo.MaxTorque.ToString());
            Console.WriteLine("Status Return Level: " + wristServo.StatusReturnLevel.ToString());
            Console.WriteLine("Alarm LED: " + wristServo.AlarmLED.ToString());
            Console.WriteLine("Alarm Shutdown: " + wristServo.AlarmShutdown.ToString());
            Console.WriteLine("Torque Enable: " + wristServo.TorqueEnable.ToString());
            Console.WriteLine("LED value: " + wristServo.LED.ToString());
            Console.WriteLine("CW Compliance Margin: " + wristServo.CWComplianceMargin.ToString());
            Console.WriteLine("CCW Compliance Margin: " + wristServo.CCWComplianceMargin.ToString());
            Console.WriteLine("CW Compliance Slope: " + wristServo.CWComplianceSlope.ToString());
            Console.WriteLine("CCW Compliance Slope: " + wristServo.CCWComplianceSlope.ToString());
            Console.WriteLine("Moving Speed: " + wristServo.MovingSpeed.ToString());
            Console.WriteLine("Torque Limit: " + wristServo.TorqueLimit.ToString());
            Console.WriteLine("Goal Position: " + wristServo.GoalPosition.ToString());
            Console.WriteLine("Present Position: " + wristServo.PresentPosition.ToString());
            Console.WriteLine("Present Speed: " + wristServo.PresentSpeed.ToString());
            Console.WriteLine("Present Load: " + wristServo.PresentLoad.ToString());
            Console.WriteLine("Present Voltage: " + wristServo.PresentVoltage.ToString());
            Console.WriteLine("Present Temperature: " + wristServo.PresentTemperature.ToString());
            Console.WriteLine("Instruction Registered: " + wristServo.Registered.ToString());
            Console.WriteLine("Moving Status: " + wristServo.Moving.ToString());
            Console.WriteLine("EEPROM Status: " + wristServo.LockEEPROM.ToString());
            Console.WriteLine("Punch: " + wristServo.Punch.ToString());
            Console.WriteLine();

            // Gripper servo stats.
            Console.WriteLine();
            Console.WriteLine("Servo Configuration Stats: ");
            Console.WriteLine("ID: " + clawServo.ServoID.ToString());
            Console.WriteLine("Model: " + clawServo.ModelNumber.ToString());
            Console.WriteLine("Firmware:" + clawServo.Firmware.ToString());
            Console.WriteLine("Baud Rate:" + clawServo.BaudRate.ToString());
            Console.WriteLine("Return Delay:" + clawServo.ReturnDelay.ToString());
            Console.WriteLine("CW Angle Limit:" + clawServo.CWAngleLimit.ToString());
            Console.WriteLine("CCW Angle Limit:" + clawServo.CCWAngleLimit.ToString());
            Console.WriteLine("Max Temperature: " + clawServo.MaxTemperature.ToString());
            Console.WriteLine("Min Voltage: " + clawServo.MinVoltage.ToString());
            Console.WriteLine("Max Voltage: " + clawServo.MaxVoltage.ToString());
            Console.WriteLine("Max Torque: " + clawServo.MaxTorque.ToString());
            Console.WriteLine("Status Return Level: " + clawServo.StatusReturnLevel.ToString());
            Console.WriteLine("Alarm LED: " + clawServo.AlarmLED.ToString());
            Console.WriteLine("Alarm Shutdown: " + clawServo.AlarmShutdown.ToString());
            Console.WriteLine("Torque Enable: " + clawServo.TorqueEnable.ToString());
            Console.WriteLine("LED value: " + clawServo.LED.ToString());
            Console.WriteLine("CW Compliance Margin: " + clawServo.CWComplianceMargin.ToString());
            Console.WriteLine("CCW Compliance Margin: " + clawServo.CCWComplianceMargin.ToString());
            Console.WriteLine("CW Compliance Slope: " + clawServo.CWComplianceSlope.ToString());
            Console.WriteLine("CCW Compliance Slope: " + clawServo.CCWComplianceSlope.ToString());
            Console.WriteLine("Moving Speed: " + clawServo.MovingSpeed.ToString());
            Console.WriteLine("Torque Limit: " + clawServo.TorqueLimit.ToString());
            Console.WriteLine("Goal Position: " + clawServo.GoalPosition.ToString());
            Console.WriteLine("Present Position: " + clawServo.PresentPosition.ToString());
            Console.WriteLine("Present Speed: " + clawServo.PresentSpeed.ToString());
            Console.WriteLine("Present Load: " + clawServo.PresentLoad.ToString());
            Console.WriteLine("Present Voltage: " + clawServo.PresentVoltage.ToString());
            Console.WriteLine("Present Temperature: " + clawServo.PresentTemperature.ToString());
            Console.WriteLine("Instruction Registered: " + clawServo.Registered.ToString());
            Console.WriteLine("Moving Status: " + clawServo.Moving.ToString());
            Console.WriteLine("EEPROM Status: " + clawServo.LockEEPROM.ToString());
            Console.WriteLine("Punch: " + clawServo.Punch.ToString());
            Console.WriteLine();

            if (pause)
            {
                //Console.WriteLine("Press any key to continue...");
                //Console.ReadKey(true);
            }
        }

        public void DumpVitals(bool pause = false)
        {
            ShoulderRotateVitals();
            ShoulderLiftVitals();
            ElbowVitals();
            WristVitals();
            ClawVitals();

            if (pause)
            {
                //Console.WriteLine("Press any key to continue...");
                //Console.ReadKey(true);
            }
        }

        public void ShoulderRotateVitals(bool pause = false)
        {
            Logger.WriteLine("Shoulder Rotate: ");
            Logger.WriteLine("Torque Enable: " + baseServo.TorqueEnable.ToString());
            Logger.WriteLine("Present Position: " + baseServo.PresentPosition.ToString());
            Logger.WriteLine("Goal Position: " + baseServo.GoalPosition.ToString());
            Logger.WriteLine("Moving Status: " + baseServo.Moving.ToString());
            Logger.WriteLine("Present Speed: " + baseServo.PresentSpeed.ToString());
            Logger.WriteLine("Assigned Speed: " + baseServo.MovingSpeed.ToString());
            Logger.WriteLine("Present Voltage: " + baseServo.PresentVoltage.ToString());
            Logger.WriteLine("Present Temperature: " + baseServo.PresentTemperature.ToString());
            Logger.WriteLine("Current Draw: " + baseServo.ConsumingCurrent.ToString() + "\n");
            if (pause)
            {
                //Logger.WriteLine("Press any key to continue...");
               // Logger.ReadKey(true);
            }
        }

        public void ShoulderLiftVitals(bool pause = false)
        {
            Logger.WriteLine("Shoulder Lift:");
            Logger.WriteLine("Torque Enable: " + shoulderServo.TorqueEnable.ToString());
            Logger.WriteLine("Present Position: " + shoulderServo.PresentPosition.ToString());
            Logger.WriteLine("Goal Position: " + shoulderServo.GoalPosition.ToString());
            Logger.WriteLine("Moving Status: " + shoulderServo.Moving.ToString());
            Logger.WriteLine("Present Speed: " + shoulderServo.PresentSpeed.ToString());
            Logger.WriteLine("Assigned Speed: " + shoulderServo.MovingSpeed.ToString());
            Logger.WriteLine("Present Voltage: " + shoulderServo.PresentVoltage.ToString());
            Logger.WriteLine("Present Temperature: " + shoulderServo.PresentTemperature.ToString());
            Logger.WriteLine("Current Draw: " + shoulderServo.ConsumingCurrent.ToString() + "\n");

            if (pause)
            {
                //Logger.WriteLine("Press any key to continue...");
                //Logger.ReadKey(true);
            }
        }

        public void ElbowVitals(bool pause = false)
        {
            Logger.WriteLine("Elbow: ");
            Logger.WriteLine("Torque Enable: " + elbowServo.TorqueEnable.ToString());
            Logger.WriteLine("Present Position: " + elbowServo.PresentPosition.ToString());
            Logger.WriteLine("Goal Position: " + elbowServo.GoalPosition.ToString());
            Logger.WriteLine("Moving Status: " + elbowServo.Moving.ToString());
            Logger.WriteLine("Present Speed: " + elbowServo.PresentSpeed.ToString());
            Logger.WriteLine("Assigned Speed: " + elbowServo.MovingSpeed.ToString());
            Logger.WriteLine("Present Voltage: " + elbowServo.PresentVoltage.ToString());
            Logger.WriteLine("Present Temperature: " + elbowServo.PresentTemperature.ToString());
            Logger.WriteLine("Current Draw: " + elbowServo.ConsumingCurrent.ToString() + "\n");

            if (pause)
            {
                //Logger.WriteLine("Press any key to continue...");
               // Logger.ReadKey(true);
            }
        }

        public void WristVitals(bool pause = false)
        {
            Logger.WriteLine("Wrist: ");
            Logger.WriteLine("Torque Enable: " + wristServo.TorqueEnable.ToString());
            Logger.WriteLine("Present Position: " + wristServo.PresentPosition.ToString());
            Logger.WriteLine("Goal Position: " + wristServo.GoalPosition.ToString());
            Logger.WriteLine("Moving Status: " + wristServo.Moving.ToString());
            Logger.WriteLine("Present Speed: " + wristServo.PresentSpeed.ToString());
            Logger.WriteLine("Assigned Speed: " + wristServo.MovingSpeed.ToString());
            Logger.WriteLine("Present Voltage: " + wristServo.PresentVoltage.ToString());
            Logger.WriteLine("Present Temperature: " + wristServo.PresentTemperature.ToString());
            Logger.WriteLine("CW Compliance Margin: " + wristServo.CWComplianceMargin.ToString());
            Logger.WriteLine("CCW Compliance Margin: " + wristServo.CCWComplianceMargin.ToString());
            Logger.WriteLine("CW Compliance Slope: " + wristServo.CWComplianceSlope.ToString());
            Logger.WriteLine("CCW Compliance Slope: " + wristServo.CCWComplianceSlope.ToString() + "\n");

            if (pause)
            {
                //Logger.WriteLine("Press any key to continue...");
                //Logger.ReadKey(true);
            }
        }

        public void ClawVitals(bool pause = false)
        {
            Logger.WriteLine("Gripper: ");
            Logger.WriteLine("Torque Enable: " + clawServo.TorqueEnable.ToString());
            Logger.WriteLine("Present Position: " + clawServo.PresentPosition.ToString());
            Logger.WriteLine("Goal Position: " + clawServo.GoalPosition.ToString());
            Logger.WriteLine("Moving Status: " + clawServo.Moving.ToString());
            Logger.WriteLine("Present Speed: " + clawServo.PresentSpeed.ToString());
            Logger.WriteLine("Assigned Speed: " + clawServo.MovingSpeed.ToString());
            Logger.WriteLine("Present Voltage: " + clawServo.PresentVoltage.ToString());
            Logger.WriteLine("Present Temperature: " + clawServo.PresentTemperature.ToString());
            Logger.WriteLine("CW Compliance Margin: " + clawServo.CWComplianceMargin.ToString());
            Logger.WriteLine("CCW Compliance Margin: " + clawServo.CCWComplianceMargin.ToString());
            Logger.WriteLine("CW Compliance Slope: " + clawServo.CWComplianceSlope.ToString());
            Logger.WriteLine("CCW Compliance Slope: " + clawServo.CCWComplianceSlope.ToString() + "\n");

            if (pause)
            {
                //Logger.WriteLine("Press any key to continue...");
                //Logger.ReadKey(true);
            }
        }

        #endregion
        
        #region Test Functions
        public void Test()
        {   
            
        }

        #endregion
        
    }
}
