using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.Core
{
    public class ClientStatusModel
    {
        public string ObjectID { get; set; }

        public string MCS_IP { get; set; }

        public int MCS_Port { get; set; }
    }

    public class MMSModel
    {
        public string MMS_IP { get; set; }
        public int MMS_Port { get; set; }
    }

    public class MDSModel
    {
        public string MDS_IP { get; set; }
        public int MDS_Port { get; set; }
        public string ArrangeChars { get; set; }
        public string MDS_ID { get; set; }
    }

    public class MCSModel
    {
        public string MCS_IP { get; set; }
        public int MCS_Port { get; set; }
        public string ArrangeChars { get; set; }
        public string MCS_ID { get; set; }
    }

    public class MsgRecordModel
    {
        public string ObjectID { get; set; }

        public string ObjectName { get; set; }

        public string Content { get; set; }

        public string RecivedGroupID { get; set; }

        public string RecivedObjectID { get; set; }

        public string RecivedObjectID2 { get; set; }

        /// <summary>
        /// 0:text;1:file
        /// </summary>
        public int MsgType { get; set; }

        /// <summary>
        /// 0:single;1:group
        /// </summary>
        public int SendType { get; set; }

        public DateTime SendTime { get; set; }

        //public bool IsGetMessage { get; set; }
        public string MDS_IP { get; set; }
        public int MDS_Port { get; set; }
    }

    public class GetMsgModel
    {
        public string ObjectID { get; set; }

        public IList<string> GroupIDs { get; set; }

        public DateTime EndTime { get; set; }

        public DateTime StartTime { get; set; }

        public string MDS_IP { get; set; }
        public int MDS_Port { get; set; }
    }

}
