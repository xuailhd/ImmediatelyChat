using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageMainServer
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

        public static bool IsBeginMessageService { get; set; } 

        #region manage MCSs

        public static IDictionary<string, MCSModel> GetMCSs
        {
            get
            {
                if(Singleton<IDictionary<string,MCSModel>>.Instance==null)
                {
                    Singleton<IDictionary<string,MCSModel>>.Instance = new Dictionary<string,MCSModel>();
                }
                return Singleton<IDictionary<string,MCSModel>>.Instance;
            }
        }

        public static void AddMCS(MCSModel mcsModel)
        {
            if(Singleton<IDictionary<string,MCSModel>>.Instance==null)
            {
                Singleton<IDictionary<string,MCSModel>>.Instance = new Dictionary<string,MCSModel>();
            }

            lock (Singleton<IDictionary<string,MCSModel>>.Instance)
            { 
                Singleton<IDictionary<string,MCSModel>>.Instance.Add(mcsModel.MCS_ID, mcsModel);
            }
        }

        public static void RemoveMCS(string mcs_ID)
        {
            lock (Singleton<IDictionary<string, MCSModel>>.Instance)
            {
                Singleton<IDictionary<string, MCSModel>>.Instance.Remove(mcs_ID);
            }
        }

        #endregion

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
