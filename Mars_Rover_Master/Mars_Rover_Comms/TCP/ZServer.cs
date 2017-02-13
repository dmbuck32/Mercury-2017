using System;
using System.Text;
using System.Net.Sockets;
using System.Threading;
using System.Net;
using System.Collections.Generic;
using System.IO;
using System.Collections.Concurrent;
using System.Xml.Serialization;
using Mars_Rover_Comms;
using System.ComponentModel;
using System.Windows.Threading;

namespace Mars_Rover_Comms.TCP
{
    // Todo: switch over to a concurrent container for client list
    // Todo: exception (tcplistener) when restarting server
    // Todo: socket closed from another thread exception?

    public class ClientThreadPair
    {
        public TcpClient Client;
        public Thread Thread;
       

        public ClientThreadPair(TcpClient client, Thread thread)
        {
            if (client == null)
                throw new ArgumentNullException("client");
            if (thread == null)
                throw new ArgumentNullException("thread");

            this.Client = client;
            this.Thread = thread;
        }
    }

    public class ZServer
    {
        #region Private Variables

            private TcpListener tcpListener;
            
            private Thread listenThread;
            private Thread ParsingThread;
            private System.Timers.Timer reaper;
            
            private String _robotLog = "";

            private readonly object _LogLock = new object();
            private readonly object reaperSync = new object();
            private readonly object _clientConnectedLock = new object();

            private CancellationTokenSource tokenSource;

            //no timeout by default
            private int readTimeout = 0;
            
            private int receiveBytesThreshold = 10; 

            private ConcurrentDictionary<System.Net.EndPoint, ClientThreadPair> clientDictionary = new ConcurrentDictionary<EndPoint, ClientThreadPair>();

            // Sets the receive buffer size from default of 8192 to 1024, which makes more sense for our application
            private int receiveBufferSize = 1024;

            private XmlSerializer serializer;
            private Utility.BlockingQueue<byte[]> bytesQueue;

            private Boolean _isClientConnected = false;

            // time we last heard from OCU-server
            private DateTime LastHeardFromClient;


        #endregion

        public ZServer() : this("0.0.0.0", 1111)
        {
        }

        public ZServer(int iPort) : this("0.0.0.0", iPort)
        {
        }

        public ZServer(String sIP, int iPort)
        {
            InitializeServer(sIP, iPort);
           
            
        }

        private void InitializeServer(String sIP, int iPort)
        {
            this.tcpListener = new TcpListener(IPAddress.Parse(sIP), iPort);
            this.listenThread = new Thread(new ThreadStart(ListenForClients_DoWork));
            this.listenThread.Name = "Listen Thread";
            
            this.ParsingThread = new Thread(new ThreadStart(ParsingThread_DoWork));
            this.ParsingThread.Name = "Parsing Thread";  
                 
            reaper = new System.Timers.Timer(5000); //5 second interval to reap disconnected clients
            reaper.Elapsed += new System.Timers.ElapsedEventHandler(reaper_Elapsed);
            bytesQueue = new Utility.BlockingQueue<byte[]>(-1);
            serializer = new System.Xml.Serialization.XmlSerializer(typeof(RobotState));
               
        }

        #region Properties

        public String RobotLog
        {
            get { return _robotLog; }
            set
            {
                lock (_LogLock)
                {
                    if (!value.Equals(_robotLog))
                    {
                        _robotLog = value;
                    }
                }
            }
        }

        public int ReceiveBufferSize
        {
            get { return receiveBufferSize; }
            //will add setter when needed...
        }

  
        public Boolean IsClientConnected
        {
            get 
            {
            return _isClientConnected;
            }
            
            private set 
            {         
                _isClientConnected = value;
                OnClientConnectedChanged();
            }
        }

        public int ReadTimeout
        {
            get { return readTimeout; }
        }

        public int ReceiveBytesThreshold
        {
            get { return receiveBytesThreshold; }
            //will add setter when needed
        }

        #endregion

        #region Property Changed Event Handlers

        //Server PropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        private void OnClientConnectedChanged()
        {
            //Server PropertyChanged
            if (PropertyChanged != null)
                //Server PropertyChanged
                PropertyChanged(this, new PropertyChangedEventArgs("IsClientConnected"));
        }

        #endregion
   
        public int GetNumConnectedClients()
        {
            return clientDictionary.Count;
        }  

        public void Start()
        {
            tokenSource = new CancellationTokenSource();
            this.listenThread.Start();
            this.ParsingThread.Start();
            reaper.Enabled = true;
        }

        public void Stop()
        {
            tokenSource.Cancel();
            
            this.tcpListener.Stop(); //will cause an exception on listenThread
            
            if (this.listenThread != null && this.listenThread.ThreadState != ThreadState.Unstarted)
                this.listenThread.Join();

            if (this.ParsingThread != null && this.ParsingThread.ThreadState != ThreadState.Unstarted)
                this.ParsingThread.Join();

            reaper.Enabled = false;

            Monitor.Enter(reaperSync); //just in case reaper is still running...
            
            try
            { //final cleanup
                foreach (ClientThreadPair pair in clientDictionary.Values)
                {
                    CleanupClient(pair);
                }
                clientDictionary.Clear();
            }
            finally
            {
                Monitor.Exit(reaperSync);
            }
        }

        public void SendToAllClients(Byte[] buffer)
        {
            if(!clientDictionary.IsEmpty)
            {
                byte[] packet = new byte[buffer.Length + 2];
                packet[0] = Mars_Rover_Comms.TCP.TCPConstants.StartToken;
                packet[packet.Length - 1] = Mars_Rover_Comms.TCP.TCPConstants.StopToken;
                Array.Copy(buffer, 0, packet, 1, buffer.Length);

                SendPacketToAllClients(packet);
            }
        }

        public void SendToAllClients(MemoryStream stream)
        {
            if (!clientDictionary.IsEmpty)
            {
                byte[] packet = new byte[stream.Length + 2];
                packet[0] = Mars_Rover_Comms.TCP.TCPConstants.StartToken;
                packet[packet.Length - 1] = Mars_Rover_Comms.TCP.TCPConstants.StopToken;
                stream.Position = 0;
                stream.Read(packet, 1, (Int32)stream.Length);

                SendPacketToAllClients(packet);
            }
        }

        /// <summary>
        /// Does not throw - silent failure.
        /// </summary>
        /// <param name="packet"></param>
        private void SendPacketToAllClients(byte[] packet)
        {
            foreach (ClientThreadPair pair in clientDictionary.Values)
            {
                try
                {
                    if (pair.Client.Connected)
                    {
                        pair.Client.Client.Send(packet);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message); //todo : allow this to propagate?
                }
            }
        }

        private void sendWelcome()
        {
            try
            {
                RobotState initState = new RobotState();

                try
                {
                    //Generate a cmd message
                    initState.CmdLineState = new CmdLineState() { CmdLine = "welcome houston" };
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.Message);
                }

                //send request to robot
                using (System.IO.MemoryStream ms = new System.IO.MemoryStream())
                {
                    serializer.Serialize(ms, initState);
                    SendToAllClients(ms);
                    Console.WriteLine("Welcome Message Sent to Robot...");
                }
            }
            catch (Exception ex)
            {
                //log it or something!
                Console.WriteLine(ex.Message);
            } 
        }

        /// <summary>
        /// Will spawn a new thread for each client that connects
        /// </summary>
        private void ListenForClients_DoWork()
        {
            // Start the Server Listening for Clients to connect while blocking.
            this.tcpListener.Start();

            while (!tokenSource.Token.IsCancellationRequested)
            {
                try
                {
                    // blocks until a client has connected to the server - this is OK because this thread does nothing else, so it can block
                    TcpClient client = this.tcpListener.AcceptTcpClient();
      
                    // sets properties for the client
                    client.ReceiveBufferSize = receiveBufferSize;
                    client.ReceiveTimeout = readTimeout;
                    
                    //setting NoDelay to reduce latency
                    client.Client.NoDelay = true;

                  
                    //remove any client that has the same endpoint
                    ClientThreadPair oldClient;
                    if (clientDictionary.TryRemove(client.Client.RemoteEndPoint, out oldClient))
                    {
                        Console.WriteLine("Accepted client with same endpoint as pre-existing client.  Removing old client...");
                        CleanupClient(oldClient);
                    }

                    Console.WriteLine("Client connected from: " + client.Client.RemoteEndPoint.ToString());


                    //When a managed thread is created, the method that executes on the thread is represented by 
                    //a ThreadStart delegate or a ParameterizedThreadStart delegate that is passed to the Thread 
                    //constructor. The thread does not begin executing until the Thread.Start method is called. 
                    //The ThreadStart or ParameterizedThreadStart delegate is invoked on the thread, and execution 
                    //begins at the first line of the method represented by the delegate. In the case of the ParameterizedThreadStart 
                    //delegate, the object that is passed to the Start(Object) method is passed to the delegate. 

                    Thread clientThread = new Thread(new ParameterizedThreadStart(HandleClientComm));
                    clientThread.Name = "Client Thread " + client.Client.RemoteEndPoint.ToString();         
                    clientDictionary[client.Client.RemoteEndPoint] = new ClientThreadPair(client, clientThread);

                    try
                    {
                        sendWelcome();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Send Welcome Failed: " + ex.Message);
                    }

                    IsClientConnected = true;

                    clientThread.Start(client);
                }
                catch (SocketException socketEx)
                {
                    Console.WriteLine("Socket Closed from Another Thread: " + socketEx.ToString());
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ZServer: unhandled exception in ListenForClients: " + ex.Message);
                }
            }
            Console.WriteLine("ListenForClients exiting...");
        }

        /// <summary>
        /// Responsible for handling the comms of each TcpClient from their respective thread
        /// </summary>
        /// <param name="client"></param>
        private void HandleClientComm(object client)
        {
            TcpClient tcpClient = (TcpClient)client;
            EndPoint ep = tcpClient.Client.RemoteEndPoint;

            try
            {
                NetworkStream clientStream = tcpClient.GetStream();
           
                     
            while (tcpClient.Connected && !tokenSource.Token.IsCancellationRequested)
            {
                // slow the thread down and let the recv bytes threshold value do the work
                // when a read is performed.  no reason to tax the CPU.
                Thread.Sleep(50);

                // Read all available bytes up to the number set by the ReceiveBytesThreshold
                try
                {
                    if (tcpClient.Connected)
                    {
                        while(tcpClient.Available > ReceiveBytesThreshold && !tokenSource.Token.IsCancellationRequested)
                        {
                            if (clientStream.CanRead)
                            {

                                // read from ZClient's Buffer - blocks but that's ok because it is in its own thread. but in our case
                                // bytes are available to be read so it does not block.
                                byte[] bytes = new byte[tcpClient.Client.Available];
                                clientStream.Read(bytes, 0, tcpClient.Client.Available);

                                // update timestamp we last heard from server - used for heartbeat functionality
                                LastHeardFromClient= DateTime.UtcNow;
                                bytesQueue.Enqueue(bytes);

                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    //a socket error has occured
                    Console.WriteLine("ZServer: client handler error Has occurred: " + ex.ToString());
                    break;
                }
            } //reaper will cleanup         
            }
            catch (Exception ex)
            {
                Console.WriteLine("Get Stream Failure: " + ex.Message);
            }
        }

        private String formatLog(StringBuilder data)
        {
            String trim = data.ToString().Substring(1, data.Length - 1);
            return trim.Replace("??", "\n").Replace("?", "\n");
        }
       
        private void CleanupClient(ClientThreadPair pair)
        {
            EndPoint ep = pair.Client.Client.RemoteEndPoint;
            pair.Client.Close();
            
            if (pair.Thread != null && pair.Thread.ThreadState != ThreadState.Unstarted)
                pair.Thread.Join();

            Console.WriteLine("Removed client from: " + ep);
            
            IsClientConnected = false;
        }

        public String getLog()
        {
            return _robotLog;
        }


        public Boolean getIsClientConnected()
        {
            lock (_clientConnectedLock)
            {
                return _isClientConnected;
            }
        }


        void reaper_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            if (Monitor.TryEnter(reaperSync)) //re-entrant
            {
                try
                {
                    //scan dictionary and remove any clients that are disconnected
                    foreach (ClientThreadPair pair in clientDictionary.Values)
                    {
                        if (!pair.Client.Connected)
                        {
                            ClientThreadPair toRemove;
                            if (clientDictionary.TryRemove(pair.Client.Client.RemoteEndPoint, out toRemove))
                            {
                                CleanupClient(toRemove);
                            }
                        }
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine("ZServer: unhandled exception in Reaper: " + ex.Message);
                }
                finally
                {
                    Monitor.Exit(reaperSync);
                }
            }
        }

        public event EventHandler<DataArgs> PacketReceived;


        private void ParsingThread_DoWork()
        {
            while (!tokenSource.Token.IsCancellationRequested)
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
