using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageChildServer
{
    class SocketService
    {
        private Socket mainServiceSocket;

        private bool IsGoOnListenMMS;
        private bool IsGoOnRunning;


        public void StartConnectMMS()
        {
            ThreadStart threadStart = new ThreadStart(ConnectMMS);
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        /// <summary>
        /// 1.Get MMS information from PS 
        /// 2.send itself information to MMS
        /// 3.recive MMS's feedback, connnect sucess
        /// 4.begin MCS service
        /// </summary>
        private void ConnectMMS()
        {
            Socket tempSocket=null;
            try
            {
                byte[] bytesSent;
                //1.Get MMS information from PS 
                CommonVariables.LogTool.Log("Begin connect Portal Server:" + CommonVariables.PSIP + ", Port:" + CommonVariables.PSPort.ToString());
                string mmsinfo= PostServerInfo();
                MMSModel mmsModel = CommonVariables.serializer.Deserialize<MMSModel>(mmsinfo);
                CommonVariables.MMSIP = mmsModel.MMS_IP;
                CommonVariables.MMSPort = mmsModel.MMS_Port;
                //UnBoxMMSMsg(mmsinfo);

                //2.send itself information to MMS
                //bytesSent = Encoding.UTF8.GetBytes(BoxMCSMsg());
                MCSModel mcsModel=new MCSModel();
                mcsModel.MCS_ID=CommonVariables.MCS_ID;
                mcsModel.MCS_IP=CommonVariables.MCSIP;
                mcsModel.MCS_Port = CommonVariables.MCSPort;

                bytesSent = Encoding.UTF8.GetBytes("MCS" + CommonVariables.serializer.Serialize(mcsModel));
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.MMSIP), CommonVariables.MMSPort);
                tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                tempSocket.Connect(ipe);
                CommonVariables.LogTool.Log("Begin send itself information message to MMS:" + bytesSent);
                tempSocket.Send(bytesSent, bytesSent.Length, 0);
                CommonVariables.LogTool.Log("Finish send message");

                //3.recive MMS's feedback, connnect sucess
                byte[] bytmsg = new byte[1024];
                int bytes = tempSocket.Receive(bytmsg);
                string strMsg = Encoding.UTF8.GetString(bytmsg, 0, bytes);
                CommonVariables.LogTool.Log("Recive feedback message:" + strMsg);
                tempSocket.Close();
                tempSocket = null;

                //4.begin MCS service
                StartMCSServiceAsync();

            }
            catch (Exception ex)
            {
                if (tempSocket!=null)
                {
                    tempSocket.Close();
                    tempSocket = null;
                }
                CommonVariables.LogTool.Log(ex.Message);
            }
        }

        /// <summary>
        /// Get MMS information from PS 
        /// </summary>
        /// <returns></returns>
        private string PostServerInfo()
        {
            StreamReader sReader = null;
            WebResponse webResponse = null;
            Stream stream = null;
            try
            {
                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("http://" + CommonVariables.PSIP
                    + ":" + CommonVariables.PSPort + "/AppServer/FindMMS");

                myRequest.Method = "Get";
                webResponse = myRequest.GetResponse();
                stream = webResponse.GetResponseStream();
                sReader = new StreamReader(stream, Encoding.GetEncoding("utf-8"));
                string result = sReader.ReadToEnd();

                return result;
            }
            catch (Exception ex)
            {
                return null;
            }
            finally
            {
                if (sReader != null) { sReader.Close(); }
                if (stream != null) { stream.Close(); }
                if (webResponse != null) { webResponse.Close(); }
            }
        }

        //private string BoxMCSMsg()
        //{
        //    string msg = "MCS" + CommonFlag.F_IP + CommonVariables.MCSIP + ";" + CommonFlag.F_Port + CommonVariables.MCSPort.ToString() +
        //        ";" + CommonFlag.F_ID + CommonVariables.MCS_ID + ";" + CommonFlag.F_ArrangeChars + CommonVariables.ArrangeChars + ";";
        //    return msg;
        //}

        //private void UnBoxMMSMsg(string msg)
        //{
        //    string tempMsg = msg;
        //    if (tempMsg.IndexOf(CommonFlag.F_IP) >= 0)
        //    {
        //        CommonVariables.MMSIP = tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_IP) + CommonFlag.F_IP.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_IP)) - tempMsg.IndexOf(CommonFlag.F_IP) - CommonFlag.F_IP.Length);
        //        tempMsg = tempMsg.Replace(CommonFlag.F_IP + CommonVariables.MMSIP + ";", "");
        //    }

        //    if (tempMsg.IndexOf(CommonFlag.F_Port) >= 0)
        //    {
        //        CommonVariables.MMSPort = Convert.ToInt32(tempMsg.Substring(tempMsg.IndexOf(CommonFlag.F_Port) + CommonFlag.F_Port.Length, tempMsg.IndexOf(";", tempMsg.IndexOf(CommonFlag.F_Port)) - tempMsg.IndexOf(CommonFlag.F_Port) - CommonFlag.F_Port.Length));
        //        tempMsg = tempMsg.Replace(CommonFlag.F_Port + CommonVariables.MMSPort.ToString() + ";", "");
        //    }
        //}

        /// <summary>
        /// begin MCS service
        /// 1.wait Start Command
        /// 2.analysis command, save MDSs inforamtion
        /// 3.Start MCS service
        /// </summary>
        //private void StartMCSService()
        //{
        //    try
        //    {
        //        byte[] bytesRevice=new byte[1024];;

        //        //1.wait Start Command
        //        CommonVariables.LogTool.Log("Wait Start Command");
        //        CommonVariables.LogTool.Log("IP:" + CommonVariables.MCSIP + "    Port:" + CommonVariables.MCSPort.ToString());
        //        IPAddress ip = IPAddress.Parse(CommonVariables.MCSIP);
        //        IPEndPoint ipe = new IPEndPoint(ip, CommonVariables.MCSPort);

        //        mainServiceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //        mainServiceSocket.Bind(ipe);
        //        mainServiceSocket.Listen(0);

        //        IsGoOnListenMMS = true;
        //        IsGoOnRunning = true;

        //        try
        //        {
        //            string strMsg="";
        //            while (IsGoOnListenMMS)
        //            {
        //                Socket clientSocket = mainServiceSocket.Accept();
        //                int bytes = clientSocket.Receive(bytesRevice);
        //                strMsg = "";
        //                strMsg = Encoding.UTF8.GetString(bytesRevice, 0, bytes);

        //                if (strMsg.StartsWith("stop"))
        //                {
        //                    clientSocket.Close();
        //                    mainServiceSocket.Close();
        //                    clientSocket = null;
        //                    mainServiceSocket = null;
        //                    return;
        //                }

        //                if (strMsg.StartsWith(CommonFlag.F_MMSCallMCSStart))
        //                {
        //                    //2.analysis command, save MDSs inforamtion
        //                    SaveMDSs(strMsg);
        //                    break;
        //                }
        //            }

        //            //3.Start MCS service
        //            CommonVariables.LogTool.Log("Start MCS service:" + CommonVariables.MCSIP + ", Port:" + CommonVariables.MCSPort.ToString());
        //            while (IsGoOnRunning)
        //            {
        //                Socket clientSocket = mainServiceSocket.Accept();
        //                SocketThead socketThead = new SocketThead(clientSocket);
        //            }

        //            mainServiceSocket.Close();
        //            CommonVariables.LogTool.Log("stop MCS Server");
        //        }
        //        catch (SocketException ex)
        //        {
        //            CommonVariables.LogTool.Log("MCS Server Stoped:" + ex.Message);
        //        }
        //        catch (ObjectDisposedException ex)
        //        {
        //            CommonVariables.LogTool.Log("MCS Server Stoped:" + ex.Message);
        //        }

        //        //CommonVariables.
        //    }
        //    catch (Exception ex)
        //    {
        //        CommonVariables.LogTool.Log(ex.Message);
        //    }
        //}


        /// <summary>
        /// begin MCS service
        /// 1.wait Start Command
        /// 2.analysis command, save MDSs inforamtion
        /// 3.Start MCS service
        /// </summary>
        private void StartMCSServiceAsync()
        {
            try
            {
                byte[] bytesRevice = new byte[1024]; ;

                //1.wait Start Command
                CommonVariables.LogTool.Log("Wait Start Command");
                CommonVariables.LogTool.Log("IP:" + CommonVariables.MCSIP + "    Port:" + CommonVariables.MCSPort.ToString());
                IPAddress ip = IPAddress.Parse(CommonVariables.MCSIP);
                IPEndPoint ipe = new IPEndPoint(ip, CommonVariables.MCSPort);

                mainServiceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mainServiceSocket.Bind(ipe);
                mainServiceSocket.Listen(0);

                IsGoOnListenMMS = true;
                IsGoOnRunning = true;

                try
                {
                    string strMsg = "";
                    while (IsGoOnListenMMS)
                    {
                        Socket clientSocket = mainServiceSocket.Accept();
                        int bytes = clientSocket.Receive(bytesRevice);
                        strMsg = "";
                        strMsg = Encoding.UTF8.GetString(bytesRevice, 0, bytes);

                        if (strMsg.StartsWith("stop"))
                        {
                            clientSocket.Close();
                            mainServiceSocket.Close();
                            clientSocket = null;
                            mainServiceSocket = null;
                            return;
                        }

                        if (strMsg.StartsWith(CommonFlag.F_MMSCallMCSStart))
                        {
                            //2.analysis command, save MDSs inforamtion
                            SaveMDSs(strMsg);
                            break;
                        }
                    }
                    mainServiceSocket.Close();
                    mainServiceSocket = null;


                    //3.Start MCS service

                    SocketListener socketListener = new SocketListener();
                    socketListener.BeginService();
                    
                    CommonVariables.LogTool.Log("stop MCS Server");
                }
                catch (SocketException ex)
                {
                    CommonVariables.LogTool.Log("MCS Server Stoped:" + ex.Message);
                }
                catch (ObjectDisposedException ex)
                {
                    CommonVariables.LogTool.Log("MCS Server Stoped:" + ex.Message);
                }

                //CommonVariables.
            }
            catch (Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message);
            }
        }

        /// <summary>
        /// analysis command, save MDSs inforamtion
        /// </summary>
        /// <param name="strMsg"></param>
        private void SaveMDSs(string strMsg)
        {
            //"MCS start"
            string tempMsg = strMsg.Remove(0,9);


            IList<MDSModel> mdsModels=CommonVariables.serializer.Deserialize<IList<MDSModel>>(tempMsg);

            foreach(MDSModel mdsModel in mdsModels)
            {
                CommonVariables.AddMDS(mdsModel);
            }   
        }

        public void StopMCSService()
        {

            if (IsGoOnRunning == true)
            {
                IsGoOnRunning = false;
                IsGoOnListenMMS = false;
                try
                {
                    byte[] bytesSent;
                    bytesSent = Encoding.UTF8.GetBytes("stop");
                    IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.MCSIP), CommonVariables.MCSPort);
                    Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);
                    tempSocket.Send(bytesSent, bytesSent.Length, 0);
                    tempSocket.Close();
                    tempSocket = null;

                    bytesSent = Encoding.UTF8.GetBytes("stopMCS" + CommonVariables.MCS_ID);
                    ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.MMSIP), CommonVariables.MMSPort);
                    tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);
                    tempSocket.Send(bytesSent, bytesSent.Length, 0);
                    tempSocket.Close();
                    tempSocket = null;
                }
                catch (Exception ex)
                {

                }
                //serviceSocket = null;
            }
        }
    }
}
