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

namespace Xugl.ImmediatelyChat.MessageChildServer
{
    /// <summary>
    /// 通过UDP p2p发送的信息 不在这里的处理范围
    /// </summary>
    public class BufferContorl
    {
        /// <summary>
        /// new message Buffer, from UA
        /// </summary>
        private IList<MsgRecordModel> BufferMsgRecordModels1 = new List<MsgRecordModel>();
        private IList<MsgRecordModel> BufferMsgRecordModels2 = new List<MsgRecordModel>();
        private bool UsingTagForMsgRecord = false;

        /// <summary>
        /// message requestiong Buffer
        /// </summary>
        private IList<GetMsgModel> BufferGetMsgModels1 = new List<GetMsgModel>();
        private IList<GetMsgModel> BufferGetMsgModels2 = new List<GetMsgModel>();
        ////for loop and delete
        //private IList<string> BufferGetMsgKeys1 = new List<string>();
        //private IList<string> BufferGetMsgKeys2 = new List<string>();
        private bool UsingTagForGetMsg = false;

        private IList<ClientStatusModel> BufferUAModels1 = new List<ClientStatusModel>();
        private IList<ClientStatusModel> BufferUAModels2 = new List<ClientStatusModel>();
        private bool UsingTagForUAMode = false;

        /// <summary>
        /// for prevent async data error
        /// </summary>
        private IList<MsgRecordModel> ExeingMsgRecordModels = new List<MsgRecordModel>();
        //private IList<GetMsgModel> ExeingGetMsgModels = new List<GetMsgModel>();

        /// <summary>
        /// message Buffer, use to send to UA
        /// </summary>
        private IDictionary<string, IList<MsgRecord>> OutMsgRecords = new Dictionary<string, IList<MsgRecord>>();
        //private IDictionary<string, IList<MsgRecord>> OutMsgRecords2 = new Dictionary<string, IList<MsgRecord>>();
        ////for loop and delete
        //private IList<string> OutMsgRecordKeys1 = new List<string>();
        //private IList<string> OutMsgRecordKeys2 = new List<string>();
        //private bool UsingTagForOutMsg = false;

        private AsyncSocketClient sendMsgClient;
        private AsyncSocketClient getMsgClient;

        private const int _maxSize = 1024;
        private const int _maxSendConnections = 10;
        private const int _maxGetConnections = 10;
        private const int _sendDelay = 200;
        private const int _getDelay = 500;

        public bool IsRunning = false;

        private object lockObject = new object();

        public void AddMsgRecordIntoBuffer(MsgRecordModel _msgRecordModel)
        {
            IList<MsgRecordModel> msgRecordModels = GenerateMsgRecordModel(_msgRecordModel);

            foreach (MsgRecordModel msgRecordModel in msgRecordModels)
            {
                GetUsingMsgRecordBuffer.Add(msgRecordModel);
            }
        }


        #region using unusing buffer manage

        private IList<MsgRecordModel> GetUsingMsgRecordBuffer
        {
            get { 
                if(UsingTagForMsgRecord)
                {
                    return BufferMsgRecordModels1;
                }
                else
                {
                    return BufferMsgRecordModels2;
                }
            }
        }

        private IList<MsgRecordModel> GetUnUsingMsgRecordBuffer
        {
            get
            {
                if (!UsingTagForMsgRecord)
                {
                    return BufferMsgRecordModels1;
                }
                else
                {
                    return BufferMsgRecordModels2;
                }
            }
        }


        private IList<GetMsgModel> GetUsingGetMsgBuffer
        {
            get
            {
                if (UsingTagForGetMsg)
                {
                    return BufferGetMsgModels1;
                }
                else
                {
                    return BufferGetMsgModels2;
                }
            }
        }

        private IList<GetMsgModel> GetUnUsingGetMsgBuffer
        {
            get
            {
                if (!UsingTagForGetMsg)
                {
                    return BufferGetMsgModels1;
                }
                else
                {
                    return BufferGetMsgModels2;
                }
            }
        }

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

        private IList<MsgRecordModel> GenerateMsgRecordModel(MsgRecordModel msgRecordModel)
        {
            CommonVariables.LogTool.Log("GenerateMsgRecordModel:" + msgRecordModel.MsgRecipientGroupID + ":" + msgRecordModel.MsgRecipientObjectID);

            IList<MsgRecordModel> msgRecords = new List<MsgRecordModel>();
            if (!string.IsNullOrEmpty(msgRecordModel.MsgRecipientGroupID))
            {
                IContactPersonService contactGroupService = ObjectContainerFactory.CurrentContainer.Resolver<IContactPersonService>();
                IList<String> ContactPersonIDs = contactGroupService.GetContactPersonIDListByGroupID(msgRecordModel.MsgSenderObjectID,msgRecordModel.MsgRecipientGroupID);

                CommonVariables.LogTool.Log("GenerateMsgRecordModel:" + ContactPersonIDs + " count:" + ContactPersonIDs.Count);

                foreach (String objectID in ContactPersonIDs)
                {
                    MsgRecordModel _msgRecordModel = new MsgRecordModel();
                    _msgRecordModel.MsgContent = msgRecordModel.MsgContent;
                    _msgRecordModel.MsgType = msgRecordModel.MsgType;
                    _msgRecordModel.MsgSenderObjectID = msgRecordModel.MsgSenderObjectID;
                    _msgRecordModel.MsgSenderName = msgRecordModel.MsgSenderName;
                    _msgRecordModel.MsgRecipientGroupID = msgRecordModel.MsgRecipientGroupID;
                    _msgRecordModel.IsSended = msgRecordModel.IsSended;
                    _msgRecordModel.MsgRecipientObjectID = objectID;
                    _msgRecordModel.SendTime = msgRecordModel.SendTime;
                    _msgRecordModel.MsgID = Guid.NewGuid().ToString();
                    CommonVariables.LogTool.Log("generate MsgRecordModel :" + _msgRecordModel.MsgSenderName + " MsgRecipientGroupID:" + _msgRecordModel.MsgRecipientGroupID +
                        " MsgRecipientObjectID:" + _msgRecordModel.MsgRecipientObjectID);
                    for (int i = 0; i < CommonVariables.MDSServers.Count;i++ )
                    {
                        if (CommonVariables.MDSServers[i].ArrangeStr.Contains(_msgRecordModel.MsgRecipientObjectID.Substring(0, 1)))
                        {
                            _msgRecordModel.MDS_IP = CommonVariables.MDSServers[i].MDS_IP;
                            _msgRecordModel.MDS_Port = CommonVariables.MDSServers[i].MDS_Port;
                            //_msgRecordModel.MDS_ID = CommonVariables.MDSServers[i].MDS_ID;
                            break;
                        }
                    }

                    msgRecords.Add(msgRecordModel);
                }
            }
            else if (string.IsNullOrEmpty(msgRecordModel.MsgRecipientGroupID) && !string.IsNullOrEmpty(msgRecordModel.MsgRecipientObjectID))
            {
                for (int i = 0; i < CommonVariables.MDSServers.Count; i++)
                {
                    if (CommonVariables.MDSServers[i].ArrangeStr.Contains(msgRecordModel.MsgRecipientObjectID.Substring(0, 1)))
                    {
                        msgRecordModel.MDS_IP = CommonVariables.MDSServers[i].MDS_IP;
                        msgRecordModel.MDS_Port = CommonVariables.MDSServers[i].MDS_Port;
                        //msgRecordModel.MDS_ID = CommonVariables.GetMDSs[mds_id].MDS_ID;
                        if (string.IsNullOrEmpty(msgRecordModel.MsgID))
                        {
                            msgRecordModel.MsgID = Guid.NewGuid().ToString();
                        }
                        break;
                    }
                }
                msgRecords.Add(msgRecordModel);
            }
            return msgRecords;
        }

        public void AddGetMsgIntoBuffer(GetMsgModel getMsgModel)
        {
            if (CommonVariables.ClientModels.ContainsKey(getMsgModel.ObjectID))
            {
                //if (getMsgModel.LatestTime.CompareTo(CommonVariables.ClientModels[getMsgModel.ObjectID].LatestTime) > 0)
                //{
                    //CommonVariables.ClientModels[getMsgModel.ObjectID].LatestTime = getMsgModel.LatestTime;

                    getMsgModel.MessageID = Guid.NewGuid().ToString();
                    getMsgModel.MDS_IP = CommonVariables.ClientModels[getMsgModel.ObjectID].MDS_IP;
                    getMsgModel.MDS_Port = CommonVariables.ClientModels[getMsgModel.ObjectID].MDS_Port;
                    GetUsingGetMsgBuffer.Add(getMsgModel);

                    //CommonVariables.LogTool.Log("AddGetMsg by Exist:" + getMsgModel.ObjectID + "/" + getMsgModel.LatestTime + "/" + getMsgModel.MDS_IP);
                //}
            }
            else
            {
                for (int i = 0; i < CommonVariables.MDSServers.Count; i++)
                {
                    if (CommonVariables.MDSServers[i].ArrangeStr.Contains(getMsgModel.ObjectID.Substring(0, 1)))
                    {
                        getMsgModel.MessageID = Guid.NewGuid().ToString();
                        getMsgModel.MDS_IP = CommonVariables.MDSServers[i].MDS_IP;
                        getMsgModel.MDS_Port = CommonVariables.MDSServers[i].MDS_Port;
                        break;
                    }
                }
                CommonVariables.ClientModels.Add(getMsgModel.ObjectID, new ClientModel() { ObjectID = getMsgModel.ObjectID, 
                    LatestTime = getMsgModel.LatestTime, MDS_IP=getMsgModel.MDS_IP, MDS_Port= getMsgModel.MDS_Port });
                GetUsingGetMsgBuffer.Add(getMsgModel);

            }
        }

        public void AddMsgIntoOutBuffer(MsgRecord msgRecord)
        {
            CommonVariables.LogTool.Log("AddMsgIntoOutBuffer:");   
            if (OutMsgRecords.ContainsKey(msgRecord.MsgRecipientObjectID))
            {
                if (!(OutMsgRecords[msgRecord.MsgRecipientObjectID].Where(t => t.MsgID == msgRecord.MsgID).Count() > 0))
                {
                    OutMsgRecords[msgRecord.MsgRecipientObjectID].Add(msgRecord);
                }
            }
            else
            {
                if (msgRecord.MsgRecipientObjectID==null)
                {
                    CommonVariables.LogTool.Log("null");   
                }
                else
                {
                    CommonVariables.LogTool.Log(msgRecord.MsgRecipientObjectID);   
                }
                OutMsgRecords.Add(msgRecord.MsgRecipientObjectID, new List<MsgRecord>());
                OutMsgRecords[msgRecord.MsgRecipientObjectID].Add(msgRecord);
            }
        }

        public IList<MsgRecord> GetMSG(GetMsgModel getMsgModel)
        {
            if (OutMsgRecords.ContainsKey(getMsgModel.ObjectID))
            {
                IList<MsgRecord> msgRecords = OutMsgRecords[getMsgModel.ObjectID].Where(t => t.SendTime.CompareTo(getMsgModel.LatestTime) > 0).ToList();

                if (msgRecords != null && msgRecords.Count > 0)
                {
                    OutMsgRecords[getMsgModel.ObjectID].Clear();
                    return msgRecords;
                }
            }
            return null;
        }

        //public void ReturnMsg(IList<MsgRecord> msgRecords)
        //{
        //    if(msgRecords!=null && msgRecords.Count>0)
        //    {
        //        for(int i=0;i<msgRecords.Count;i++)
        //        {
        //            OutMsgRecords.Add(msgRecords[i]);
        //        }
        //    }
        //}

        public void StartMainThread()
        {
            IsRunning = true;
            ThreadStart threadStart = new ThreadStart(MainSendMSGThread);
            Thread thread = new Thread(threadStart);
            thread.Start();

            threadStart = new ThreadStart(MainGetMSGThread);
            thread = new Thread(threadStart);
            thread.Start();
        }


        public void StopMainThread()
        {
            IsRunning = false;
        }

        
        
        private void MainSendMSGThread()
        {
            sendMsgClient = new AsyncSocketClient(_maxSize, _maxSendConnections, CommonVariables.LogTool);
            try
            {
                while (IsRunning)
                {
                    if (GetUsingMsgRecordBuffer.Count > 0)
                    {
                        UsingTagForMsgRecord = !UsingTagForMsgRecord;
                        //CommonVariables.LogTool.Log("GetUnUsingMsgRecordBuffer count  " + GetUnUsingMsgRecordBuffer.Count.ToString());
                        

                        while (GetUnUsingMsgRecordBuffer.Count > 0)
                        {
                            MsgRecordModel msgRecordModel = GetUnUsingMsgRecordBuffer[0];
                            try
                            {
                                string messageStr = CommonFlag.F_MDSVerifyMCSMSG + CommonVariables.serializer.Serialize(msgRecordModel);
                                //CommonVariables.LogTool.Log("begin send mds " + msgRecordModel.MDS_IP + " port:" + msgRecordModel.MDS_Port + messageStr);
                                sendMsgClient.SendMsg(msgRecordModel.MDS_IP, msgRecordModel.MDS_Port, messageStr, msgRecordModel.MsgID, HandlerMsgReturnData);

                                ExeingMsgRecordModels.Add(msgRecordModel);
                            }
                            catch (Exception ex)
                            {
                                GetUsingMsgRecordBuffer.Add(msgRecordModel);
                                CommonVariables.LogTool.Log(msgRecordModel.MsgID + ex.Message + ex.StackTrace);
                            }
                            GetUnUsingMsgRecordBuffer.RemoveAt(0);
                        }
                    }
                    Thread.Sleep(_sendDelay);
                }
            }
            catch (Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
            }
        }

        private void MainGetMSGThread()
        {
            getMsgClient = new AsyncSocketClient(_maxSize, _maxGetConnections, CommonVariables.LogTool);
            try
            {
                while (IsRunning)
                {
                    if (GetUsingGetMsgBuffer.Count > 0)
                    {
                        UsingTagForGetMsg = !UsingTagForGetMsg;
                        //CommonVariables.LogTool.Log("GetUnUsingGetMsgBuffer count  " + GetUnUsingGetMsgBuffer.Count.ToString());
                        while (GetUnUsingGetMsgBuffer.Count > 0)
                        {
                            GetMsgModel getMsgModel = GetUnUsingGetMsgBuffer[0];
                            try
                            {
                                string messageStr = CommonFlag.F_MDSVerifyMCSGetMSG + CommonVariables.serializer.Serialize(getMsgModel);
                                //CommonVariables.LogTool.Log("begin send mds " + getMsgModel.MDS_IP + " port:" + getMsgModel.MDS_Port + messageStr);
                                getMsgClient.SendMsg(getMsgModel.MDS_IP, getMsgModel.MDS_Port, messageStr, getMsgModel.MessageID, HandlerGetMsgReturnData);
                                //ExeingGetMsgModels.Add(getMsgModel);                        
                            }
                            catch (Exception ex)
                            {
                                CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
                            }
                            GetUnUsingGetMsgBuffer.RemoveAt(0);
                        }
                    }
                    Thread.Sleep(_sendDelay);
                }
            }
            catch (Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
            }
        }

        private string HandlerMsgReturnData(string returnData,bool IsError)
        {
            if (!string.IsNullOrEmpty(returnData))
            {
                MsgRecordModel tempmodel=ExeingMsgRecordModels.Single(t => t.MsgID == returnData);
                if (IsError)
                {
                    GetUsingMsgRecordBuffer.Add(tempmodel);
                }
                ExeingMsgRecordModels.Remove(ExeingMsgRecordModels.Single(t => t.MsgID == returnData));
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
                            return CommonFlag.F_MDSReciveMCSFBMSG + tempMsgRecord.MsgID;
                        }
                    }
                }
            }
            return string.Empty;
        }
    }

}

