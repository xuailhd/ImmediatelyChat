using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;
using Xugl.ImmediatelyChat.Model;

namespace Xugl.ImmediatelyChat.Site.Models
{
    public class LoginReturnContext
    {
        public string ObjectID { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        /// <summary>
        /// 0/1/2   normal/account or password incorrect/MMS server have not start
        /// </summary>
        public int Status { get; set; }
    }

    public class ServersModel
    {
        public IList<MMSServer> MMSServers { get; set; }
        public IList<MCSServer> MCSServers { get; set; }
        public IList<MDSServer> MDSServers { get; set; }
    }
    
}
