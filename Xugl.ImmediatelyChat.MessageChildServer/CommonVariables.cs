﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Xugl.ImmediatelyChat.Common;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Model;

namespace Xugl.ImmediatelyChat.MessageChildServer
{
    public class CommonVariables
    {
        public static string PSIP { get; set; }
        public static int PSPort { get; set; }

        public static string MCSIP { get; set; }
        public static int MCSPort { get; set; }
        public static string MCS_ID { get; set; }

        public static string ArrangeStr { get; set; }

        public static bool IsBeginMessageService { get; set; }
        #region JavaScriptSerializer

        public static JavaScriptSerializer serializer
        {
            get
            {
                if(Singleton<JavaScriptSerializer>.Instance==null)
                {
                    Singleton<JavaScriptSerializer>.Instance = new JavaScriptSerializer();
                }

                return Singleton<JavaScriptSerializer>.Instance;
            }
        }

        #endregion

        #region manage MDSs

        public static IList<MDSServer> MDSServers
        {
            get
            {
                return SingletonList<MDSServer>.Instance;
            }
        }

        #endregion

        #region Log tool

        public static  ICommonLog LogTool
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


        #region InRunningUAList
        //public static IList<string> InRunningUAList
        //{
        //    get
        //    {
        //        return SingletonList<string>.Instance;
        //    }
        //}
        #endregion


        #region  OperateFile
        public static IOperateFile OperateFile
        {
            get
            {
                if (Singleton<IOperateFile>.Instance == null)
                {
                    Singleton<IOperateFile>.Instance = Xugl.ImmediatelyChat.Core.DependencyResolution.ObjectContainerFactory.CurrentContainer.Resolver<IOperateFile>();
                }
                return Singleton<IOperateFile>.Instance;
            }
        }


        private static string m_ConfigFilePath;
        public static string ConfigFilePath
        {
            get
            {
                if (string.IsNullOrEmpty(m_ConfigFilePath))
                {
                    m_ConfigFilePath = AppDomain.CurrentDomain.BaseDirectory + "config.txt";
                }
                return m_ConfigFilePath;
            }
        }
        #endregion
    }
}
