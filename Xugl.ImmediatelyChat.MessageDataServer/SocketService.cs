﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Xugl.ImmediatelyChat.Common;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageDataServer
{
    class SocketService
    {
        private SocketListener socketListener;
        public void StartConnectMMS()
        {
            ThreadStart threadStart = new ThreadStart(ConnectPS);
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        /// <summary>
        /// 1.Get MMS information from PS 
        /// 2.send itself information to MMS
        /// 3.recive MMS's feedback, connnect sucess
        /// 4.begin MCS service
        /// </summary>
        //private void ConnectMMS()
        //{
        //    Socket tempSocket=null;
        //    try
        //    {
        //        byte[] bytesSent;
        //        JavaScriptSerializer serializer = new JavaScriptSerializer();
        //        //1.Get MMS information from PS 
        //        CommonVariables.LogTool.Log("Begin connect Portal Server:" + CommonVariables.PSIP + ", Port:" + CommonVariables.PSPort.ToString());
        //        string mmsinfo= PostServerInfo();
        //        MMSModel mmsModel= serializer.Deserialize<MMSModel>(mmsinfo);
        //        CommonVariables.MMSIP = mmsModel.MMS_IP;
        //        CommonVariables.MMSPort = mmsModel.MMS_Port;
        //        //UnBoxMMSMsg(mmsinfo);

        //        //2.send itself information to MMS

                
        //        MDSModel mdsModel = new MDSModel();
        //        mdsModel.MDS_ID = CommonVariables.MDS_ID;
        //        mdsModel.MDS_IP = CommonVariables.MDSIP;
        //        mdsModel.MDS_Port = CommonVariables.MDSPort;
        //        mdsModel.ArrangeChars = CommonVariables.ArrangeStr;

        //        bytesSent = Encoding.UTF8.GetBytes("MDS" + serializer.Serialize(mdsModel));
        //        IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.MMSIP), CommonVariables.MMSPort);
        //        tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
        //        tempSocket.Connect(ipe);
        //        CommonVariables.LogTool.Log("Begin send itself information message to MMS:" + bytesSent);
        //        tempSocket.Send(bytesSent, bytesSent.Length, 0);
        //        CommonVariables.LogTool.Log("Finish send message");

        //        //3.recive MMS's feedback, connnect sucess
        //        byte[] bytmsg = new byte[1024];
        //        int bytes = tempSocket.Receive(bytmsg);
        //        string strMsg = Encoding.UTF8.GetString(bytmsg, 0, bytes);
        //        CommonVariables.LogTool.Log("Recive feedback message:" + strMsg);
        //        tempSocket.Close();
        //        tempSocket = null;

        //        //4.begin MCS service
        //        StartMDSService();

        //    }
        //    catch (Exception ex)
        //    {
        //        if (tempSocket!=null)
        //        {
        //            tempSocket.Close();
        //            tempSocket = null;
        //        }
        //        CommonVariables.LogTool.Log(ex.Message);
        //    }
        //}


        private void ConnectPS()
        {
            try
            {
                //1.Post information to PS 
                CommonVariables.LogTool.Log("Begin connect Portal Server:" + CommonVariables.PSIP + ", Port:" + CommonVariables.PSPort.ToString());
                string PSMsg = PostServerInfo();

                if (!string.IsNullOrEmpty(PSMsg))
                {
                    //2.begin MDS service
                    CommonVariables.LogTool.Log("Wait Start Command");
                    CommonVariables.LogTool.Log("IP:" + CommonVariables.MDSIP + "    Port:" + CommonVariables.MDSPort.ToString());

                    socketListener = new SocketListener();
                    socketListener.BeginService();
                    return;
                }
                CommonVariables.LogTool.Log("Post PS Failed");
            }
            catch (Exception ex)
            {
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
                string arrangeStr= CommonVariables.OperateFile.GetConfig(CommonVariables.ConfigFilePath,"ArrangeStr");

                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("http://" + CommonVariables.PSIP
                    + ":" + CommonVariables.PSPort + "/AppServer/CollectMDS?ip=" + CommonVariables.MDSIP
                    + "&&port=" + CommonVariables.MDSPort + "&&arrangeStr=" + arrangeStr);

                myRequest.Method = "Get";
                webResponse = myRequest.GetResponse();
                stream = webResponse.GetResponseStream();
                sReader = new StreamReader(stream, Encoding.GetEncoding("utf-8"));
                string result = sReader.ReadToEnd();

                return result;
            }
            catch (Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message);
                return null;
            }
            finally
            {
                if (sReader != null) { sReader.Close(); }
                if (stream != null) { stream.Close(); }
                if (webResponse != null) { webResponse.Close(); }
            }
        }


        /// <summary>
        /// begin MCS service
        /// 1.wait Start Command
        /// 2.analysis command, save MDSs inforamtion
        /// 3.Start MCS service
        /// </summary>
        //private void StartMDSService()
        //{
        //    try
        //    {
        //        byte[] bytesRevice=new byte[1024];;

        //        //1.wait Start Command
        //        CommonVariables.LogTool.Log("Wait Start Command");
        //        CommonVariables.LogTool.Log("IP:" + CommonVariables.MDSIP + "    Port:" + CommonVariables.MDSPort.ToString());
        //        IPAddress ip = IPAddress.Parse(CommonVariables.MDSIP);
        //        IPEndPoint ipe = new IPEndPoint(ip, CommonVariables.MDSPort);

        //        mainServiceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
        //        mainServiceSocket.Bind(ipe);
        //        mainServiceSocket.Listen(0);

        //        IsGoOnListenMMS = true;
        //        IsGoOnRunning = true;

        //        try
        //        {
        //            string strMsg = "";
        //            while (IsGoOnListenMMS)
        //            {
        //                Socket clientSocket = mainServiceSocket.Accept();
        //                int bytes = clientSocket.Receive(bytesRevice);
        //                strMsg = Encoding.UTF8.GetString(bytesRevice, 0, bytes);

        //                if (strMsg.StartsWith("stop"))
        //                {
        //                    clientSocket.Close();
        //                    mainServiceSocket.Close();
        //                    clientSocket = null;
        //                    mainServiceSocket = null;
        //                    return;
        //                }

        //                if (strMsg.StartsWith(CommonFlag.F_MMSCallMDSStart))
        //                {
        //                    //2.analysis command, save MDSs inforamtion
        //                    SaveArrangeChars(strMsg);
        //                    break;
        //                }
        //            }
        //            mainServiceSocket.Close();
        //            //3.Start MCS service
        //            CommonVariables.LogTool.Log("Start MDS service:" + CommonVariables.MDSIP + ", Port:" + CommonVariables.MDSPort.ToString());
        //            SocketListener socketListener = new SocketListener();
        //            socketListener.BeginService();

                    
        //            //CommonVariables.LogTool.Log("stop MDS Server");
        //        }
        //        catch (SocketException ex)
        //        {
        //            CommonVariables.LogTool.Log("MDS Server Stoped:" + ex.Message);
        //        }
        //        catch (ObjectDisposedException ex)
        //        {
        //            CommonVariables.LogTool.Log("MDS Server Stoped:" + ex.Message);
        //        }

        //        //CommonVariables.
        //    }
        //    catch (Exception ex)
        //    {
        //        CommonVariables.LogTool.Log(ex.Message);
        //    }
        //}



        public void StopMDSService()
        {
            CommonVariables.MessageContorl.StopMainThread();
            //if (IsGoOnRunning == true)
            //{
            //    IsGoOnRunning = false;
            //    IsGoOnListenMMS = false;
            //    try
            //    {
            //        byte[] bytesSent;
            //        bytesSent = Encoding.UTF8.GetBytes("stop");
            //        IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.MDSIP), CommonVariables.MDSPort);
            //        Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //        tempSocket.Connect(ipe);
            //        tempSocket.Send(bytesSent, bytesSent.Length, 0);
            //        tempSocket.Close();
            //        tempSocket = null;

            //        bytesSent = Encoding.UTF8.GetBytes("stopMDS" + CommonVariables.MDS_ID);
            //        ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.MMSIP), CommonVariables.MMSPort);
            //        tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

            //        tempSocket.Connect(ipe);
            //        tempSocket.Send(bytesSent, bytesSent.Length, 0);
            //        tempSocket.Close();
            //        tempSocket = null;
            //    }
            //    catch (Exception ex)
            //    {

            //    }
            //    //serviceSocket = null;
            //}
        }
    }
}
