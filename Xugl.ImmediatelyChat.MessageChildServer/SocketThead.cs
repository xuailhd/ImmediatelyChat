using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageChildServer
{
    // Get Client Status
    internal class SocketThead
    {
        private Socket clientSocket;
        private Thread childThread;

        public SocketThead(Socket clientSocket)
        {
            this.clientSocket = clientSocket;
            childThread = new Thread(new ThreadStart(Handle));
            childThread.Start();

        }

        private void Handle()
        {
            //ClientStatusModel clientStatusModel;
            byte[] bytmsg=new byte[1024];
            int bytes = clientSocket.Receive(bytmsg);
            string strMsg = Encoding.UTF8.GetString(bytmsg, 0, bytes);

            if (strMsg == "stop")
            {
                return;
            }

            if (strMsg.StartsWith("VerifyAccount"))
            {
                string tempStr = strMsg.Remove(0, 13);
                ClientStatusModel clientModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(tempStr);
                if (!string.IsNullOrEmpty(clientModel.ObjectID))
                {
                    CommonVariables.LogTool.Log("Account " + clientModel.ObjectID + " connect");
                    byte[] bytesSent = Encoding.UTF8.GetBytes("ok");
                    clientSocket.Send(bytesSent, bytesSent.Length, 0);
                }
            }


            if (strMsg.StartsWith("VerifyMSG"))
            {
                string tempStr = strMsg.Remove(0, 9);
                MsgRecordModel msgModel = CommonVariables.serializer.Deserialize<MsgRecordModel>(tempStr);
                if (!string.IsNullOrEmpty(msgModel.ObjectID))
                {
                    CommonVariables.LogTool.Log("Recived message from " + msgModel.ObjectID + " ,content:" + msgModel.Content);
                    byte[] bytesSent = Encoding.UTF8.GetBytes("ok");
                    clientSocket.Send(bytesSent, bytesSent.Length, 0);
                }
            }

            if (strMsg.StartsWith("VerifyGetMSG"))
            {
                string tempStr = strMsg.Remove(0, 12);
                ClientStatusModel clientModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(tempStr);
                if (!string.IsNullOrEmpty(clientModel.ObjectID))
                {
                    byte[] bytesSent = Encoding.UTF8.GetBytes("No");
                    clientSocket.Send(bytesSent, bytesSent.Length, 0);
                }
            }
            
            clientSocket.Close();
        }


        private void HandleNew()
        {
            //ClientStatusModel clientStatusModel;
            byte[] bytmsg = new byte[1024];
            clientSocket.ReceiveAsync()
            int bytes = clientSocket.Receive(bytmsg);
            string strMsg = Encoding.UTF8.GetString(bytmsg, 0, bytes);

            if (strMsg == "stop")
            {
                return;
            }

            if (strMsg.StartsWith("VerifyAccount"))
            {
                string tempStr = strMsg.Remove(0, 13);
                ClientStatusModel clientModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(tempStr);
                if (!string.IsNullOrEmpty(clientModel.ObjectID))
                {
                    CommonVariables.LogTool.Log("Account " + clientModel.ObjectID + " connect");
                    byte[] bytesSent = Encoding.UTF8.GetBytes("ok");
                    clientSocket.Send(bytesSent, bytesSent.Length, 0);
                }
            }


            if (strMsg.StartsWith("VerifyMSG"))
            {
                string tempStr = strMsg.Remove(0, 9);
                MsgRecordModel msgModel = CommonVariables.serializer.Deserialize<MsgRecordModel>(tempStr);
                if (!string.IsNullOrEmpty(msgModel.ObjectID))
                {
                    CommonVariables.LogTool.Log("Recived message from " + msgModel.ObjectID + " ,content:" + msgModel.Content);
                    byte[] bytesSent = Encoding.UTF8.GetBytes("ok");
                    clientSocket.Send(bytesSent, bytesSent.Length, 0);
                }
            }

            if (strMsg.StartsWith("VerifyGetMSG"))
            {
                string tempStr = strMsg.Remove(0, 12);
                ClientStatusModel clientModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(tempStr);
                if (!string.IsNullOrEmpty(clientModel.ObjectID))
                {
                    byte[] bytesSent = Encoding.UTF8.GetBytes("No");
                    clientSocket.Send(bytesSent, bytesSent.Length, 0);
                }
            }

            clientSocket.Close();
        }
    }
}
