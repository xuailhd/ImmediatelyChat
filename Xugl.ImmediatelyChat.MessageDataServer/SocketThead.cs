using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageDataServer
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
            MCSModel tempMCSModel=null;
            MDSModel tempMDSModel=null;

            byte[] bytmsg=new byte[1024];
            int bytes = clientSocket.Receive(bytmsg);
            string strMsg = Encoding.UTF8.GetString(bytmsg, 0, bytes);

            if (strMsg == "stop")
            {
                return;
            }

            string signStr = strMsg.Substring(0, 3);



            if (tempMCSModel!=null)
            {
                CommonVariables.LogTool.Log("Message Child Server " + tempMCSModel.MCS_ID + " connect");
            }

            if (tempMDSModel!=null)
            {
                CommonVariables.LogTool.Log("Message Data Server " + tempMDSModel.MDS_ID + " connect");
            }

            clientSocket.Close();
        }

    }
}
