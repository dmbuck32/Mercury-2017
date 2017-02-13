
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net;
using System.Net.Sockets;
using System.IO.Ports;


namespace APMCommTest
{

    class Program
    {
       static SerialPort apmPort;
       static bool cont;
       static Object thisLock = new Object();
       static bool threadRun = false;

        static void Main(string[] args)
        {
           StringComparer stringComparer = StringComparer.OrdinalIgnoreCase;
           string message;
           
            apmPort = new SerialPort();
            apmPort.PortName = "COM4";
            apmPort.DataBits = 8;
            apmPort.Parity = Parity.None;
            apmPort.StopBits = StopBits.One;
            apmPort.BaudRate = 115200;
            apmPort.DtrEnable = true;
            apmPort.ReadBufferSize = 1024 * 1024 * 4;
                
            apmPort.ReadTimeout = 500;
            apmPort.WriteTimeout = 500;

            apmPort.Open();
               
                try
                {
                    apmPort.DiscardInBuffer();
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Problem with discard: " + ex.Message);
                }

                System.Threading.Thread readThread = new System.Threading.Thread(delegate()
                    {

                        threadRun = true;

                        try
                        {
                            apmPort.Write("\r");
                        }
                        catch { }


                        waitandsleep(10000);

                        Console.WriteLine("Terminal thread 1 run " + threadRun + " " + apmPort.IsOpen);

                        readandsleep(100);

                        Console.WriteLine("Terminal thread 2 run " + threadRun + " " + apmPort.IsOpen);

                        try
                        {
                            if (apmPort.IsOpen)
                                apmPort.Write("\n\n\n");

                            // 1 secs
                            if (apmPort.IsOpen)
                                readandsleep(1000);

                            if (apmPort.IsOpen)
                                apmPort.Write("\r\r\r?\r");
                        }
                        catch (Exception ex) { Console.WriteLine("Terminal thread 3 " + ex.ToString()); threadRun = false; return; }

                        Console.WriteLine("Terminal thread 3 run " + threadRun + " " + apmPort.IsOpen);

                        while (threadRun)
                        {
                            try
                            {
                                System.Threading.Thread.Sleep(10);

                                if (!threadRun)
                                    break;
                                if (!apmPort.IsOpen)
                                {
                                    Console.WriteLine("Comport Closed");
                                    break;
                                }
                                if (apmPort.BytesToRead > 0)
                                {
                                    apmPort_DataReceived((object)null, (SerialDataReceivedEventArgs)null);
                                }
                            }
                            catch (Exception ex) { Console.WriteLine("Terminal thread 4 " + ex.ToString()); }
                        }

                        threadRun = false;
                        try
                        {
                            apmPort.DtrEnable = false;
                        }
                        catch { }

                        try
                        {
                            Console.WriteLine("term thread close run " + threadRun + " " + apmPort.IsOpen);
                            apmPort.Close();
                        }
                        catch { }

                        Console.WriteLine("Comport thread close run " + threadRun);             
                    });

                readThread.IsBackground = true;
                readThread.Name = "Terminal Serial Thread";
                readThread.Start();

                Console.WriteLine("Opened com port\r\n");

                cont = true;

                while (cont)
                {
                    string input = Console.ReadLine();

                    //if (stringComparer.Equals("start", input))
                    //{
                        try
                        {
                            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
                            //byte[] data = encoding.GetBytes("exit\rsetup\rshow\r");
                            byte[] data = encoding.GetBytes(input + "\r");
                            apmPort.Write(data, 0, data.Length);
                        }
                        catch
                        {
                        }

                   // }
                }
        }


        public static void printTxt(string data)
        {
            data = data.Replace("U3", "");
            data = data.Replace("U$", "");
            data = data.Replace(@"U""", "");
            data = data.Replace("d'`F", "");
            data = data.Replace("U.", "");
            data = data.Replace("'`", "");

            data = data.TrimEnd('\r'); // else added \n all by itself
            data = data.Replace("\0", "");

            Console.Write(data);

            if (data.Contains("\b"))
            {
                Console.WriteLine();
            }

        }

        private static void waitandsleep(int time)
        {
            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalMilliseconds < time)
            {
                try
                {
                    if (!apmPort.IsOpen || apmPort.BytesToRead > 0)
                    {
                        return;
                    }
                }
                catch { threadRun = false; return; }
            }
        }

        private static void readandsleep(int time)
        {
            DateTime start = DateTime.Now;

            while ((DateTime.Now - start).TotalMilliseconds < time)
            {
                try
                {
                    if (!apmPort.IsOpen)
                        return;
                    if (apmPort.BytesToRead > 0)
                    {
                        apmPort_DataReceived((object)null, (SerialDataReceivedEventArgs)null);
                    }
                }
                catch { threadRun = false; return; }
            }
        }


       static void apmPort_DataReceived(object sender, SerialDataReceivedEventArgs e)
        {
            if (!apmPort.IsOpen)
                return;

            // if btr > 0 then this shouldnt happen
            apmPort.ReadTimeout = 300;

            try
            {
                lock (thisLock)
                {
                    byte[] buffer = new byte[256];
                    int a = 0;

                    while (apmPort.IsOpen && apmPort.BytesToRead > 0)
                    {
                        byte indata = (byte)apmPort.ReadByte();

                        buffer[a] = indata;

                        if (buffer[a] >= 0x20 && buffer[a] < 0x7f || buffer[a] == (int)'\n' || buffer[a] == (int)'\r')
                        {
                            a++;
                        }
                        if (a == (buffer.Length - 1))
                            break;
                    }

                    printTxt(ASCIIEncoding.ASCII.GetString(buffer, 0, a + 1));
                }
            }
            catch (Exception ex) { Console.WriteLine(ex.ToString()); if (!threadRun) return;}
        }

        //public void toggleDTR()
        //{

        //    bool open = this.IsOpen;
        //    Console.WriteLine("toggleDTR " + this.IsOpen);
        //    try
        //    {
        //        if (!open)
        //            this.Open();
        //    }
        //    catch { }


        //    base.DtrEnable = false;
        //    base.RtsEnable = false;

        //    System.Threading.Thread.Sleep(50);

        //    base.DtrEnable = true;
        //    base.RtsEnable = true;

        //    System.Threading.Thread.Sleep(50);

        //    try
        //    {
        //        if (!open)
        //            this.Close();
        //    }
        //    catch { }
        //    Console.WriteLine("toggleDTR done " + this.IsOpen);
        //}
    }


}
