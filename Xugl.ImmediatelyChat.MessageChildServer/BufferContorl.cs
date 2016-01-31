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

        private IList<MsgRecordModel> BufferMsgRecordModels = new List<MsgRecordModel>();
        private IList<GetMsgModel> BufferGetMsgModels = new List<GetMsgModel>();

        private IList<MsgRecordModel> PerpareMsgRecordModels = new List<MsgRecordModel>();
        private IList<GetMsgModel> PerpareGetMsgModels = new List<GetMsgModel>();

        //for prevent async data error
        private IList<MsgRecordModel> ExeingMsgRecordModels = new List<MsgRecordModel>();
        //private IList<GetMsgModel> ExeingGetMsgModels = new List<GetMsgModel>();

        private IList<MsgRecord> OutMsgRecords = new List<MsgRecord>();

        private Thread mainThread = null;
        private const int messagePriority = 3;
        private const int getMessagePriority = 2;

        private AsyncSocketClient asyncSocketClient;

        private int _maxSize = 1024;
        private int _maxConnnections = 10;

        public bool IsRunning = false;

        public void AddMSgRecordIntoBuffer(MsgRecordModel _msgRecordModel)
        {
            IList<MsgRecordModel> msgRecordModels = GenerateMsgRecordModel(_msgRecordModel);

            foreach (MsgRecordModel msgRecordModel in msgRecordModels)
            {
                BufferMsgRecordModels.Add(msgRecordModel);
            }
        }

        private IList<MsgRecordModel> GenerateMsgRecordModel(MsgRecordModel msgRecordModel)
        {
            IList<MsgRecordModel> msgRecords = new List<MsgRecordModel>();
            if (msgRecordModel.SendType == 1)
            {
                IContactGroupService contactGroupService = ObjectContainerFactory.CurrentContainer.Resolver<IContactGroupService>();
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

                    foreach (string mds_id in CommonVariables.GetMDSs.Keys)
                    {
                        if (CommonVariables.GetMDSs[mds_id].ArrangeChars.Contains(_msgRecordModel.RecivedObjectID.Substring(0, 1)))
                        {
                            _msgRecordModel.MDS_IP = CommonVariables.GetMDSs[mds_id].MDS_IP;
                            _msgRecordModel.MDS_Port = CommonVariables.GetMDSs[mds_id].MDS_Port;
                            _msgRecordModel.MDS_ID = CommonVariables.GetMDSs[mds_id].MDS_ID;
                            break;
                        }
                    }
                }
            }
            else
            {
                foreach (string mds_id in CommonVariables.GetMDSs.Keys)
                {
                    if (CommonVariables.GetMDSs[mds_id].ArrangeChars.Contains(msgRecordModel.RecivedObjectID.Substring(0, 1)))
                    {
                        msgRecordModel.MDS_IP = CommonVariables.GetMDSs[mds_id].MDS_IP;
                        msgRecordModel.MDS_Port = CommonVariables.GetMDSs[mds_id].MDS_Port;
                        msgRecordModel.MDS_ID = CommonVariables.GetMDSs[mds_id].MDS_ID;
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
            foreach (string mds_id in CommonVariables.GetMDSs.Keys)
            {
                if (CommonVariables.GetMDSs[mds_id].ArrangeChars.Contains(getMsgModel.ObjectID.Substring(0, 1)))
                {
                    getMsgModel.MessageID = Guid.NewGuid().ToString();
                    getMsgModel.MDS_IP = CommonVariables.GetMDSs[mds_id].MDS_IP;
                    getMsgModel.MDS_Port = CommonVariables.GetMDSs[mds_id].MDS_Port;
                    getMsgModel.MDS_ID = CommonVariables.GetMDSs[mds_id].MDS_ID;
                    break;
                }
            }
            BufferGetMsgModels.Add(getMsgModel);
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

        private void HandlerInputTask(MsgRecordModel msgRecordModel)
        {
            if (!ThreadPool.QueueUserWorkItem(new WaitCallback(InputMessageTask), msgRecordModel))
            {
                CommonVariables.LogTool.Log("Insert Thread Pool Failure");
            }
        }

        private void GetMessageTask(object _getMsgModel)
        {
            GetMsgModel getMsgModel = (GetMsgModel)_getMsgModel;

            foreach (string mds_id in CommonVariables.GetMDSs.Keys)
            {
                if (CommonVariables.GetMDSs[mds_id].ArrangeChars.Contains(getMsgModel.ObjectID.Substring(0, 1)))
                {
                    getMsgModel.MDS_IP = CommonVariables.GetMDSs[mds_id].MDS_IP;
                    getMsgModel.MDS_Port = CommonVariables.GetMDSs[mds_id].MDS_Port;
                    break;
                }
            }

            BufferGetMsgModels.Add(getMsgModel);

        }

        private void InputMessageTask(object _msgRecordModel)
        {

            MsgRecordModel msgRecordModel = (MsgRecordModel)_msgRecordModel;

            IList<MsgRecordModel> msgRecordModels = null;

            if (msgRecordModel.SendType == 1)
            {
                msgRecordModels = GenerateMsgRecordModel(msgRecordModel);
                foreach (MsgRecordModel tempmsgRecordModel in msgRecordModels)
                {
                    BufferMsgRecordModels.Add(tempmsgRecordModel);
                }
            }
            else
            {

                foreach (string mds_id in CommonVariables.GetMDSs.Keys)
                {
                    if (CommonVariables.GetMDSs[mds_id].ArrangeChars.Contains(msgRecordModel.RecivedObjectID.Substring(0, 1)))
                    {
                        msgRecordModel.MDS_IP = CommonVariables.GetMDSs[mds_id].MDS_IP;
                        msgRecordModel.MDS_Port = CommonVariables.GetMDSs[mds_id].MDS_Port;
                        break;
                    }
                }
                BufferMsgRecordModels.Add(msgRecordModel);
            }
        }

        public void StartMainThread()
        {
            ThreadStart threadStart = new ThreadStart(MainConnectMDSThreadAsync);
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        private void MainConnectMDSThreadAsync()
        {
            int tempMessagePriority = messagePriority;
            int tempGetMessageProority = getMessagePriority;
            asyncSocketClient = new AsyncSocketClient(_maxSize, _maxConnnections, CommonVariables.LogTool);
            while (IsRunning)
            {

                if (tempMessagePriority >= tempGetMessageProority)
                {
                    //handler new message
                    #region get data from buffer
                    if (PerpareMsgRecordModels.Count == 0 && BufferMsgRecordModels.Count > 0)
                    {
                        for (int i = 0; i < BufferMsgRecordModels.Count; i++)
                        {
                            PerpareMsgRecordModels.Add(BufferMsgRecordModels[i]);
                        }
                    }
                    #endregion

                    #region handler perpare area data

                    if (PerpareMsgRecordModels.Count > 0)
                    {
                        MsgRecordModel msgRecordModel = PerpareMsgRecordModels[0];
                        try
                        {
                            string messageStr = CommonFlag.F_MDSVerifyMCSMSG + CommonVariables.serializer.Serialize(msgRecordModel);
                            asyncSocketClient.SendMsg(msgRecordModel.MDS_IP, msgRecordModel.MDS_Port, messageStr, msgRecordModel.MessageID, HandlerMsgReturnData);

                            ExeingMsgRecordModels.Add(msgRecordModel);
                            PerpareMsgRecordModels.RemoveAt(0);
                        }
                        catch (Exception ex)
                        {
                            CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
                        }
                    }

                    #endregion

                    tempMessagePriority--;
                }
                else
                {
                    //handler get message
                    #region get data from buffer
                    if (PerpareGetMsgModels.Count == 0 && BufferGetMsgModels.Count > 0)
                    {
                        for (int i = 0; i < BufferGetMsgModels.Count;i++ )
                        {
                            PerpareGetMsgModels.Add(BufferGetMsgModels[i]);
                        }
                    }
                    #endregion

                    #region handler perpare area data
                    if (PerpareMsgRecordModels.Count > 0)
                    {
                        GetMsgModel getMsgModel = PerpareGetMsgModels[0];
                        try
                        {
                            string messageStr = CommonFlag.F_MDSVerifyMCSGetMSG + CommonVariables.serializer.Serialize(getMsgModel);
                            asyncSocketClient.SendMsg(getMsgModel.MDS_IP, getMsgModel.MDS_Port, messageStr, getMsgModel.MessageID, HandlerGetMsgReturnData);

                            //ExeingGetMsgModels.Add(getMsgModel);
                            PerpareGetMsgModels.RemoveAt(0);
                        }
                        catch (Exception ex)
                        {
                            CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
                        }

                    }
                    #endregion

                    tempMessagePriority--;
                }
            }
        }


        private string HandlerMsgReturnData(string returnData,bool IsError)
        {
            if (!string.IsNullOrEmpty(returnData))
            {
                MsgRecordModel tempmodel=ExeingMsgRecordModels.Single(t => t.MessageID == returnData);
                if (IsError)
                {
                    PerpareMsgRecordModels.Add(tempmodel);
                }
                ExeingMsgRecordModels.Remove(ExeingMsgRecordModels.Single(t => t.MessageID == returnData));
            }

            return string.Empty;
        }


        private string HandlerGetMsgReturnData(string returnData, bool IsError)
        {
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

