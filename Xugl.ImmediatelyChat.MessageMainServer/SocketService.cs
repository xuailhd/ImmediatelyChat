using System;
using System.Collections.Generic;
using System.IO;
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
    class SocketService
    {
        private Socket mainServiceSocket;
        private bool IsConnectGoOnRuning;

        public void StartMMSService()
        {
            IsConnectGoOnRuning = true;

            ThreadStart threadStart = new ThreadStart(MMSService);
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        private void MMSService()
        {
            CommonVariables.LogTool.Log("Begin post server information to portal server " + CommonVariables.PSIP);
            string PSMsg = PostServerInfo();
            CommonVariables.LogTool.Log(PSMsg);
            if (string.IsNullOrEmpty(PSMsg))
            {
                CommonVariables.LogTool.Log("Can not connect portal server " + CommonVariables.PSIP);
                return;
            }
            try
            {

                IPAddress ip = IPAddress.Parse(CommonVariables.MMSIP);
                IPEndPoint ipe = new IPEndPoint(ip, CommonVariables.MMSPort);

                mainServiceSocket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                mainServiceSocket.Bind(ipe);
                mainServiceSocket.Listen(0);
                CommonVariables.LogTool.Log("Start Message Main Server:" + CommonVariables.MMSIP + ", Port:" + CommonVariables.MMSPort.ToString());

            
                while (IsConnectGoOnRuning)
                {
                    Socket clientSocket = mainServiceSocket.Accept();
                    SocketThead socketThead = new SocketThead(clientSocket);
                }
                mainServiceSocket.Close();
                CommonVariables.LogTool.Log("Stop Message Main Server");
            }
            catch (SocketException ex)
            {
                CommonVariables.LogTool.Log("Message main server be stoped, error:" + ex.Message);
            }
            catch (ObjectDisposedException ex)
            {
                CommonVariables.LogTool.Log("Message main Server be stoped, error:" + ex.Message);
            }
        }

        private string PostServerInfo()
        {
            StreamReader sReader = null;
            WebResponse webResponse = null;
            Stream stream=null;
            try
            {

                HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create("http://" + CommonVariables.PSIP
                    + ":" + CommonVariables.PSPort + "/AppServer/CollectMMS?ip=" + CommonVariables.MMSIP + "&&port=" + CommonVariables.MMSPort);

                myRequest.Method = "Get";

                webResponse = myRequest.GetResponse();

                stream=webResponse.GetResponseStream();

                sReader = new StreamReader(stream, Encoding.GetEncoding("utf-8"));

                string result = sReader.ReadToEnd();
                
                
                return result;
            }
            catch(Exception ex)
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

        public void StartService()
        {
            ArrangeChar();
            SendStartCommand();

            CommonVariables.IsBeginMessageService = true;

            CommonVariables.LogTool.Log("Begin accept messages");
        }

        private void SendStartCommand()
        {
            byte[] bytesSent;
            string cmdOrder = "";
            string mdssStr = "";
            JavaScriptSerializer serializer = new JavaScriptSerializer();
            mdssStr= serializer.Serialize(CommonVariables.GetMDSs.Values.ToList());


            foreach (string mcs_id in CommonVariables.GetMCSs.Keys)
            {
                bytesSent = Encoding.UTF8.GetBytes("MCS start" + mdssStr);
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.GetMCSs[mcs_id].MCS_IP), CommonVariables.GetMCSs[mcs_id].MCS_Port);
                Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                tempSocket.Connect(ipe);
                tempSocket.Send(bytesSent, bytesSent.Length, 0);
                tempSocket.Close();
            }

            foreach (string mds_id in CommonVariables.GetMDSs.Keys)
            {
                cmdOrder = "MDS start";
                cmdOrder = cmdOrder + CommonFlag.F_ArrangeChars;
                cmdOrder = cmdOrder + CommonVariables.GetMDSs[mds_id].ArrangeChars;
                cmdOrder = cmdOrder + ";";

                bytesSent = Encoding.UTF8.GetBytes(cmdOrder);
                IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.GetMDSs[mds_id].MDS_IP), CommonVariables.GetMDSs[mds_id].MDS_Port);
                Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                tempSocket.Connect(ipe);
                tempSocket.Send(bytesSent, bytesSent.Length, 0);
                tempSocket.Close();
            }
            
        }

        private void ArrangeChar()
        {
            char[] chars ={'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s',
                             't','u','v','w','x','y','z','0','1','2','3','4','5','6','7','8','9'};

            int mcsCount = CommonVariables.GetMCSs.Count;
            int mdsCount = CommonVariables.GetMDSs.Count;
            IList<char> OldChars = new List<char>();

            //not to handle duplicate chars this time
            //IDictionary<string,char[]> mcsChars=new Dictionary<string,char[]>();
            //IDictionary<string,char> conflictChars=new Dictionary<string,char>();

            //arrange MCSs
            foreach (string mcs_id in CommonVariables.GetMCSs.Keys)
            {
                if(!string.IsNullOrEmpty(CommonVariables.GetMCSs[mcs_id].ArrangeChars))
                {
                    List<char> tempChars = CommonVariables.GetMCSs[mcs_id].ArrangeChars.ToCharArray().ToList<char>();
                    foreach (char tempChar in tempChars)
                    {
                        OldChars.Add(tempChar);
                    }
                }
                
            }

            var tempquery = from aa in chars
                            join bb in OldChars on aa equals bb into cc
                            from temp in cc.DefaultIfEmpty()
                            where temp.Equals(char.MinValue)
                            select aa;

                                        //where string.IsNullOrEmpty(temp.ToString())

            IList<char> availableChars = tempquery.ToList();

            int eachServiceCount = chars.Length / mcsCount;
            int count = 0;

            foreach (string mcs_id in CommonVariables.GetMCSs.Keys)
            {
                count++;
                if (count < mcsCount)
                {
                    int tmpi = 0;
                    if (string.IsNullOrEmpty( CommonVariables.GetMCSs[mcs_id].ArrangeChars))
                    {
                        tmpi = eachServiceCount;
                    }
                    else
                    {
                        tmpi = eachServiceCount - CommonVariables.GetMCSs[mcs_id].ArrangeChars.Length;
                    }
                    
                    while (tmpi > 0)
                    {
                        CommonVariables.GetMCSs[mcs_id].ArrangeChars = CommonVariables.GetMCSs[mcs_id].ArrangeChars + availableChars[0].ToString();
                        availableChars.Remove(availableChars[0]);
                        tmpi--;
                    }
                }
                else
                {
                    foreach (char availableChar in availableChars)
                    {
                        CommonVariables.GetMCSs[mcs_id].ArrangeChars = CommonVariables.GetMCSs[mcs_id].ArrangeChars + availableChar;
                    }
                }
            }


            //arrange MDSs
            OldChars = new List<char>();
            foreach (string mds_id in CommonVariables.GetMDSs.Keys)
            {
                if (!string.IsNullOrEmpty(CommonVariables.GetMDSs[mds_id].ArrangeChars))
                {
                    List<char> tempChars = CommonVariables.GetMDSs[mds_id].ArrangeChars.ToCharArray().ToList<char>();
                    foreach (char tempChar in tempChars)
                    {
                        OldChars.Add(tempChar);
                    }
                }
            }

            tempquery = from aa in chars
                        join bb in OldChars on aa equals bb into cc
                        from temp in cc.DefaultIfEmpty()
                        where temp.Equals(char.MinValue)
                        select aa;

            availableChars = tempquery.ToList();

            eachServiceCount = chars.Length / mcsCount;
            count = 0;

            foreach (string mds_id in CommonVariables.GetMDSs.Keys)
            {
                count++;
                if (count < mcsCount)
                {
                    int tmpi = 0;
                    if (string.IsNullOrEmpty(CommonVariables.GetMDSs[mds_id].ArrangeChars))
                    {
                        tmpi = eachServiceCount;
                    }
                    else
                    {
                        tmpi = eachServiceCount - CommonVariables.GetMDSs[mds_id].ArrangeChars.Length;
                    }
                    
                    while (tmpi > 0)
                    {
                        CommonVariables.GetMDSs[mds_id].ArrangeChars =CommonVariables.GetMDSs[mds_id].ArrangeChars + availableChars[0].ToString();
                        availableChars.Remove(availableChars[0]);
                        tmpi--;
                    }
                }
                else
                {
                    foreach (char availableChar in availableChars)
                    {
                        CommonVariables.GetMDSs[mds_id].ArrangeChars = CommonVariables.GetMDSs[mds_id].ArrangeChars + availableChars[0].ToString();
                    }
                }
            }
        }

        public void StopMMSService()
        {

            if (IsConnectGoOnRuning == true)
            {
                IsConnectGoOnRuning = false;
                try
                {
                    byte[] bytesSent;
                    bytesSent = Encoding.UTF8.GetBytes("stop");
                    IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(CommonVariables.MMSIP), CommonVariables.MMSPort);
                    Socket tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);
                    tempSocket.Send(bytesSent, bytesSent.Length, 0);
                    tempSocket.Close();
                }
                catch (Exception ex)
                {

                }
            }
        }
    }
}
