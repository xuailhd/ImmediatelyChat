using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Xugl.ImmediatelyChat.Common;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Core.DependencyResolution;
using Xugl.ImmediatelyChat.IServices;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageMainServer
{
    public class BufferContorl
    {
        private int InputCount;
        private int OutPutCount;

        private IList<ClientStatusModel> BufferUAModels1 = new List<ClientStatusModel>();
        private IList<ClientStatusModel> BufferUAModels2 = new List<ClientStatusModel>();

        private IList<ContactData> ContactDatas = new List<ContactData>();

        private bool UsingTagForUAMode = false;

        private AsyncSocketClient asyncSocketClient;

        private int _maxSize = 1024;
        private int _maxConnnections = 10;

        public bool IsRunning = false;

        public void AddUAModelIntoBuffer(ClientStatusModel clientStatusModel)
        {
            GetUsingUAModelBuffer.Add(clientStatusModel);
        }


        #region using unusing buffer manage

        private IList<ClientStatusModel> GetUsingUAModelBuffer
        {
            get
            {
                if (UsingTagForUAMode)
                {
                    return BufferUAModels1;
                }
                else
                {
                    return BufferUAModels2;
                }
            }
        }

        private IList<ClientStatusModel> GetUnUsingUAModelBuffer
        {
            get
            {
                if (!UsingTagForUAMode)
                {
                    return BufferUAModels1;
                }
                else
                {
                    return BufferUAModels2;
                }
            }
        }

        #endregion

        public void StartMainThread()
        {
            IsRunning = true;
            ThreadStart threadStart = new ThreadStart(MainConnectMDSThreadAsync);
            Thread thread = new Thread(threadStart);
            thread.Start();
        }


        public void StopMainThread()
        {
            IsRunning = false;
        }

        private void MainConnectMDSThreadAsync()
        {
            asyncSocketClient = new AsyncSocketClient(_maxSize, _maxConnnections, CommonVariables.LogTool);

            CommonVariables.LogTool.Log("begin buffer contorl");
            try
            {
                while (IsRunning)
                {
                    //CommonVariables.LogTool.Log("GetUsingMsgRecordBuffer count  " + GetUsingMsgRecordBuffer.Count.ToString());
                    if (GetUsingUAModelBuffer.Count > 0)
                    {
                        UsingTagForUAMode = !UsingTagForUAMode;
                        //CommonVariables.LogTool.Log("GetUnUsingMsgRecordBuffer count  " + GetUnUsingMsgRecordBuffer.Count.ToString());
                        while (GetUnUsingUAModelBuffer.Count > 0)
                        {
                            ClientStatusModel clientStatusModel = GetUnUsingUAModelBuffer[0];
                            try
                            {
                                string messageStr = CommonFlag.F_MCSReceiveUAInfo + CommonVariables.serializer.Serialize(clientStatusModel);
                                //CommonVariables.LogTool.Log("begin send mds " + msgRecordModel.MDS_IP + " port:" + msgRecordModel.MDS_Port + messageStr);
                                asyncSocketClient.SendMsg(clientStatusModel.MCS_IP, clientStatusModel.MCS_Port, messageStr, msgRecordModel.MessageID, HandlerMsgReturnData);

                                ExeingMsgRecordModels.Add(msgRecordModel);
                            }
                            catch (Exception ex)
                            {
                                GetUsingMsgRecordBuffer.Add(msgRecordModel);
                                CommonVariables.LogTool.Log(msgRecordModel.MessageID + ex.Message + ex.StackTrace);
                            }
                            GetUnUsingMsgRecordBuffer.RemoveAt(0);
                        }
                    }
                    Thread.Sleep(2000);
                }
            }
            catch(Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
            }
        }
        


        private string HandlerMsgReturnData(string returnData,bool IsError)
        {
            if (!string.IsNullOrEmpty(returnData))
            {
                MsgRecordModel tempmodel=ExeingMsgRecordModels.Single(t => t.MessageID == returnData);
                if (IsError)
                {
                    GetUsingMsgRecordBuffer.Add(tempmodel);
                }
                ExeingMsgRecordModels.Remove(ExeingMsgRecordModels.Single(t => t.MessageID == returnData));
            }

            return string.Empty;
        }


        private string HandlerGetMsgReturnData(string returnData, bool IsError)
        {
            //CommonVariables.LogTool.Log("recive mds return data:" + returnData);
            if(IsError)
            {
                return string.Empty;
            }

           

            if (!string.IsNullOrEmpty(returnData))
            {
                if (returnData.StartsWith(CommonFlag.F_MCSVerfiyMDSMSG))
                {
                    string tempStr = returnData.Remove(0, CommonFlag.F_MCSVerfiyMDSMSG.Length);

                    MsgRecord tempMsgRecord = CommonVariables.serializer.Deserialize<MsgRecord>(tempStr);
                    if (tempMsgRecord != null)
                    {
                        if (!string.IsNullOrEmpty(tempMsgRecord.MsgID))
                        {
                            AddMsgIntoOutBuffer(tempMsgRecord);
                            return tempMsgRecord.MsgID;
                        }
                    }
                }
            }
            return string.Empty;
        }
    }

}

