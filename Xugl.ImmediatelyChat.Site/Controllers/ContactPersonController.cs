using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.Site.Models;
using Xugl.ImmediatelyChat.Core.DependencyResolution;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.Site.Controllers
{
    public class ContactPersonController : Controller
    {
        private IRepository<ContactPerson> contactPersonRepository;
        private IRepository<ContactGroupSub> contactGroupSubRepository;
        private ICacheManage cacheManage;

        public ContactPersonController(IRepository<ContactPerson> contactPersonRepository, IRepository<ContactGroupSub> contactGroupSubRepository,
            ICacheManage cacheManage)
        {
            this.contactPersonRepository = contactPersonRepository;
            this.contactGroupSubRepository = contactGroupSubRepository;
            this.cacheManage = cacheManage;
        }
        //
        // GET: /ContactPerson/

        public JsonResult LoginForAPI(string ObjectName,string Password)
        {
            //contactPersonRepository = ObjectContainerFactory.CurrentContainer.Resolver<IRepository<ContactPerson>>();
            //contactGroupSubRepository = ObjectContainerFactory.CurrentContainer.Resolver<IRepository<ContactGroupSub>>();
            if (string.IsNullOrEmpty(ObjectName))
            {
                return null;
            }

            ContactPerson contactPerson = contactPersonRepository.Find(t => t.ContactName == ObjectName);
            LoginReturnContext loginReturnContext = new LoginReturnContext();
            if (contactPerson == null)
            {
                contactPerson = new ContactPerson();
                contactPerson.ObjectID = Guid.NewGuid().ToString();
                contactPerson.ContactName = ObjectName;
                contactPerson.Password = "123456";
                contactPersonRepository.Insert(contactPerson);
                ContactGroupSub contactGroupSub = new ContactGroupSub();
                contactGroupSub.ContactGroupID = "Group1";
                contactGroupSub.ContactPersonObjectID = contactPerson.ObjectID;
                contactGroupSubRepository.Insert(contactGroupSub);
                loginReturnContext.Status = 0;
            }
            else
            {
                if(contactPerson.Password!=Password )
                {
                    loginReturnContext.Status = 1;
                }
                else
                {
                    loginReturnContext.Status = 0;
                }

            }

            loginReturnContext.IP = cacheManage.GetCache<string>("MMSIP");
            loginReturnContext.Port = cacheManage.GetCache<int>("MMSPort");
            loginReturnContext.ObjectID = contactPerson.ObjectID;
            
            return Json(loginReturnContext, JsonRequestBehavior.AllowGet);
        }


        //private string GetIP()
        //{
        //    string ip = string.Empty;
        //    if (!string.IsNullOrEmpty(System.Web.HttpContext.Current.Request.ServerVariables["HTTP_VIA"]))
        //        ip = Convert.ToString(System.Web.HttpContext.Current.Request.ServerVariables["HTTP_X_FORWARDED_FOR"]);
        //    if (string.IsNullOrEmpty(ip))
        //        ip = Convert.ToString(System.Web.HttpContext.Current.Request.ServerVariables["REMOTE_ADDR"]);
        //    return ip;
        //}

    }
}
