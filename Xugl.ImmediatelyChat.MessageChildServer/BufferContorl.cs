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
    public class BufferContorl
    {
        private int InputCount;
        private int OutPutCount;

        private IList<MsgRecordModel> BufferMsgRecordModels1 = new List<MsgRecordModel>();
        private IList<MsgRecordModel> BufferMsgRecordModels2 = new List<MsgRecordModel>();

        private IList<GetMsgModel> BufferGetMsgModels1 = new List<GetMsgModel>();
        private IList<GetMsgModel> BufferGetMsgModels2 = new List<GetMsgModel>();

        private IList<ClientStatusModel> BufferUAModels1 = new List<ClientStatusModel>();
        private IList<ClientStatusModel> BufferUAModels2 = new List<ClientStatusModel>();


        private bool UsingTagForMsgRecord = false;
        private bool UsingTagForGetMsg = false;
        private bool UsingTagForUAMode = false;

        //for prevent async data error
        private IList<MsgRecordModel> ExeingMsgRecordModels = new List<MsgRecordModel>();
        //private IList<GetMsgModel> ExeingGetMsgModels = new List<GetMsgModel>();

        private IList<MsgRecord> OutMsgRecords = new List<MsgRecord>();
        private AsyncSocketClient asyncSocketClient;

        private int _maxSize = 1024;
        private int _maxConnnections = 10;

        public bool IsRunning = false;

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
            GetUsingGetMsgBuffer.Add(getMsgModel);
        }

        public void AddMsgIntoOutBuffer(MsgRecord msgRecord)
        {
            if (!(OutMsgRecords.Where(t => t.MsgID == msgRecord.MsgID).Count() > 0))
            {
                OutMsgRecords.Add(msgRecord);
            }
        }

        public IList<MsgRecord> GetMSG(GetMsgModel getMsgModel)
        {
            IList<MsgRecord> msgRecords= OutMsgRecords.Where(t => DateTime.Compare(t.SendTime, getMsgModel.LatestTime) <= 0
                && t.MsgRecipientObjectID == getMsgModel.ObjectID).ToList();

            for(int i=0;i<msgRecords.Count;i++)
            {
                OutMsgRecords.Remove(msgRecords[i]);
            }

            return msgRecords;
        }

        public void ReturnMsg(IList<MsgRecord> msgRecords)
        {
            if(msgRecords!=null && msgRecords.Count>0)
            {
                for(int i=0;i<msgRecords.Count;i++)
                {
                    OutMsgRecords.Add(msgRecords[i]);
                }
            }
        }

        //public void BufferContorlStart()
        //{
        //    WaitCallback waitCallback=new WaitCallback(HandlerInputTask);

        //    ThreadPool.QueueUserWorkItem()

        //    //ThreadStart threadStart = new ThreadStart(HandlerTask);
        //    //mainThread = new Thread(threadStart);
        //    //mainThread.Start();
        //}

        //private void HandlerInputTask(MsgRecordModel msgRecordModel)
        //{
        //    if (!ThreadPool.QueueUserWorkItem(new WaitCallback(InputMessageTask), msgRecordModel))
        //    {
        //        CommonVariables.LogTool.Log("Insert Thread Pool Failure");
        //    }
        //}

        //private void GetMessageTask(object _getMsgModel)
        //{
        //    GetMsgModel getMsgModel = (GetMsgModel)_getMsgModel;

        //    foreach (string mds_id in CommonVariables.GetMDSs.Keys)
        //    {
        //        if (CommonVariables.GetMDSs[mds_id].ArrangeChars.Contains(getMsgModel.ObjectID.Substring(0, 1)))
        //        {
        //            getMsgModel.MDS_IP = CommonVariables.GetMDSs[mds_id].MDS_IP;
        //            getMsgModel.MDS_Port = CommonVariables.GetMDSs[mds_id].MDS_Port;
        //            break;
        //        }
        //    }

        //    BufferGetMsgModels.Add(getMsgModel);

        //}

        //private void InputMessageTask(object _msgRecordModel)
        //{

        //    MsgRecordModel msgRecordModel = (MsgRecordModel)_msgRecordModel;

        //    IList<MsgRecordModel> msgRecordModels = null;

        //    if (msgRecordModel.SendType == 1)
        //    {
        //        msgRecordModels = GenerateMsgRecordModel(msgRecordModel);
        //        foreach (MsgRecordModel tempmsgRecordModel in msgRecordModels)
        //        {
        //            BufferMsgRecordModels.Add(tempmsgRecordModel);
        //        }
        //    }
        //    else
        //    {

        //        foreach (string mds_id in CommonVariables.GetMDSs.Keys)
        //        {
        //            if (CommonVariables.GetMDSs[mds_id].ArrangeChars.Contains(msgRecordModel.RecivedObjectID.Substring(0, 1)))
        //            {
        //                msgRecordModel.MDS_IP = CommonVariables.GetMDSs[mds_id].MDS_IP;
        //                msgRecordModel.MDS_Port = CommonVariables.GetMDSs[mds_id].MDS_Port;
        //                break;
        //            }
        //        }
        //        BufferMsgRecordModels.Add(msgRecordModel);
        //    }
        //}

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
            bool tag = true;

            asyncSocketClient = new AsyncSocketClient(_maxSize, _maxConnnections, CommonVariables.LogTool);

            CommonVariables.LogTool.Log("begin buffer contorl");

            try
            {

                while (IsRunning)
                {
                    if (tag)
                    {
                        //handler new message

                        #region handler new message data
                        //CommonVariables.LogTool.Log("GetUsingMsgRecordBuffer count  " + GetUsingMsgRecordBuffer.Count.ToString());
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
                                    asyncSocketClient.SendMsg(msgRecordModel.MDS_IP, msgRecordModel.MDS_Port, messageStr, msgRecordModel.MessageID, HandlerMsgReturnData);

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

                        #endregion

                        tag = !tag;
                    }
                    else
                    {
                        //CommonVariables.LogTool.Log("GetUsingGetMsgBuffer count  " + GetUsingGetMsgBuffer.Count.ToString());
                        #region handler get message
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
                                    asyncSocketClient.SendMsg(getMsgModel.MDS_IP, getMsgModel.MDS_Port, messageStr, getMsgModel.MessageID, HandlerGetMsgReturnData);
                                    //ExeingGetMsgModels.Add(getMsgModel);                        
                                }
                                catch (Exception ex)
                                {
                                    CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
                                }
                                GetUnUsingGetMsgBuffer.RemoveAt(0);
                            }
                        }
                        #endregion

                        tag = !tag;
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

