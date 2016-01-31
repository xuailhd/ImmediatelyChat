using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Xugl.ImmediatelyChat.Core;

namespace Xugl.ImmediatelyChat.MessageDataServer
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

        public static string MDSIP
        {
            get;
            set;
        }

        public static int MDSPort
        {
            get;
            set;
        }

        public static string MDS_ID
        {
            get;
            set;
        }

        public static string ArrangeChars { get; set; }

        #region JavaScriptSerializer

        public static JavaScriptSerializer serializer
        {
            get
            {
                if (Singleton<JavaScriptSerializer>.Instance == null)
                {
                    Singleton<JavaScriptSerializer>.Instance = new JavaScriptSerializer();
                }

                return Singleton<JavaScriptSerializer>.Instance;
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
                    Singleton<ICommonLog>.Instance = Xugl.ImmediatelyChat.Core.DependencyResolution.ObjectContainerFactory.CurrentContainer.Resolver<ICommonLog>();
                }
                return Singleton<ICommonLog>.Instance;
            }
        }

        #endregion

        #region BufferContorl
        public static BufferContorl MessageContorl
        {
            get
            {
                if (Singleton<BufferContorl>.Instance == null)
                {
                    Singleton<BufferContorl>.Instance = new BufferContorl();
                }
                return Singleton<BufferContorl>.Instance;
            }
        }
        #endregion
    }
}
