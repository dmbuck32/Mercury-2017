using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Timers;


namespace Arduino_LED_Client
{
    class Program
    {

        static ZClient client;
        static Thread stateProcessor;
        static CancellationTokenSource tokenSource = new CancellationTokenSource();

        static void Main(string[] args)
        {
            String serverIP = "";
            Int32 serverPort = 0;

            Console.WriteLine("Enter the Server IP address: ");
            serverIP = Console.ReadLine();
            Console.WriteLine("Enter the port: ");
            serverPort = Int32.Parse(Console.ReadLine());

            try
            {
                client = new ZClient(serverIP, serverPort);
                client.PacketReceived += new EventHandler<DataArgs>(client_PacketReceived);

                //packet handler - runs in its own thread
                stateProcessor = new Thread(new ThreadStart(StateProcessorDoWork));
                stateProcessor.Start();
            }
            catch(Exception ex)
            {
                Console.WriteLine("Error during startup: " + ex.Message);
                return;
            }
            while (true)
            {
                Console.WriteLine("Enter exit to shutdown");
                string input = Console.ReadLine().ToLower();
                if (input.Contains("exit"))
                {
                    break;
                }
            }

            //shutdown sequence
            client.PacketReceived -= client_PacketReceived;
            client.Close();
            tokenSource.Cancel();
            stateProcessor.Join();
        }

        // handler for commands received from the Server
        static void client_PacketReceived(object sender, DataArgs e)
        {
            try
            {
                using (MemoryStream ms = new MemoryStream(e.Data))
                {
                    // robot drive state received - enqueue the state so it is processed in the StateProcessorDoWork() below
                    //RobotState state = (RobotState)robotStateDeserializer.Deserialize(ms);
                    //stateQueue.Enqueue(state);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Error deserializing state: " + ex.Message);
            }
        }

        static void StateProcessorDoWork()
        {
            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {

                    //Comms.RobotState robotState = stateQueue.Dequeue(tokenSource.Token);

                    // Lynx Controller
                    //if ((robotState.DriveState != null) && (UseLynx))
                    //{
                    //    Dictionary<Devices, int> driveState = kinematics.GetWheelStates(robotState.DriveState.Radius, robotState.DriveState.Speed);
                    //    lynxController.EnqueueState(driveState);


                    //    //if (robotState.CmdLineState.CmdLine.CompareTo("") != 0)
                    //    if (robotState.CmdLineState.CmdLine != null)
                    //        lynxController.EnqueueCmd(robotState.CmdLineState.CmdLine);

                     //   if (robotState.TowerState.TowerMotor != 0)
                     //       lynxController.EnqueueTower(robotState.TowerState);
                    //    if ((robotState.ArmState != null) && (UseArm))
                    //        lynxController.EnqueueArm(robotState.ArmState.GamePadVariables);                   

                    //}
                }
                catch (OperationCanceledException ex)
                {
                    Console.WriteLine("StateProcessor: " + ex.Message);
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("StateProcessor: unhandled exception: " + ex.Message);
                }
            }
            Console.WriteLine("StateProcessor exiting...");
        }
    }

    public class ZClient
    {

        //CONSTANTS
        //================================================
        private const Int32 RECEIVE_BUFFER_SIZE = 2048;
        private const Int32 RECEIVE_TIMEOUT = 10000;


        //VARS
        //================================================
        private NetworkStream networkStream;
        private System.Timers.Timer GeneralTimer = new System.Timers.Timer(5000);
        private DateTime LastHeardFromServer;
        private CancellationTokenSource TokenSource;
        private byte[] parsingBytes = null;
        private int currentParsingIndex = 0;

        private String _serverIP;
        private Int32 _serverPort;
        private TcpClient TCPClient;

        //EVENTS
        //=================================================
        public event EventHandler<DataArgs> PacketReceived;


        //UTILITIES
        //=================================================
        private BlockingQueue<String> outputQueue;          //Queue commands headed to Server
        private BlockingQueue<byte[]> bytesQueue;

        //THREADING
        //=================================================
        private Thread ZClientReadThread;
        private Thread ZClientWriteThread;
        private Thread ParsingThread;
        private readonly object generalSync = new object(); //Token to enter thread lock. Allows a scope 
                                                            //of statements to be synchronized between 
                                                            //threads.

        public ZClient(String ip, Int32 port)
        {
            LastHeardFromServer = DateTime.UtcNow;
            _serverIP = ip;
            _serverPort = port;

            bytesQueue = new BlockingQueue<byte[]>(-1);
            outputQueue = new BlockingQueue<string>(-1);

            //Attempt to connect to server
            GeneralTimer.Elapsed += new ElapsedEventHandler(generalTimer_Elapsed);
            GeneralTimer.Enabled = true;
        }

        public void Close()
        {
            // disable the reconnect timer
            GeneralTimer.Enabled = false;

            TokenSource.Cancel();
            // perform cleanup of any objects
            if ((TCPClient != null) && isConnected())
            {
                TCPClient.Client.Disconnect(false);
            }

            if (TCPClient != null)
            {
                TCPClient.Close();
            }

            //join all threads
            if (ZClientReadThread != null && ZClientReadThread.ThreadState != ThreadState.Unstarted)
                ZClientReadThread.Join();

            if (ZClientWriteThread != null && ZClientWriteThread.ThreadState != ThreadState.Unstarted)
                ZClientWriteThread.Join();

            if (ParsingThread != null && ParsingThread.ThreadState != ThreadState.Unstarted)
                ParsingThread.Join();

        }

        private bool isConnected()
        {
            try
            {
                if (TCPClient.Connected)
                {
                    //Determine status of TCPClient underlying socket. Is it writable? Are there errors?
                    if ((TCPClient.Client.Poll(0, SelectMode.SelectWrite)) && (!TCPClient.Client.Poll(0, SelectMode.SelectError)))
                    {
                        Console.WriteLine("This Socket is writable and has no errors...");

                        byte[] buffer = new byte[1];

                        if (TCPClient.Client.Receive(buffer, SocketFlags.Peek) == 0)
                        {
                            return false;
                        }
                        else
                        {
                            Console.WriteLine("TCPClient connected...");
                            return true;
                        }

                    }
                    else
                    {
                        Console.WriteLine("This Socket is either readonly or has errors...");
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
                Console.WriteLine("isConnected(): " + ex.Message);
                return false;
            }

        }
       
        /// <summary>
        /// Manually created Thread executes this function.
        /// </summary>
        private void readFromServer_DoWork()
        {
            while(!TokenSource.Token.IsCancellationRequested)
            {
                // Sleep this thread occassionally. This helps reduce cpu load - I know, because someone tested it.
                Thread.Sleep(25); //TODO Did someone test this on i7?

                // check that ZClient object has not been disposed for some unknown reason - someone saw this happen once - keeping
                // and eye on it
                if (TCPClient.Client != null)
                {
                    try
                    {
                        //wait until at least 10 bytes in buffer before reading. this helps reduce CPU load TODO: for i7?
                        if((isConnected()) && (TCPClient.Client.Available > 10))
                        {
                            //Read from ZClient's Buffer - BLOCKS of bytes - but that's ok because it is in its own thread. 
                            //but in our case bytes are available to be read so it does not block.
                            byte[] bytes = new byte[TCPClient.Client.Available];
                            networkStream.Read(bytes, 0, TCPClient.Client.Available);

                            //Update timestamp we last heard from server - used for heartbeat functionality
                            LastHeardFromServer = DateTime.UtcNow;
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
        /// Manually created Thread executes this function.
        /// </summary>
        private void writeToServer_DoWork()
        {
            System.Text.ASCIIEncoding encoding = new System.Text.ASCIIEncoding();
            while (!TokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    String s = outputQueue.Dequeue(TokenSource.Token);
                    //convert string to binary and send
                    byte[] DataToSend = encoding.GetBytes(s);

                    if ((TCPClient != null) && (isConnected()))
                    {
                        try
                        {
                            TCPClient.Client.Send(DataToSend);
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

        /// <summary>
        /// Manually created Thread executes this function.
        /// </summary>
        private void parsingThread_DoWork()
        {
            byte current = TCPConstants.STOP_TOKEN; //safe initialization
            try
            {
                while (!TokenSource.Token.IsCancellationRequested)
                {
                    while (current != TCPConstants.START_TOKEN)
                    { //read until the start byte
                        current = GetNextByte(TokenSource.Token);
                    }

                    //now have the start byte!
                    MemoryStream stream = new MemoryStream();

                    while (true)
                    { //read and store until a start or stop byte is encountered...
                        current = GetNextByte(TokenSource.Token);

                        if (current == TCPConstants.START_TOKEN || current == TCPConstants.STOP_TOKEN)
                            break;
                        stream.WriteByte(current);
                    }

                    if (current == TCPConstants.STOP_TOKEN) //have a packet
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

        private void generalTimer_Elapsed(object sender, ElapsedEventArgs e)
        {
            //Attempts to acquire an exclusive lock on generalSync
            if (Monitor.TryEnter(generalSync, 300))
            {
                Console.WriteLine("Thread lock on {0} acquired...", Thread.CurrentThread.Name);
                try
                {
                    //Attempt tp connect to server. 
                    //If ZClient is not instantiated or we are not connected build/rebuild the objects.
                    if ((TCPClient == null) || (!isConnected()))
                    {
                        try
                        {
                            //We were once connected, but lost connection.
                            //Tear down existing objects.
                            //-----------------------------------------------
                            if (TokenSource != null)
                            {
                                TokenSource.Cancel();
                            }

                            if (TCPClient != null)
                            {
                                TCPClient.Close();
                            }

                            //The Join method is used to ensure a thread has terminated. The caller will block 
                            //indefinitely if the thread does not terminate. If the thread has already terminated 
                            //when Join is called, the method returns immediately. You cannot invoke Join on a thread 
                            //that is in the ThreadState.Unstarted state, hence the check for an unstarted thread.
                            if (ZClientReadThread != null && ZClientReadThread.ThreadState != ThreadState.Unstarted)
                            {
                                ZClientReadThread.Join();
                                Console.WriteLine("ZClientReadThread.Join() returned...");
                            }

                            if (ZClientWriteThread != null && ZClientWriteThread.ThreadState != ThreadState.Unstarted)
                            {
                                ZClientWriteThread.Join();
                                Console.WriteLine("ZClientWriteThread.Join() returned...");
                            }

                            if (ParsingThread != null && ParsingThread.ThreadState != ThreadState.Unstarted)
                            {
                                ParsingThread.Join();
                                Console.WriteLine("ParsingThread.Join() returned...");
                            }

                            bytesQueue.Clear();
                            parsingBytes = null;

                            //Build/Rebuild Threads and objects
                            //------------------------------------------
                            TokenSource = new CancellationTokenSource();

                            TCPClient = new TcpClient();
                            TCPClient.ReceiveBufferSize = RECEIVE_BUFFER_SIZE;
                            TCPClient.ReceiveTimeout = RECEIVE_TIMEOUT; //TODO: Implement this

                            ZClientReadThread = new Thread(new ThreadStart(readFromServer_DoWork));
                            ZClientWriteThread = new Thread(new ThreadStart(writeToServer_DoWork));
                            ParsingThread = new Thread(new ThreadStart(parsingThread_DoWork));

                            Console.WriteLine("Attempting to connect to " + _serverIP);
                            TCPClient.Connect(IPAddress.Parse(_serverIP), _serverPort);
                            Console.WriteLine("Connected!");

                            networkStream = TCPClient.GetStream();

                            // give time for network stream to make connection then start threads
                            Thread.Sleep(1000);
                            //reset heartbeat
                            LastHeardFromServer = DateTime.UtcNow;

                            // restart the threads
                            ZClientReadThread.Start();
                            ZClientWriteThread.Start();
                            ParsingThread.Start();
                        }
                        catch (SocketException ex)
                        {
                            Console.WriteLine(ex.Message);
                        }

                    }
                    else
                    {
                        // ===================================================================================
                        // heartbeat functionality - 
                        // we are connected - check that we heard from the Server in the last X seconds.  if not,
                        // disconnect, and rebuilt sockets, objects, and reconnect
                        TimeSpan ts = DateTime.UtcNow.Subtract(LastHeardFromServer);
                        if (ts.TotalSeconds > 15)
                        {
                            // disconnect the client to force it to reconnect
                            if (TCPClient != null)
                            {
                                LastHeardFromServer = DateTime.UtcNow;
                                Console.WriteLine("TIMEOUT FROM SERVER - RESTARTING CLIENT: " + ts.TotalSeconds.ToString());
                                TCPClient.Client.Disconnect(false);
                                TCPClient.Close();
                            }
                        }
                    }

                }
                catch (SocketException ex)
                {
                    Console.WriteLine(ex.Message);
                }
                finally
                {
                    Monitor.Exit(generalSync);
                }

            }
            else
            {
                Console.WriteLine("Timeout occured... Possible Deadlock!");
            }
        }
    }

    /// <summary>
    /// In many concurrent systems, one thread performs some work, the result of which another thread consumes. 
    /// This producer/consumer pattern is frequently implemented on top of blocking queues. For a concurrent 
    /// application, where the producer and consumer are executing on separate threads (and where there might 
    /// be multiple instances of both the producers and consumers executing simultaneously), synchronization 
    /// mechanisms are necessary on top of queue. More specifically, typically a design calls for a consumer 
    /// to block until an item is available for it to consume. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    public class BlockingQueue<T>
    {
         private volatile int dequeueTimeout;

        private Queue<T> queue;
        private readonly object queueSync = new object();

        private ManualResetEventSlim resetEvent;

        public BlockingQueue(int opt_timeout = 1000)
        {
            //-1 for timeout is wait infinite
            if (opt_timeout <= 0 && opt_timeout != -1)
                throw new ArgumentOutOfRangeException("opt_timeout");

            dequeueTimeout = opt_timeout;
            resetEvent = new ManualResetEventSlim(false);
            queue = new Queue<T>();
        }

        public void Enqueue(T newItem)
        {
            if (newItem == null)
                throw new ArgumentNullException("newItem");

            Monitor.Enter(queueSync);
            try
            {
                queue.Enqueue(newItem);
                resetEvent.Set();
            }
            finally
            {
                Monitor.Exit(queueSync);
            }
        }

        public T Dequeue()
        {
            if (resetEvent.Wait(dequeueTimeout))
            {
                Monitor.Enter(queueSync);
                try
                {
                    return queue.Dequeue();
                }
                finally
                {
                    if (queue.Count <= 0)
                        resetEvent.Reset();
                    Monitor.Exit(queueSync);
                }
            }
            else
                throw new TimeoutException("Dequeue operation timed out");
        }

        public T Dequeue(CancellationToken token)
        {
            if (resetEvent.Wait(dequeueTimeout, token))
            {
                Monitor.Enter(queueSync);
                try
                {
                    return queue.Dequeue();
                }
                finally
                {
                    if (queue.Count <= 0)
                        resetEvent.Reset();
                    Monitor.Exit(queueSync);
                }
            }
            else
                throw new TimeoutException("Dequeue operation timed out");
        }

        public void Clear()
        {
            Monitor.Enter(queueSync);
            try
            {
                queue.Clear();
            }
            finally
            {
                Monitor.Exit(queueSync);
            }
        }

        public int DequeueTimeout
        {
            get { return dequeueTimeout; }
            set
            {
                if (value <= 0 && value != -1)
                    throw new ArgumentOutOfRangeException("value");
                else
                    dequeueTimeout = value;
            }
        }
    }

    public static class TCPConstants
    {
        public static readonly byte START_TOKEN = 0xFE;
        public static readonly byte STOP_TOKEN = 0xFF;
    }

    public class DataArgs : EventArgs
    {
        private byte[] data;

        public DataArgs(byte[] data)
        {
            this.data = data;
        }

        public byte[] Data
        {
            get { return this.data; }
        }
    }

}
