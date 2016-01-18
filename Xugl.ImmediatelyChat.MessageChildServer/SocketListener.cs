using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageChildServer
{

    internal class SocketListener
    {
        private int m_maxConnnections;
        private int m_maxSize;
        const int opsToPreAlloc = 2;
        private SocketAsyncEventArgsPool m_readWritePool;
        private Socket mainServiceSocket;
        private  BufferManager m_bufferManager;

        private Semaphore m_maxNumberAcceptedClients;

        public SocketListener()
        {
            m_readWritePool = new SocketAsyncEventArgsPool();
            m_maxConnnections = 100;
            m_maxSize=1024;

            m_bufferManager = BufferManager.CreateBufferManager(m_maxConnnections * m_maxSize * opsToPreAlloc, m_maxSize);

            for (int i = 0; i < m_maxConnnections; i++)
            {
                SocketAsyncEventArgs socketAsyncEventArg = new SocketAsyncEventArgs();
                socketAsyncEventArg.UserToken = new AsyncUserToken();
                socketAsyncEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(IO_Completed);
                socketAsyncEventArg.SetBuffer(m_bufferManager.TakeBuffer(m_maxSize), 0, m_maxSize);

                m_readWritePool.Push(socketAsyncEventArg);
            }

            m_maxNumberAcceptedClients = new Semaphore(m_maxConnnections, m_maxConnnections);
        }

        private void IO_Completed(object sender, SocketAsyncEventArgs e)
        {
            // determine which type of operation just completed and call the associated handler
            switch (e.LastOperation)
            {
                case SocketAsyncOperation.Receive:
                    ProcessReceive(e);
                    break;
                case SocketAsyncOperation.Send:
                    ProcessSend(e);
                    break;
                default:
                    throw new ArgumentException("The last operation completed on the socket was not a receive or send");
            }

        }


        public void BeginService()
        {
            CommonVariables.LogTool.Log("Start MCS service:" + CommonVariables.MCSIP + ", Port:" + CommonVariables.MCSPort.ToString());
            IPAddress ip = IPAddress.Parse(CommonVariables.MCSIP);
            IPEndPoint ipe = new IPEndPoint(ip, CommonVariables.MCSPort);

            mainServiceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
            mainServiceSocket.Bind(ipe);
            mainServiceSocket.Listen(m_maxConnnections);

            StartAccept(null); 
        }


        public void StartAccept(SocketAsyncEventArgs acceptEventArg)
        {
            if (acceptEventArg == null)
            {
                acceptEventArg = new SocketAsyncEventArgs();
                acceptEventArg.Completed += new EventHandler<SocketAsyncEventArgs>(AcceptEventArg_Completed);
            }
            else
            {
                acceptEventArg.AcceptSocket = null;
            }

            m_maxNumberAcceptedClients.WaitOne();
            bool willRaiseEvent = mainServiceSocket.AcceptAsync(acceptEventArg);
            if (!willRaiseEvent)
            {
                ProcessAccept(acceptEventArg);
            }
        }


        private void AcceptEventArg_Completed(object sender,SocketAsyncEventArgs e)
        {
            ProcessAccept(e);
        }


        private void ProcessAccept(SocketAsyncEventArgs e)
        {
            //Interlocked.Increment(ref m_numConnectedSockets);
            //Console.WriteLine("Client connection accepted. There are {0} clients connected to the server",
            //    m_numConnectedSockets);

            // Get the socket for the accepted client connection and put it into the 
            //ReadEventArg object user token

            try
            {
                SocketAsyncEventArgs readEventArgs = m_readWritePool.Pop();
                ((AsyncUserToken)readEventArgs.UserToken).Socket = e.AcceptSocket;
                //readEventArgs.AcceptSocket = e.AcceptSocket;

                // As soon as the client is connected, post a receive to the connection
                bool willRaiseEvent = e.AcceptSocket.ReceiveAsync(readEventArgs);
                if (!willRaiseEvent)
                {
                    ProcessReceive(readEventArgs);
                }

                // Accept the next connection request
                StartAccept(e);
            }
            catch(Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
            }
            
        }


        private void ProcessReceive(SocketAsyncEventArgs e)
        {
            try
            {

                // check if the remote host closed the connection
                AsyncUserToken token = (AsyncUserToken)e.UserToken;
                if (e.BytesTransferred > 0 && e.SocketError == SocketError.Success)
                {
                    //increment the count of the total bytes receive by the server
                    //Interlocked.Add(ref m_totalBytesRead, e.BytesTransferred);
                    //Console.WriteLine("The server has read a total of {0} bytes", m_totalBytesRead);


                    string data = Encoding.UTF8.GetString(e.Buffer, e.Offset, e.BytesTransferred);
                    Byte[] sendBuffer = null;
                    if (token != null)
                    {
                        sendBuffer = Encoding.UTF8.GetBytes(token.GetReturnData(data));
                    }
                    else
                    {
                        sendBuffer = Encoding.UTF8.GetBytes("ok");
                    }


                    //echo the data received back to the client
                    e.SetBuffer(sendBuffer, 0, sendBuffer.Length);

                    bool willRaiseEvent = token.Socket.SendAsync(e);
                    if (!willRaiseEvent)
                    {
                        ProcessSend(e);
                    }

                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch(Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
            }
        }


        // This method is invoked when an asynchronous send operation completes.  
        // The method issues another receive on the socket to read any additional 
        // data sent from the client
        //
        // <param name="e"></param>
        private void ProcessSend(SocketAsyncEventArgs e)
        {
            try
            {
                if (e.SocketError == SocketError.Success)
                {
                    // done echoing data back to the client
                    AsyncUserToken token = (AsyncUserToken)e.UserToken;
                    // read the next block of data send from the client
                    bool willRaiseEvent = token.Socket.ReceiveAsync(e);
                    if (!willRaiseEvent)
                    {
                        ProcessReceive(e);
                    }
                }
                else
                {
                    CloseClientSocket(e);
                }
            }
            catch(Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
            }
        }


        private void CloseClientSocket(SocketAsyncEventArgs e)
        {
            AsyncUserToken token = e.UserToken as AsyncUserToken;

            // close the socket associated with the client
            try
            {
                token.Socket.Shutdown(SocketShutdown.Send);
            }
            // throws if client process has already closed
            catch (Exception) { }
            token.Socket.Close();

            // decrement the counter keeping track of the total number of clients connected to the server
            //Interlocked.Decrement(ref m_numConnectedSockets);
            //m_maxNumberAcceptedClients.Release();
            //Console.WriteLine("A client has been disconnected from the server. There are {0} clients connected to the server", m_numConnectedSockets);

            // Free the SocketAsyncEventArg so they can be reused by another client
            m_readWritePool.Push(e);
        }
        
    }


    internal class AsyncUserToken
    {
        private Socket m_Socket;

        public Socket Socket
        {
            get
            {
                return m_Socket;
            }
            set
            {
                m_Socket = value;
            }
        }

        public AsyncUserToken():this(null)
        {

        }

        public AsyncUserToken(Socket socket)
        {
            m_Socket = socket;
        }

        public string GetReturnData(string data)
        {
            if (string.IsNullOrEmpty(data))
            {
                return "";
            }

            if (data.StartsWith("VerifyAccount"))
            {
                string tempStr = data.Remove(0, 13);
                ClientStatusModel clientModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(tempStr);
                if (!string.IsNullOrEmpty(clientModel.ObjectID))
                {
                    CommonVariables.LogTool.Log("Account " + clientModel.ObjectID + " connect");
                    return "ok";
                }
            }


            if (data.StartsWith("VerifyMSG"))
            {
                string tempStr = data.Remove(0, 9);
                MsgRecordModel msgModel = CommonVariables.serializer.Deserialize<MsgRecordModel>(tempStr);
                if (!string.IsNullOrEmpty(msgModel.ObjectID))
                {
                    return "ok";
                }
            }

            if (data.StartsWith("VerifyGetMSG"))
            {
                string tempStr = data.Remove(0, 12);
                ClientStatusModel clientModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(tempStr);
                if (!string.IsNullOrEmpty(clientModel.ObjectID))
                {
                    return "No";
                }
            }
            return "";
            
        }


    }

    internal class SocketAsyncEventArgsPool
    {

        Stack<SocketAsyncEventArgs> _Pool;

        public SocketAsyncEventArgsPool()
        {

            _Pool = new Stack<SocketAsyncEventArgs>();

        }

        public void Push(SocketAsyncEventArgs item)
        {

            if (item == null) return;

            lock (_Pool)
            {

                _Pool.Push(item);

            }

        }

        public SocketAsyncEventArgs PopOrNew()
        {

            if (Count == 0)

                return new SocketAsyncEventArgs();

            return Pop();

        }

        public SocketAsyncEventArgs Pop()
        {

            lock (_Pool)
            {

                return _Pool.Pop();

            }

        }

        public int Count
        {

            get { return _Pool.Count; }

        }

        public void Clear()
        {

            while (Count > 0)
            {

                Pop().Dispose();

            }

        }

    } 

}
