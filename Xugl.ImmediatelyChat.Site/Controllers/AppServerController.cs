using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xugl.ImmediatelyChat.Site.Models;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Model;
using System.Web.Script.Serialization;
using System.Net.Sockets;
using System.Net;
using System.Text;
using Xugl.ImmediatelyChat.Common;

namespace Xugl.ImmediatelyChat.Site.Controllers
{
    public class AppServerController : Controller
    {
        private readonly ICacheManage cacheManage;
        private readonly IRepository<MMSServer> mmsServerRepository;
        public AppServerController(ICacheManage cacheManage, IRepository<MMSServer> mmsServerRepository)
        {
            this.cacheManage = cacheManage;
            this.mmsServerRepository = mmsServerRepository;
        }

        //
        // GET: /AppServer/

        public ActionResult CollectMMS(string ip,int port,string arrangeStr)
        {
            MMSServer mmsServer = new MMSServer();
            mmsServer.ArrangeStr = arrangeStr;
            mmsServer.MMS_IP = ip;
            mmsServer.MMS_Port = port;

            IList<MMSServer> mmsServers = cacheManage.GetCache<IList<MMSServer>>("MMSServers");

            if (mmsServers==null)
            {
                mmsServers = new List<MMSServer>();
            }

            mmsServers.Add(mmsServer);

            cacheManage.AddCache<IList<MMSServer>>("MMSServers", mmsServers);

            return Json("ok",JsonRequestBehavior.AllowGet);
        }


        public ActionResult CollectMCS(string ip, int port, string arrangeStr)
        {
            MCSServer mcsServer = new MCSServer();
            mcsServer.ArrangeStr = arrangeStr;
            mcsServer.MCS_IP = ip;
            mcsServer.MCS_Port = port;

            IList<MCSServer> mcsServers = cacheManage.GetCache<IList<MCSServer>>("MCSServers");

            if (mcsServers == null)
            {
                mcsServers = new List<MCSServer>();
            }

            mcsServers.Add(mcsServer);

            cacheManage.AddCache<IList<MCSServer>>("MCSServers", mcsServers);

            return Json("ok", JsonRequestBehavior.AllowGet);
        }

        public ActionResult CollectMDS(string ip, int port, string arrangeStr)
        {
            MDSServer mdsServer = new MDSServer();
            mdsServer.ArrangeStr = arrangeStr;
            mdsServer.MDS_IP = ip;
            mdsServer.MDS_Port = port;

            IList<MDSServer> mdsServers = cacheManage.GetCache<IList<MDSServer>>("MDSServers");

            if (mdsServers == null)
            {
                mdsServers = new List<MDSServer>();
            }

            mdsServers.Add(mdsServer);

            cacheManage.AddCache<IList<MDSServer>>("MDSServers", mdsServers);

            return Json("ok", JsonRequestBehavior.AllowGet);
        }


        public ActionResult StartServer(string password)
        {
            string returnstr=ArrangeChar();

            if(!string.IsNullOrEmpty(returnstr))
            {
                return View("StartFailed", returnstr);
            }
            returnstr=SendStartCommand();
            if(!string.IsNullOrEmpty(returnstr))
            {
                return View("StartFailed", returnstr);
            }

            ServersModel serversModel = new ServersModel();
            serversModel.MMSServers = cacheManage.GetCache<IList<MMSServer>>("MMSServers");
            serversModel.MCSServers = cacheManage.GetCache<IList<MCSServer>>("MCSServers");
            serversModel.MDSServers = cacheManage.GetCache<IList<MDSServer>>("MDSServers");

            return View("FinishStart", serversModel);
        }


        private string SendStartCommand()
        {
            byte[] bytesSent;
            string cmdOrder = "";
            string tempStr = "";
            Socket tempSocket;

            try
            {
                if (Singleton<JavaScriptSerializer>.Instance == null)
                {
                    Singleton<JavaScriptSerializer>.Instance = new JavaScriptSerializer();
                }
                tempStr = Singleton<JavaScriptSerializer>.Instance.Serialize(cacheManage.GetCache<IList<MCSServer>>("MCSServers"));

                for (int i = 0; i < cacheManage.GetCache<IList<MMSServer>>("MMSServers").Count; i++)
                {
                    bytesSent = Encoding.UTF8.GetBytes(CommonFlag.F_PSCallMMSStart + tempStr);
                    IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(cacheManage.GetCache<IList<MMSServer>>("MMSServers")[i].MMS_IP), cacheManage.GetCache<IList<MMSServer>>("MMSServers")[i].MMS_Port);
                    tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);
                    tempSocket.Send(bytesSent, bytesSent.Length, 0);
                    tempSocket.Close();
                }

                tempStr = Singleton<JavaScriptSerializer>.Instance.Serialize(cacheManage.GetCache<IList<MCSServer>>("MDSServers"));
                for (int i = 0; i < cacheManage.GetCache<IList<MCSServer>>("MCSServers").Count; i++)
                {
                    bytesSent = Encoding.UTF8.GetBytes(CommonFlag.F_PSCallMCSStart + tempStr);
                    IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].MCS_IP), cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].MCS_Port);
                    tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);
                    tempSocket.Send(bytesSent, bytesSent.Length, 0);
                    tempSocket.Close();
                }


                for (int i = 0; i < cacheManage.GetCache<IList<MDSServer>>("MDSServers").Count; i++)
                {
                    cmdOrder = CommonFlag.F_PSCallMDSStart;
                    cmdOrder = cmdOrder + CommonFlag.F_ArrangeChars;
                    cmdOrder = cmdOrder + cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr;
                    cmdOrder = cmdOrder + ";";

                    bytesSent = Encoding.UTF8.GetBytes(cmdOrder);
                    IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].MDS_IP), cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].MDS_Port);
                    tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);

                    tempSocket.Connect(ipe);
                    tempSocket.Send(bytesSent, bytesSent.Length, 0);
                    tempSocket.Close();
                }
                return string.Empty;
            }
            catch(Exception ex)
            {
                return ex.Message + ex.StackTrace;
            }
        }

        private string ArrangeChar()
        {
            //String[] chars = { "a", "b", "c", "d", "e", "f", "0", "1", "2", "3", "4", "5", "6", "7", "8", "9" };
            try
            {


                int mssCount = cacheManage.GetCache<IList<MMSServer>>("MMSServers").Count;
                int mcsCount = cacheManage.GetCache<IList<MCSServer>>("MCSServers").Count;
                int mdsCount = cacheManage.GetCache<IList<MDSServer>>("MDSServers").Count;
                IList<string> OldChars = new List<string>();
                IList<string> chars = new List<string>();


                //not to handle duplicate chars this time
                //IDictionary<string,char[]> mcsChars=new Dictionary<string,char[]>();
                //IDictionary<string,char> conflictChars=new Dictionary<string,char>();
                for (int i = 0; i < cacheManage.GetCache<IList<MMSServer>>("MMSServers").Count; i++)
                {
                    if (!string.IsNullOrEmpty(cacheManage.GetCache<IList<MMSServer>>("MMSServers")[i].ArrangeStr))
                    {
                        List<string> tempChars = cacheManage.GetCache<IList<MMSServer>>("MMSServers")[i].ArrangeStr.Split(',').ToList<string>();
                        foreach (string tempChar in tempChars)
                        {
                            chars.Add(tempChar);
                        }
                    }
                }


                //arrange MCSs
                for (int i = 0; i < cacheManage.GetCache<IList<MCSServer>>("MCSServers").Count; i++)
                {
                    if (!string.IsNullOrEmpty(cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].ArrangeStr))
                    {
                        List<string> tempChars = cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].ArrangeStr.Split(',').ToList<string>();
                        foreach (string tempChar in tempChars)
                        {
                            OldChars.Add(tempChar);
                        }
                    }
                }

                var tempquery = from aa in chars
                                join bb in OldChars on aa equals bb into cc
                                from temp in cc.DefaultIfEmpty()
                                where string.IsNullOrEmpty(temp)
                                select aa;

                //where string.IsNullOrEmpty(temp.ToString())

                IList<string> availableChars = tempquery.ToList();

                int eachServiceCount = chars.Count / mcsCount;
                int count = 0;

                for (int i = 0; i < cacheManage.GetCache<IList<MCSServer>>("MCSServers").Count; i++)
                {
                    count++;
                    if (count < mcsCount)
                    {
                        int tmpi = 0;
                        if (string.IsNullOrEmpty(cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].ArrangeStr))
                        {
                            tmpi = eachServiceCount;
                        }
                        else
                        {
                            tmpi = eachServiceCount - cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].ArrangeStr.Split(',').Length;
                        }

                        while (tmpi > 0)
                        {
                            cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].ArrangeStr = cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].ArrangeStr + "," + availableChars[0].ToString();
                            availableChars.Remove(availableChars[0]);
                            tmpi--;
                        }
                    }
                    else
                    {
                        foreach (string availableChar in availableChars)
                        {
                            cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].ArrangeStr = cacheManage.GetCache<IList<MCSServer>>("MCSServers")[i].ArrangeStr + "," + availableChar;
                        }
                    }
                }


                //arrange MDSs
                OldChars = new List<string>();
                for (int i = 0; i < cacheManage.GetCache<IList<MDSServer>>("MDSServers").Count; i++)
                {
                    if (!string.IsNullOrEmpty(cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr))
                    {
                        List<string> tempChars = cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr.Split(',').ToList<string>();
                        foreach (string tempChar in tempChars)
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

                eachServiceCount = chars.Count / mcsCount;
                count = 0;

                for (int i = 0; i < cacheManage.GetCache<IList<MDSServer>>("MDSServers").Count; i++)
                {
                    count++;
                    if (count < mcsCount)
                    {
                        int tmpi = 0;
                        if (string.IsNullOrEmpty(cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr))
                        {
                            tmpi = eachServiceCount;
                        }
                        else
                        {
                            tmpi = eachServiceCount - cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr.Split(',').Length;
                        }

                        while (tmpi > 0)
                        {
                            cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr = cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr + availableChars[0].ToString();
                            availableChars.Remove(availableChars[0]);
                            tmpi--;
                        }
                    }
                    else
                    {
                        foreach (string availableChar in availableChars)
                        {
                            cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr = cacheManage.GetCache<IList<MDSServer>>("MDSServers")[i].ArrangeStr + availableChar;
                        }
                    }
                }

                return string.Empty;
            }
            catch(Exception ex)
            {
                return ex.Message + ex.StackTrace;
            }
        }

        //public ActionResult FindMMS()
        //{
        //    MMSModel mMSModel = new MMSModel();

        //    if (cacheManage.GetCache<IList<MMSServer>>("MMSServers") == null || cacheManage.GetCache<IList<MMSServer>>("MMSServers").Count <= 0)
        //    {
        //        IList<MMSServer> mmsServers = mmsServerRepository.Table.ToList();
        //        if (mmsServers != null && mmsServers.Count > 0)
        //        {
        //            cacheManage.AddCache<IList<MMSServer>>("MMSServers", mmsServers);
        //        }
        //    }

        //    return Json(mMSModel, JsonRequestBehavior.AllowGet);
        //}

        public ActionResult Index()
        {
            MMSModel mMSModel = new MMSModel();
            mMSModel.MMS_IP = cacheManage.GetCache<string>("MMSIP");
            mMSModel.MMS_Port = cacheManage.GetCache<int>("MMSPort");
            return View(mMSModel);
        }
    }
}
