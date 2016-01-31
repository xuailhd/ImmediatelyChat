using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.Model
{
    public class MsgRecord
    {
        public string MsgID { get; set; }

        public string MsgSenderObjectID { get; set; }

        public string MsgSenderName { get; set; }

        public string MsgContent { get; set; }

        public string MsgRecipientObjectID { get; set; }

        public string GroupID { get; set; }

        /// <summary>
        /// 0:text;1:file
        /// </summary>
        public int MsgType { get; set; }

        public DateTime SendTime { get; set; }

        public bool IsSended { get; set; }
    }
}
