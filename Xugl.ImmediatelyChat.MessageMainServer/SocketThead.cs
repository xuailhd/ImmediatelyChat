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


namespace Xugl.ImmediatelyChat.MessageMainServer
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

            try
            {
                byte[] bytmsg = new byte[1024];
                int bytes = clientSocket.Receive(bytmsg);
                string strMsg = Encoding.UTF8.GetString(bytmsg, 0, bytes);
                JavaScriptSerializer serializer = new JavaScriptSerializer();
                if (strMsg == "stop")
                {
                    return;
                }

                if (strMsg.StartsWith("MCS"))
                {
                    //tempMCSModel = UnboxMCSMsg(strMsg);
                    
                    tempMCSModel = serializer.Deserialize<MCSModel>(strMsg.Remove(0, 3));
                    CommonVariables.AddMCS(tempMCSModel);
                    bytmsg = Encoding.UTF8.GetBytes("ok");
                    clientSocket.Send(bytmsg,bytmsg.Length ,0);
                    CommonVariables.LogTool.Log("Message Child Server " + tempMCSModel.MCS_ID + " connect");
                }

                if (strMsg.StartsWith("MDS"))
                {
                    //tempMDSModel = UnboxMDSMsg(strMsg);

                    tempMDSModel = serializer.Deserialize<MDSModel>(strMsg.Remove(0, 3));
                    CommonVariables.AddMDS(tempMDSModel);
                    bytmsg = Encoding.UTF8.GetBytes("ok");
                    clientSocket.Send(bytmsg, bytmsg.Length, 0);
                    CommonVariables.LogTool.Log("Message Data Server " + tempMDSModel.MDS_ID + " connect");
                }

                if (strMsg.StartsWith("stopMCS"))
                {
                    CommonVariables.RemoveMCS(strMsg.Replace("stopMCS", ""));
                    CommonVariables.LogTool.Log("Message Child Server " + strMsg.Replace("stopMCS", "") + " disconnect");
                }

                if (strMsg.StartsWith("stopMDS"))
                {
                    CommonVariables.RemoveMCS(strMsg.Replace("stopMDS", ""));
                    CommonVariables.LogTool.Log("Message Data Server " + strMsg.Replace("stopMDS", "") + " disconnect");
                }

                //UA
                if (strMsg.StartsWith("Verify"))
                {
                    ClientStatusModel clientStatusModel = serializer.Deserialize<ClientStatusModel>(strMsg.Remove(0, 6));
                    //Find MCS
                    foreach(string mcs_id in CommonVariables.GetMCSs.Keys)
                    {
                        if (CommonVariables.GetMCSs[mcs_id].ArrangeChars.Contains(clientStatusModel.ObjectID.Substring(0,1)))
                        {
                            clientStatusModel.MCS_IP = CommonVariables.GetMCSs[mcs_id].MCS_IP;
                            clientStatusModel.MCS_Port = CommonVariables.GetMCSs[mcs_id].MCS_Port;
                        }
                    }

                    //Send MCS
                    byte[] bytesSent = Encoding.UTF8.GetBytes(serializer.Serialize(clientStatusModel));
                    clientSocket.Send(bytesSent, bytesSent.Length, 0);
                }

                clientSocket.Close();
                clientSocket = null;
            }
            catch(Exception ex)
            {
                if(clientSocket!=null)
                {
                    clientSocket.Close();
                    clientSocket = null;
                }
            }

        }


        //private MCSModel UnboxMCSMsg(string msg)
        //{
        //    MCSModel mcsModel = new MCSModel();

        //    string tempMsg = msg;
        //    if (tempMsg.IndexOf(CommonFlag.F_IP)>=0)
        //    {
        //        mcsModel.MCS_IP = tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_IP) + CommonFlag.F_IP.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_IP)) - tempMsg.IndexOf(CommonFlag.F_IP) - CommonFlag.F_IP.Length);
        //        tempMsg = tempMsg.Replace(CommonFlag.F_IP + mcsModel.MCS_IP + ";", "");
        //    }

        //    if(tempMsg.IndexOf(CommonFlag.F_Port)>=0)
        //    {
        //        mcsModel.MCS_Port = Convert.ToInt32(tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_Port) + CommonFlag.F_Port.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_Port)) - tempMsg.IndexOf(CommonFlag.F_Port) - CommonFlag.F_Port.Length));
        //        tempMsg = tempMsg.Replace(CommonFlag.F_Port + mcsModel.MCS_Port.ToString() + ";", "");
        //    }

        //    if(tempMsg.IndexOf(CommonFlag.F_ID)>=0)
        //    {
        //        mcsModel.MCS_ID = tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_ID) + CommonFlag.F_ID.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_ID)) - tempMsg.IndexOf(CommonFlag.F_ID) - CommonFlag.F_ID.Length);
        //        tempMsg = tempMsg.Replace(CommonFlag.F_ID + mcsModel.MCS_ID + ";", "");
        //    }

        //    if(tempMsg.IndexOf(CommonFlag.F_ArrangeChars)>=0)
        //    {
        //        mcsModel.ArrangeChars = tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_ArrangeChars) + CommonFlag.F_ArrangeChars.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_ArrangeChars)) - tempMsg.IndexOf(CommonFlag.F_ArrangeChars) - CommonFlag.F_ArrangeChars.Length).ToCharArray().ToList();
        //        tempMsg = tempMsg.Replace(CommonFlag.F_ArrangeChars + mcsModel.ArrangeChars.ToString() + ";", "");
        //    }

        //    return mcsModel;
        //}

        //private MDSModel UnboxMDSMsg(string msg)
        //{
        //    MDSModel mdsModel = new MDSModel();

        //    string tempMsg = msg;
        //    if (tempMsg.IndexOf(CommonFlag.F_IP) >= 0)
        //    {
        //        mdsModel.MDS_IP = tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_IP) + CommonFlag.F_IP.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_IP)) - tempMsg.IndexOf(CommonFlag.F_IP) - CommonFlag.F_IP.Length);
        //        tempMsg = tempMsg.Replace(CommonFlag.F_IP + mdsModel.MDS_IP + ";", "");
        //    }

        //    if (tempMsg.IndexOf(CommonFlag.F_Port) >= 0)
        //    {
        //        mdsModel.MDS_Port = Convert.ToInt32(tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_Port) + CommonFlag.F_Port.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_Port)) - tempMsg.IndexOf(CommonFlag.F_Port) - CommonFlag.F_Port.Length));
        //        tempMsg = tempMsg.Replace(CommonFlag.F_Port + mdsModel.MDS_Port.ToString() + ";", "");
        //    }

        //    if (tempMsg.IndexOf(CommonFlag.F_ID) >= 0)
        //    {
        //        mdsModel.MDS_ID = tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_ID) + CommonFlag.F_ID.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_ID)) - tempMsg.IndexOf(CommonFlag.F_ID) - CommonFlag.F_ID.Length);
        //        tempMsg = tempMsg.Replace(CommonFlag.F_ID + mdsModel.MDS_ID + ";", "");
        //    }

        //    if (tempMsg.IndexOf(CommonFlag.F_ArrangeChars) >= 0)
        //    {
        //        mdsModel.ArrangeChars = tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_ArrangeChars) + CommonFlag.F_ArrangeChars.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_ArrangeChars)) - tempMsg.IndexOf(CommonFlag.F_ArrangeChars) - CommonFlag.F_ArrangeChars.Length).ToCharArray().ToList();
        //        tempMsg = tempMsg.Replace(CommonFlag.F_ArrangeChars + mdsModel.ArrangeChars.ToString() + ";", "");
        //    }
        //    return mdsModel;
        //}

        //private ClientStatusModel UnboxMsg(string msg)
        //{
        //    ClientStatusModel clientStatusModel = new ClientStatusModel();
        //    string tempMsg = msg;

        //    if (tempMsg.IndexOf(CommonFlag.F_ObjectID) >= 0)
        //    {
        //        clientStatusModel.ObjectID = tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_ObjectID) + CommonFlag.F_ObjectID.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_ObjectID)) - tempMsg.IndexOf(CommonFlag.F_ObjectID) - CommonFlag.F_ObjectID.Length);
        //        tempMsg = tempMsg.Replace(CommonFlag.F_ObjectID + clientStatusModel.ObjectID.ToString(), "");
        //    }

        //    return clientStatusModel;
        //}
    }
}
