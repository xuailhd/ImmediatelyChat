using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.Common
{
    public class ClientStatusModel
    {
        public string ObjectID { get; set; }

        public string MCS_IP { get; set; }

        public int MCS_Port { get; set; }

        public DateTime LatestTime { get; set; }

        public DateTime UpdateTime { get; set; }
    }

    public class ContactData
    {
        public string ObjectID { get; set; }
        public string Password { get; set; }
        public string ImageSrc { get; set; }
        public string ContactName { get; set; }
        public DateTime? LatestTime { get; set; }

        private string ContactPerson { get; set; }
        public string DestinationObjectID { get; set; }


        public string GroupObjectID { get; set; }
        public string GroupName { get; set; }

        public string ContactPersonObjectID { get; set; }
        public string ContactGroupID { get; set; }


        public bool IsDelete { get; set; }
        public DateTime UpdateTime { get; set; }

        /// <summary>
        /// 0-ContactPerson/1-ContactPersonList/2-ContactGroup/3-ContactGroupSub
        /// </summary>
        public int DataType { get; set; }

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
        public string MessageID { get; set; }

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

        public string MDS_IP { get; set; }
        public int MDS_Port { get; set; }
        public string MDS_ID { get; set; }

        /// <summary>
        /// 0:normal  1:success  2:error
        /// </summary>
        //public int Status { get; set; }
    }

    public class GetMsgModel
    {
        public string MessageID { get; set; }
        public string ObjectID { get; set; }

        public IList<string> GroupIDs { get; set; }
        public DateTime LatestTime { get; set; }

        public string MDS_IP { get; set; }
        public int MDS_Port { get; set; }
        public string MDS_ID { get; set; }

        /// <summary>
        /// 0:normal  1:success  2:error
        /// </summary>
        //public int Status { get; set; }
    }

}
