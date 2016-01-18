using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xugl.ImmediatelyChat.Site.Models;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.Site.Controllers
{
    public class AppServerController : Controller
    {
        private readonly ICacheManage cacheManage;
        public AppServerController(ICacheManage cacheManage)
        {
            this.cacheManage = cacheManage;
        }

        //
        // GET: /AppServer/

        public ActionResult CollectMMS(string ip,int port)
        {
            cacheManage.AddCache<string>("MMSIP", ip);
            cacheManage.AddCache<int>("MMSPort", port);

            return Json("ok",JsonRequestBehavior.AllowGet);
        }

        public ActionResult FindMMS()
        {
            MMSModel mMSModel = new MMSModel();
            mMSModel.MMS_IP = cacheManage.GetCache<string>("MMSIP");
            mMSModel.MMS_Port = cacheManage.GetCache<int>("MMSPort");
            return Json(mMSModel, JsonRequestBehavior.AllowGet);
        }

        public ActionResult Index()
        {
            MMSModel mMSModel = new MMSModel();
            mMSModel.MMS_IP = cacheManage.GetCache<string>("MMSIP");
            mMSModel.MMS_Port = cacheManage.GetCache<int>("MMSPort");
            return View(mMSModel);
        }
    }
}
