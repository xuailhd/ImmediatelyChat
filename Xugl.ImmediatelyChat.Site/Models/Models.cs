using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Data.Entity;
using System.Globalization;
using System.Web.Security;

namespace Xugl.ImmediatelyChat.Site.Models
{
    public class LoginReturnContext
    {
        public string ObjectID { get; set; }
        public string IP { get; set; }
        public int Port { get; set; }
        public string CilentIP { get; set; }
        /// <summary>
        /// 0/1/   normal/account or password incorrect
        /// </summary>
        public int Status { get; set; }
    }
    
}
