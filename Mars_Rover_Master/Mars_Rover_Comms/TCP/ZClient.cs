
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Timers;
using System.Collections.Concurrent;
using Utility;

namespace Mars_Rover_Comms.TCP
{
    public class ZClient
    {
        private NetworkStream networkStream;
        private TcpClient tcpClient;
        
        private Thread ZClientReadThread;
        private Thread ZClientWriteThread;
        private Thread ParsingThread;
       
        private CancellationTokenSource tokenSource;

        //default general purpose thread
        private System.Timers.Timer GeneralTimer = new System.Timers.Timer(5000);   
        
        //Lock Token
        private readonly object generalSync = new object();

        //want to send a cmd to server, just add to Q
        private Utility.BlockingQueue<byte[]> outputQueue;                    
        private Utility.BlockingQueue<byte[]> bytesQueue;

        // ip address to connect to
        private String ocu_ip_address;                                              
        private Int32 ocu_port;
        
        // time we last heard from OCU-server
        private DateTime LastHeardFromOCUServer;                                    
       

        public ZClient(String ip, Int32 port)
        {
            LastHeardFromOCUServer = DateTime.UtcNow;
            ocu_ip_address = ip;
            ocu_port = port;

            bytesQueue = new Utility.BlockingQueue<byte[]>(-1);
            outputQueue = new Utility.BlockingQueue<byte[]>(-1);

            // start the timer first to get the client connected and build the objects
            GeneralTimer.Elapsed += new ElapsedEventHandler(GeneralTimer_Elapsed);
            GeneralTimer.Enabled = true;
        }

        public void Close()
        {
            
            GeneralTimer.Enabled = false;
            if(tokenSource != null)
            {
                tokenSource.Cancel();
            }
                  
            //perform cleanup of any objects
            if ((tcpClient != null) && IsConnected())
            {
                //Disconnect and do not reuse socket
                tcpClient.Client.Disconnect(false);
            }

            if (tcpClient != null)
            {
                tcpClient.Close();
            }

            //join all threads
            if (ZClientReadThread != null && ZClientReadThread.ThreadState != ThreadState.Unstarted)
                ZClientReadThread.Join();

            if (ZClientWriteThread != null && ZClientWriteThread.ThreadState != ThreadState.Unstarted)
                ZClientWriteThread.Join();

            if (ParsingThread != null && ParsingThread.ThreadState != ThreadState.Unstarted)
                ParsingThread.Join();
        }

        public void SendToOCUServer(String s)
        {

            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            byte[] DataToSend = encoding.GetBytes(s);

            byte[] packet = new byte[DataToSend.Length + 2];
            packet[0] = Mars_Rover_Comms.TCP.TCPConstants.StartToken;
            packet[packet.Length - 1] = Mars_Rover_Comms.TCP.TCPConstants.StopToken;

            outputQueue.Enqueue(packet);
        }

        public void SendToOCUServer(MemoryStream stream)
        {
            byte[] packet = new byte[stream.Length + 2];
            packet[0] = Mars_Rover_Comms.TCP.TCPConstants.StartToken;
            packet[packet.Length - 1] = Mars_Rover_Comms.TCP.TCPConstants.StopToken;
            stream.Position = 0;
            stream.Read(packet, 1, (Int32)stream.Length);

            outputQueue.Enqueue(packet);
        }


        public event EventHandler<DataArgs> PacketReceived;

        /// <summary>
        /// Checks the connection state
        /// </summary>
        /// <returns>True on connected. False on disconnected.</returns>
        public bool IsConnected()
        {
            try
            {
                if (tcpClient.Connected)
                {
                    if ((tcpClient.Client.Poll(0, SelectMode.SelectWrite)) && (!tcpClient.Client.Poll(0, SelectMode.SelectError)))
                    {
                        byte[] buffer = new byte[1];
                        if (tcpClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
                        {
                            return false;
                        }
                        else
                        {
                            return true;
                        }
                    }
                    else
                    {
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("IsConnected(): " + ex.Message);
                return false;
            }
        }

        /// <summary>
        /// General Purpose Timer Elapsed Handler - Handles Periodic Events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void GeneralTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(generalSync))
            {
                try
                {
                    //=======================================
                    // attempt to connect to OCU/server every 5 seconds

                    // if zclient not instantiated or we are not connected, then build up the objects
                    if ((tcpClient == null) || (!IsConnected()))
                    {
                        try
                        {
                            // whenever we are disconnected, then rebuild all objects
                            // and threads from scratch.  this is rather drastic, but it 
                            // helps keep this app up and running

                            //======================================================================
                            // tear down the client object and stop the threads if they are running
                            if (tokenSource != null)
                                tokenSource.Cancel();

                            if (tcpClient != null)
                                tcpClient.Close();

                            if (ZClientReadThread != null && ZClientReadThread.ThreadState != ThreadState.Unstarted)
                                ZClientReadThread.Join();

                            if (ZClientWriteThread != null && ZClientWriteThread.ThreadState != ThreadState.Unstarted)
                                ZClientWriteThread.Join();

                            if (ParsingThread != null && ParsingThread.ThreadState != ThreadState.Unstarted)
                                ParsingThread.Join();

                            //reset parsing queue
                            bytesQueue.Clear();
                            parsingBytes = null;

                            //==============================================
                            // rebuild the object and threads - initialize
                            tokenSource = new CancellationTokenSource();
                            tcpClient = new TcpClient();
                            
                            tcpClient.ReceiveBufferSize = 2048;
                            tcpClient.ReceiveTimeout = 1000;   //client will throw timeout if it hasnt heard from server in 10 seconds: todo, implement this
                            
                            ZClientReadThread = new Thread(new ThreadStart(ReadFromServer_DoWork)); //thread for reading from the server
                            ZClientWriteThread = new Thread(new ThreadStart(WriteToServer_DoWork)); //thread for writing to the server                          
                            ParsingThread = new Thread(new ThreadStart(ParsingThread_DoWork));

                            Console.WriteLine("Attempting to connect to " + ocu_ip_address);
                            tcpClient.Connect(IPAddress.Parse(ocu_ip_address), ocu_port);
                            Console.WriteLine("Connection Successful");
                            
                            // grab the network stream
                            networkStream = tcpClient.GetStream();

                            // give time for network stream to make connection then start threads
                            Thread.Sleep(1000);
                            
                            //reset heartbeat
                            LastHeardFromOCUServer = DateTime.UtcNow;
                            
                            // restart the threads
                            ZClientReadThread.Name = "Read Thread";
                            ZClientReadThread.Start();
                            
                            ZClientWriteThread.Name = "Write Thread";
                            ZClientWriteThread.Start();
                            
                            ParsingThread.Name = "Parsing Thread";
                            ParsingThread.Start();

                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine("$$ Socket Exception: " + ex.Message);
                        }
                    }
                    else // we are connected - check that we heard from the OCU in the last X seconds.  
                    {
                        TimeSpan ts = DateTime.UtcNow.Subtract(LastHeardFromOCUServer);
                        if (ts.TotalSeconds > 15)
                        {                      
                            if (tcpClient != null)
                            {
                                // disconnect the client to force it to reconnect if we cannont talk with server
                                if ((!tcpClient.Client.Poll(0, SelectMode.SelectWrite)) || (tcpClient.Client.Poll(0, SelectMode.SelectError)))
                                {
                                    LastHeardFromOCUServer = DateTime.UtcNow;
                                    Console.WriteLine("TIMEOUT FROM OCU - RESTARTING CLIENT: " + ts.TotalSeconds.ToString());
                                    tcpClient.Client.Disconnect(false);
                                    tcpClient.Close();
                                }
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ZClient: Unhandled exception in GeneralTimer: " + ex.Message);
                }
                finally
                {
                    Monitor.Exit(generalSync);
                }
            }
        }

        /// <summary>
        /// Threaded Function for Reading from the Server
        /// </summary>
        private void ReadFromServer_DoWork()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                // Sleep this thread occassionally.  this helps reduce cpu load - i know, because i tested it.
                Thread.Sleep(25);

                // check that ZClient object has not been disposed for some unknown reason - saw this happen once - keeping
                // and eye on it
                if (tcpClient.Client != null)
                {
                    //wait until at least 10 bytes in buffer before reading. this helps reduce CPU load
                    try
                    {
                        if ((IsConnected()) && (tcpClient.Client.Available > 2)) //10
                        {
                            // read from ZClient's Buffer - blocks but that's ok because it is in its own thread. but in our case
                            // bytes are available to be read so it does not block.
                            byte[] bytes = new byte[tcpClient.Client.Available];
                            networkStream.Read(bytes, 0, tcpClient.Client.Available);

                            // update timestamp we last heard from server - used for heartbeat functionality
                            LastHeardFromOCUServer = DateTime.UtcNow;
                            bytesQueue.Enqueue(bytes);
                        }
                    }
                    catch (ObjectDisposedException ex)
                    {
                        Console.WriteLine("ZClient: Object disposed exception! " + ex.Message);
                    }
                    catch (TimeoutException)
                    {
                        Console.WriteLine("ZClient: Timeout attempting to read from network stream");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("ZClient: Unhandled exception in ReadFromServer: " + ex.Message);
                    }
                }
            }
            Console.WriteLine("ZClient: ReadFromServer exiting...");
        }

        /// <summary>
        /// Threaded Function for Writing to the Server
        /// </summary>
        private void WriteToServer_DoWork()
        {
            
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    byte[] DataToSend = outputQueue.Dequeue(tokenSource.Token);

                    byte[] packet = new byte[DataToSend.Length + 2];
                    packet[0] = Mars_Rover_Comms.TCP.TCPConstants.StartToken;
                    packet[packet.Length - 1] = Mars_Rover_Comms.TCP.TCPConstants.StopToken;
                    Array.Copy(DataToSend, 0, packet, 1, DataToSend.Length);

                    if ((tcpClient != null) && (IsConnected()))
                    {
                        try
                        {
                            tcpClient.Client.Send(packet);
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine("WriteToServer: " + ex.Message);
                        }
                    }
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine("WriteToServer: " + ex.Message);
                }
            }
            Console.WriteLine("WriteToServer exiting...");
        }

        private void ParsingThread_DoWork()
        {
            byte current = Mars_Rover_Comms.TCP.TCPConstants.StopToken; //safe initialization
            try
            {
                while (!tokenSource.Token.IsCancellationRequested)
                {
                    while (current != Mars_Rover_Comms.TCP.TCPConstants.StartToken)
                    { //read until the start byte
                        current = GetNextByte(tokenSource.Token);
                    }

                    //now have the start byte!
                    MemoryStream stream = new MemoryStream();
                    while (true)
                    { //read and store until a start or stop byte is encountered...
                        current = GetNextByte(tokenSource.Token);

                        if (current == Mars_Rover_Comms.TCP.TCPConstants.StartToken || current == Mars_Rover_Comms.TCP.TCPConstants.StopToken)
                            break;
                        stream.WriteByte(current);
                    }
                    if (current == Mars_Rover_Comms.TCP.TCPConstants.StopToken) //have a packet
                    {
                        if (PacketReceived != null)
                            PacketReceived(this, new DataArgs(stream.ToArray()));
                    }
                    else
                    {
                        //received a start byte again - start over!
                    }
                }
            }
            catch (OperationCanceledException ex)
            {
                Console.WriteLine("Parser: " + ex.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine("Parser: Unhandled exception: " + ex.Message);
            }
            finally
            {
                Console.WriteLine("Parser exiting...");
            }
            
        }

        private byte[] parsingBytes = null;
        private int currentParsingIndex = 0;
        private byte GetNextByte(CancellationToken token)
        {
            if (parsingBytes == null)
            {
                parsingBytes = bytesQueue.Dequeue(token);
                currentParsingIndex = 0;
            }

            if (currentParsingIndex < parsingBytes.Length)
            {
                return parsingBytes[currentParsingIndex++];
            }
            else
            {
                parsingBytes = null;
                return GetNextByte(token);
            }
        }
    }
}
