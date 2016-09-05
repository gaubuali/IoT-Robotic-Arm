using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Devices.Gpio;
using Windows.Foundation;
using Windows.System.Threading;

namespace SimpleWeb
{
    public class Stepper
    {

        public Stepper()
        {
            stopwatch = Stopwatch.StartNew();
        }
        public enum Direction
        {
            None,
            Left, 
            Right,
            Max
        }
        public class RunningOption
        {
            public Direction dir = Direction.Left;
            public UInt32 Step = 0;
            public uint StepDelay = 10;

            public bool RunInfitive = false;
        }
        Stopwatch stopwatch;
        // You can change those pin to your configuration
        private const int LED_PIN1 = 18;
        private const int LED_PIN2 = 23;
        private const int LED_PIN3 = 24;
        private const int LED_PIN4 = 25;

        private const int LED_PINB1 = 21;
        private const int LED_PINB2 = 20;
        private const int LED_PINB3 = 16;
        private const int LED_PINB4 = 12;

        private const int LED_PINC1 = 6;
        private const int LED_PINC2 = 13;
        private const int LED_PINC3 = 19;
        private const int LED_PINC4 = 26;

        private const int LED_PIND1 = 11;
        private const int LED_PIND2 = 9;
        private const int LED_PIND3 = 10;
        private const int LED_PIND4 = 22;

        private GpioPin pin1, pin2, pin3, pin4;
        private int iCounter = 0;
        private bool isStopMotorRequest = false;
        private RunningOption motorOption;
        public bool isBusy = false;
        public void InitialiseGPIO(uint selectmotor)
        {
            switch (selectmotor)
            {
                case 1:
                    pin1 = GpioController.GetDefault().OpenPin(LED_PIN1);
                    pin1.Write(GpioPinValue.Low);
                    pin1.SetDriveMode(GpioPinDriveMode.Output);
                    pin2 = GpioController.GetDefault().OpenPin(LED_PIN2);
                    pin2.Write(GpioPinValue.Low);
                    pin2.SetDriveMode(GpioPinDriveMode.Output);
                    pin3 = GpioController.GetDefault().OpenPin(LED_PIN3);
                    pin3.Write(GpioPinValue.Low);
                    pin3.SetDriveMode(GpioPinDriveMode.Output);
                    pin4 = GpioController.GetDefault().OpenPin(LED_PIN4);
                    pin4.Write(GpioPinValue.Low);
                    pin4.SetDriveMode(GpioPinDriveMode.Output);
                    break;

                case 2:
                    pin1 = GpioController.GetDefault().OpenPin(LED_PINB1);
                    pin1.Write(GpioPinValue.Low);
                    pin1.SetDriveMode(GpioPinDriveMode.Output);
                    pin2 = GpioController.GetDefault().OpenPin(LED_PINB2);
                    pin2.Write(GpioPinValue.Low);
                    pin2.SetDriveMode(GpioPinDriveMode.Output);
                    pin3 = GpioController.GetDefault().OpenPin(LED_PINB3);
                    pin3.Write(GpioPinValue.Low);
                    pin3.SetDriveMode(GpioPinDriveMode.Output);
                    pin4 = GpioController.GetDefault().OpenPin(LED_PINB4);
                    pin4.Write(GpioPinValue.Low);
                    pin4.SetDriveMode(GpioPinDriveMode.Output);
                    break;

                case 3:
                    pin1 = GpioController.GetDefault().OpenPin(LED_PINC1);
                    pin1.Write(GpioPinValue.Low);
                    pin1.SetDriveMode(GpioPinDriveMode.Output);
                    pin2 = GpioController.GetDefault().OpenPin(LED_PINC2);
                    pin2.Write(GpioPinValue.Low);
                    pin2.SetDriveMode(GpioPinDriveMode.Output);
                    pin3 = GpioController.GetDefault().OpenPin(LED_PINC3);
                    pin3.Write(GpioPinValue.Low);
                    pin3.SetDriveMode(GpioPinDriveMode.Output);
                    pin4 = GpioController.GetDefault().OpenPin(LED_PINC4);
                    pin4.Write(GpioPinValue.Low);
                    pin4.SetDriveMode(GpioPinDriveMode.Output);
                    break;

                case 4:
                    pin1 = GpioController.GetDefault().OpenPin(LED_PIND1);
                    pin1.Write(GpioPinValue.Low);
                    pin1.SetDriveMode(GpioPinDriveMode.Output);
                    pin2 = GpioController.GetDefault().OpenPin(LED_PIND2);
                    pin2.Write(GpioPinValue.Low);
                    pin2.SetDriveMode(GpioPinDriveMode.Output);
                    pin3 = GpioController.GetDefault().OpenPin(LED_PIND3);
                    pin3.Write(GpioPinValue.Low);
                    pin3.SetDriveMode(GpioPinDriveMode.Output);
                    pin4 = GpioController.GetDefault().OpenPin(LED_PIND4);
                    pin4.Write(GpioPinValue.Low);
                    pin4.SetDriveMode(GpioPinDriveMode.Output);
                    break;
                default: break;
            }


            GpioController controller = GpioController.GetDefault();
        }
        
        public void ForceToStopMotor()
        {
            isStopMotorRequest = true;
            ReleaseAllGpio();
        }

        private void ReleaseAllGpio()
        {
            pin1.Write(GpioPinValue.Low);
            pin2.Write(GpioPinValue.Low);
            pin3.Write(GpioPinValue.Low);
            pin4.Write(GpioPinValue.Low);
        }

        private void MotorThread(IAsyncAction action)
        {
            UInt32 PhaseNumber = motorOption.Step;
            isBusy = true;
            //This motor thread runs on a high priority task and loops forever to pulse the motor
            while (isStopMotorRequest == false)
            {
                #region 8 phase motor stepping
                switch (iCounter)
                {
                    
                    // 8 stats stepping
                    case 0:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.High);
                        break;
                    case 1:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.High);
                        pin4.Write(GpioPinValue.High);
                        break;
                    case 2:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.High);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 3:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.High);
                        pin3.Write(GpioPinValue.High);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 4:
                        pin1.Write(GpioPinValue.Low);
                        pin2.Write(GpioPinValue.High);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 5:
                        pin1.Write(GpioPinValue.High);
                        pin2.Write(GpioPinValue.High);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 6:
                        pin1.Write(GpioPinValue.High);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.Low);
                        break;
                    case 7:
                        pin1.Write(GpioPinValue.High);
                        pin2.Write(GpioPinValue.Low);
                        pin3.Write(GpioPinValue.Low);
                        pin4.Write(GpioPinValue.High);
                        break;
                    default: break;
                }
                #endregion
                if (motorOption.RunInfitive == false)
                {
                    PhaseNumber--;
                    if (PhaseNumber == 0) isStopMotorRequest = true;
                }
                if (motorOption != null)
                {
                    if (motorOption.dir == Direction.Left)
                    {
                        if (iCounter == 0) iCounter = 7;
                        else iCounter--;
                    }
                    else
                    {
                        if (iCounter == 7) iCounter = 0;
                        else iCounter++;
                    }
                }
                else
                {
                    if (iCounter == 7) iCounter = 0;
                    else iCounter++;
                }

                //Use the wait helper method to wait for the length of the pulse
                Wait(motorOption.StepDelay);
            }
            ReleaseAllGpio();
            isBusy = false;
        }

        public async void RunMotor(RunningOption option)
        {
            if (isBusy == false)
            {
                isStopMotorRequest = false;
                motorOption = new RunningOption();
                motorOption = option;
                if (motorOption.Step != 0) await ThreadPool.RunAsync(this.MotorThread, WorkItemPriority.High);
            }
        }
        public async void RunMotor()
        {
            motorOption = new RunningOption();
            await ThreadPool.RunAsync(this.MotorThread, WorkItemPriority.High);
        }
        //A synchronous wait is used to avoid yielding the thread 
        //This method calculates the number of CPU ticks will elapse in the specified time and spins
        //in a loop until that threshold is hit. This allows for very precise timing.
        private void Wait(double milliseconds)
        {
            long initialTick = stopwatch.ElapsedTicks;
            long initialElapsed = stopwatch.ElapsedMilliseconds;
            double desiredTicks = milliseconds / 1000.0 * Stopwatch.Frequency;
            double finalTick = initialTick + desiredTicks;
            while (stopwatch.ElapsedTicks < finalTick)
            {

            }
        }
    }
}
