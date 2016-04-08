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
        private IDictionary<string, GetMsgModel> BufferGetMsgModels1 = new Dictionary<string, GetMsgModel>();
        private IDictionary<string, GetMsgModel> BufferGetMsgModels2 = new Dictionary<string, GetMsgModel>();
        //for loop and delete
        private IList<string> BufferGetMsgKeys1 = new List<string>();
        private IList<string> BufferGetMsgKeys2 = new List<string>();
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
        private IDictionary<string, IList<MsgRecord>> OutMsgRecords1 = new Dictionary<string, IList<MsgRecord>>();
        private IDictionary<string, IList<MsgRecord>> OutMsgRecords2 = new Dictionary<string, IList<MsgRecord>>();
        //for loop and delete
        private IList<string> OutMsgRecordKeys1 = new List<string>();
        private IList<string> OutMsgRecordKeys2 = new List<string>();
        private bool UsingTagForOutMsg = false;

        private AsyncSocketClient sendMsgClient;
        private AsyncSocketClient getMsgClient;

        private const int _maxSize = 1024;
        private const int _maxSendConnections = 10;
        private const int _maxGetConnections = 10;
        private const int _sendDelay = 200;
        private const int _getDelay = 500;

        public bool IsRunning = false;

        private object lockObject = new object();

        public void AddMSgRecordIntoBuffer(MsgRecordModel _msgRecordModel)
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


        private IDictionary<string, GetMsgModel> GetUsingGetMsgBuffer
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

        private IDictionary<string, GetMsgModel> GetUnUsingGetMsgBuffer
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

        private IList<string> GetUsingGetMsgKeys
        {
            get
            {
                if (UsingTagForGetMsg)
                {
                    return BufferGetMsgKeys1;
                }
                else
                {
                    return BufferGetMsgKeys2;
                }
            }
        }

        private IList<string> GetUnUsingGetMsgKeys
        {
            get
            {
                if (!UsingTagForGetMsg)
                {
                    return BufferGetMsgKeys1;
                }
                else
                {
                    return BufferGetMsgKeys2;
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

        private IDictionary<string, IList<MsgRecord>> GetUsingOutMsgBuffer
        {
            get
            {
                if(UsingTagForOutMsg)
                {
                    return OutMsgRecords1;
                }
                else
                {
                    return OutMsgRecords2;
                }
            }
        }

        private IDictionary<string, IList<MsgRecord>> GetUnUsingOutMsgBuffer
        {
            get
            {
                if (!UsingTagForOutMsg)
                {
                    return OutMsgRecords1;
                }
                else
                {
                    return OutMsgRecords2;
                }
            }
        }

        private IList<string> GetUsingOutMsgKeys
        {
            get
            {
                if (UsingTagForOutMsg)
                {
                    return OutMsgRecordKeys1;
                }
                else
                {
                    return OutMsgRecordKeys2;
                }
            }
        }

        private IList<string> GetUnUsingOutMsgKeys
        {
            get
            {
                if (!UsingTagForOutMsg)
                {
                    return OutMsgRecordKeys1;
                }
                else
                {
                    return OutMsgRecordKeys2;
                }
            }
        }

        #endregion

        private IList<MsgRecordModel> GenerateMsgRecordModel(MsgRecordModel msgRecordModel)
        {
            IList<MsgRecordModel> msgRecords = new List<MsgRecordModel>();
            if (msgRecordModel.SendType == 1)
            {
                IContactPersonService contactGroupService = ObjectContainerFactory.CurrentContainer.Resolver<IContactPersonService>();
                IList<ContactPerson> ContactPersons = contactGroupService.GetContactPersonIDListByGroupID(msgRecordModel.RecivedGroupID);

                foreach (ContactPerson contactPerson in ContactPersons)
                {
                    MsgRecordModel _msgRecordModel = new MsgRecordModel();
                    _msgRecordModel.Content = msgRecordModel.Content;
                    _msgRecordModel.MsgType = msgRecordModel.MsgType;
                    _msgRecordModel.ObjectID = msgRecordModel.ObjectID;
                    _msgRecordModel.ObjectName = msgRecordModel.ObjectName;
                    _msgRecordModel.RecivedGroupID = msgRecordModel.RecivedGroupID;
                    _msgRecordModel.RecivedObjectID = msgRecordModel.RecivedObjectID;
                    _msgRecordModel.RecivedObjectID2 = msgRecordModel.RecivedObjectID2;
                    _msgRecordModel.SendType = msgRecordModel.SendType;
                    _msgRecordModel.MessageID = Guid.NewGuid().ToString();

                    for (int i = 0; i < CommonVariables.MDSServers.Count;i++ )
                    {
                        if (CommonVariables.MDSServers[i].ArrangeStr.Contains(_msgRecordModel.RecivedObjectID.Substring(0, 1)))
                        {
                            _msgRecordModel.MDS_IP = CommonVariables.MDSServers[i].MDS_IP;
                            _msgRecordModel.MDS_Port = CommonVariables.MDSServers[i].MDS_Port;
                            //_msgRecordModel.MDS_ID = CommonVariables.MDSServers[i].MDS_ID;
                            break;
                        }
                    }
                }
            }
            else
            {
                for (int i = 0; i < CommonVariables.MDSServers.Count; i++)
                {
                    if (CommonVariables.MDSServers[i].ArrangeStr.Contains(msgRecordModel.RecivedObjectID.Substring(0, 1)))
                    {
                        msgRecordModel.MDS_IP = CommonVariables.MDSServers[i].MDS_IP;
                        msgRecordModel.MDS_Port = CommonVariables.MDSServers[i].MDS_Port;
                        //msgRecordModel.MDS_ID = CommonVariables.GetMDSs[mds_id].MDS_ID;
                        msgRecordModel.MessageID = Guid.NewGuid().ToString();
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
                


                if (getMsgModel.LatestTime.CompareTo(CommonVariables.ClientModels[getMsgModel.ObjectID].LatestTime) > 0)
                {
                    lock (lockObject)
                    {
                        if (GetUsingGetMsgKeys.Contains(getMsgModel.ObjectID))
                        {
                            GetUsingGetMsgBuffer[getMsgModel.ObjectID].LatestTime = getMsgModel.LatestTime;
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
                                    //getMsgModel.MDS_ID = CommonVariables.GetMDSs[mds_id].MDS_ID;
                                    //CommonVariables.LogTool.Log("getMsgModel " + getMsgModel.MDS_IP + ":" + getMsgModel.MDS_Port.ToString());
                                    break;
                                }
                            }
                            GetUsingGetMsgBuffer.Add(getMsgModel.ObjectID, getMsgModel);
                        }
                    }
                }
            }
        }

        public void AddMsgIntoOutBuffer(MsgRecord msgRecord)
        {
            try
            {
                if (GetUsingOutMsgKeys.Contains(msgRecord.MsgRecipientGroupID))
                {
                    if (!(GetUsingOutMsgBuffer[msgRecord.MsgRecipientGroupID].Where(t => t.MsgID == msgRecord.MsgID).Count() > 0))
                    {
                        GetUsingOutMsgBuffer[msgRecord.MsgRecipientGroupID].Add(msgRecord);
                    }
                }
            }
            catch (KeyNotFoundException ex)
            {
            }
        }

        public IList<MsgRecord> GetMSG(GetMsgModel getMsgModel)
        {

            IList<MsgRecord> msgRecords = GetUsingOutMsgBuffer[getMsgModel.ObjectID].Where(t => t.SendTime.CompareTo(getMsgModel.LatestTime) <= 0).ToList();

            if (msgRecords != null && msgRecords.Count > 0)
            {
                for (int i = 0; i < msgRecords.Count; i++)
                {
                    GetUsingOutMsgBuffer[getMsgModel.ObjectID].Remove(msgRecords[i]);
                }
            }
            return msgRecords;
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
                                sendMsgClient.SendMsg(msgRecordModel.MDS_IP, msgRecordModel.MDS_Port, messageStr, msgRecordModel.MessageID, HandlerMsgReturnData);

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
                        while (GetUnUsingGetMsgKeys.Count > 0)
                        {
                            GetMsgModel getMsgModel = GetUnUsingGetMsgBuffer[GetUnUsingGetMsgKeys[0]];
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
                            GetUnUsingGetMsgBuffer.Remove(GetUnUsingGetMsgKeys[0]);
                            GetUnUsingGetMsgKeys.RemoveAt(0);
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

