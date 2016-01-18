using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageChildServer
{
    public class CommonVariables
    {
        public static string PSIP
        {
            get;
            set;
        }

        public static int PSPort
        {
            get;
            set;
        }


        public static string MMSIP
        {
            get;
            set;
        }

        public static int MMSPort
        {
            get;
            set;
        }

        public static string MCSIP
        {
            get;
            set;
        }

        public static int MCSPort
        {
            get;
            set;
        }

        public static string MCS_ID
        {
            get;
            set;
        }

        public static string ArrangeChars { get; set; }

        public static JavaScriptSerializer serializer { get; set; }

        #region manage MDSs

        public static IDictionary<string, MDSModel> GetMDSs
        {
            get
            {
                if (Singleton<IDictionary<string, MDSModel>>.Instance == null)
                {
                    Singleton<IDictionary<string, MDSModel>>.Instance = new Dictionary<string, MDSModel>();
                }
                return Singleton<IDictionary<string, MDSModel>>.Instance;
            }
        }

        public static void AddMDS(MDSModel mdsModel)
        {
            if (Singleton<IDictionary<string, MDSModel>>.Instance == null)
            {
                Singleton<IDictionary<string, MDSModel>>.Instance = new Dictionary<string, MDSModel>();
            }

            lock (Singleton<IDictionary<string, MDSModel>>.Instance)
            {
                Singleton<IDictionary<string, MDSModel>>.Instance.Add(mdsModel.MDS_ID, mdsModel);
            }
        }

        public static void RemoveMDS(string mds_ID)
        {
            lock (Singleton<IDictionary<string, MDSModel>>.Instance)
            {
                Singleton<IDictionary<string, MDSModel>>.Instance.Remove(mds_ID);
            }
        }

        #endregion

        #region Log tool

        public static ICommonLog LogTool
        {
            get
            {
                if (Singleton<ICommonLog>.Instance == null)
                {
                    Singleton<ICommonLog>.Instance = new CommonLog();
                }
                return Singleton<ICommonLog>.Instance;
            }
        }

        #endregion
    }
}
