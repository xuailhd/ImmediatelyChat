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

        private IList<MsgRecordModel> OutMsgRecordModels = new List<MsgRecordModel>();

        private Thread mainThread = null;
        private const int messagePriority = 3;
        private const int getMessagePriority = 2;

        private AsyncSocketClient asyncSocketClient;

        private int _maxSize = 1024;
        private int _maxConnnections = 10;

        public bool IsRunning = false;

        public void AddMSgRecordIntoBuffer(MsgRecordModel _msgRecordModel)
        {
            //if (!ThreadPool.QueueUserWorkItem(new WaitCallback(InputMessageTask), msgRecordModel))
            //{
            //    CommonVariables.LogTool.Log("Insert MsgRecordModel Thread Pool Failure");
            //}
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
                        break;
                    }
                }
                msgRecords.Add(msgRecordModel);
            }
            return msgRecords;
        }

        public void AddGetMsgIntoBuffer(GetMsgModel getMsgModel)
        {
            //if (!ThreadPool.QueueUserWorkItem(new WaitCallback(GetMessageTask), getMsgModel))
            //{
            //    CommonVariables.LogTool.Log("Insert GetMsgModel Thread Pool Failure");
            //}
            foreach (string mds_id in CommonVariables.GetMDSs.Keys)
            {
                if (CommonVariables.GetMDSs[mds_id].ArrangeChars.Contains(getMsgModel.ObjectID.Substring(0, 1)))
                {
                    getMsgModel.MDS_IP = CommonVariables.GetMDSs[mds_id].MDS_IP;
                    getMsgModel.MDS_Port = CommonVariables.GetMDSs[mds_id].MDS_Port;
                    getMsgModel.MDS_ID = CommonVariables.GetMDSs[mds_id].MDS_ID;
                    break;
                }
            }
            BufferGetMsgModels.Add(getMsgModel);
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
            ThreadStart threadStart = new ThreadStart(MainConnectMDSThread);

            Thread thread = new Thread(threadStart);

            thread.Start();
        }

        private void MainConnectMDSThread()
        {
            Socket tempSocket=null;
            int tempMessagePriority = messagePriority;
            int tempGetMessageProority = getMessagePriority;
            while (IsRunning)
            {

                if (tempMessagePriority >= tempGetMessageProority)
                {
                    //handler new message
                    #region get data from buffer
                    if (PerpareMsgRecordModels.Count == 0 && BufferMsgRecordModels.Count > 0)
                    {
                        lock (BufferMsgRecordModels)
                        {
                            foreach (MsgRecordModel msgRecordModel in BufferMsgRecordModels)
                            {
                                PerpareMsgRecordModels.Add(msgRecordModel);
                            }
                        }
                    }
                    #endregion

                    #region handler perpare area data

                    if (PerpareMsgRecordModels.Count > 0)
                    {
                        MsgRecordModel msgRecordModel = PerpareMsgRecordModels[0];
                        try
                        {

                            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(msgRecordModel.MDS_IP), msgRecordModel.MDS_Port);
                            tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            tempSocket.Connect(ipe);

                            var msgQuery = from aa in PerpareMsgRecordModels
                                           where aa.MDS_ID == msgRecordModel.MDS_ID
                                           select aa;

                            IList<MsgRecordModel> toMDSList = msgQuery.ToList();

                            
                            string messageStr="MCSMessage" + CommonVariables.serializer.Serialize(toMDSList);

                            byte[] buffer= new byte[_maxSize];
                            int bytesCount=Encoding.UTF8.GetBytes(messageStr,0,messageStr.Length,buffer,0);
                            tempSocket.Send(buffer, bytesCount, 0);
                            bytesCount = tempSocket.Receive(buffer);

                            messageStr=Encoding.UTF8.GetString(buffer, 0, bytesCount);
                            IList<string> retrunStrs= CommonVariables.serializer.Deserialize<IList<string>>(messageStr);

                            if(retrunStrs!=null && retrunStrs.Count>0)
                            {
                                msgQuery = from aa in PerpareMsgRecordModels
                                       join bb in retrunStrs on aa.MessageID equals bb
                                        select aa;

                                toMDSList=null;
                                toMDSList=msgQuery.ToList();
                                foreach(MsgRecordModel _msgRecordModel in toMDSList)
                                {
                                    PerpareMsgRecordModels.Remove(_msgRecordModel);
                                }
                            }
                        }
                        catch(Exception ex) {
                            CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
                            if(tempSocket!=null)
                            {
                                tempSocket.Close();
                                tempSocket= null;
                            }
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
                        lock (BufferGetMsgModels)
                        {
                            foreach (GetMsgModel getMsgModel in BufferGetMsgModels)
                            {
                                PerpareGetMsgModels.Add(getMsgModel);
                            }
                        }
                    }
                    #endregion

                    #region handler perpare area data
                    if (PerpareMsgRecordModels.Count > 0)
                    {
                        GetMsgModel getMsgModel = PerpareGetMsgModels[0];
                        try
                        {

                            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(getMsgModel.MDS_IP), getMsgModel.MDS_Port);
                            tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            tempSocket.Connect(ipe);

                            var msgQuery = from aa in PerpareGetMsgModels
                                           where aa.MDS_ID == getMsgModel.MDS_ID
                                           select aa;

                            IList<GetMsgModel> toMDSList = msgQuery.ToList();


                            string messageStr = "MCSMessage" + CommonVariables.serializer.Serialize(toMDSList);

                            byte[] buffer = new byte[_maxSize];
                            int bytesCount = Encoding.UTF8.GetBytes(messageStr, 0, messageStr.Length, buffer, 0);
                            tempSocket.Send(buffer, bytesCount, 0);
                            bytesCount = tempSocket.Receive(buffer);

                            messageStr = Encoding.UTF8.GetString(buffer, 0, bytesCount);
                            IList<MsgRecordModel> retrunStrs = CommonVariables.serializer.Deserialize<IList<MsgRecordModel>>(messageStr);

                            if (retrunStrs != null && retrunStrs.Count > 0)
                            {
                                foreach(MsgRecordModel msgRecordModel in retrunStrs)
                                {
                                    OutMsgRecordModels.Add(msgRecordModel);
                                }
                                
                            }
                        }
                        catch (Exception ex)
                        {
                            CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
                            if (tempSocket != null)
                            {
                                tempSocket.Close();
                                tempSocket = null;
                            }
                        }

                    }
                    #endregion

                    tempMessagePriority--;
                }
            }
        }

        private void MainConnectMDSThreadAsync(int threadcount)
        {
            Socket tempSocket = null;
            int tempMessagePriority = messagePriority;
            int tempGetMessageProority = getMessagePriority;
            asyncSocketClient=new AsyncSocketClient(_maxSize,_maxConnnections,CommonVariables.LogTool);
            while (IsRunning)
            {

                if (tempMessagePriority >= tempGetMessageProority)
                {
                    //handler new message
                    #region get data from buffer
                    if (PerpareMsgRecordModels.Count == 0 && BufferMsgRecordModels.Count > 0)
                    {
                        lock (BufferMsgRecordModels)
                        {
                            foreach (MsgRecordModel msgRecordModel in BufferMsgRecordModels)
                            {
                                PerpareMsgRecordModels.Add(msgRecordModel);
                            }
                        }
                    }
                    #endregion

                    #region handler perpare area data

                    if (PerpareMsgRecordModels.Count > 0)
                    {
                        MsgRecordModel msgRecordModel = PerpareMsgRecordModels[0];
                        try
                        {
                            string messageStr = "MCSMessage" + CommonVariables.serializer.Serialize(msgRecordModel);

                            asyncSocketClient.SendMsg(msgRecordModel.MDS_IP,msgRecordModel.MDS_Port, messageStr, HandlerMsgReturnData);
                        }
                        catch (Exception ex)
                        {
                            CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
                            if (tempSocket != null)
                            {
                                tempSocket.Close();
                                tempSocket = null;
                            }
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
                        lock (BufferGetMsgModels)
                        {
                            foreach (GetMsgModel getMsgModel in BufferGetMsgModels)
                            {
                                PerpareGetMsgModels.Add(getMsgModel);
                            }
                        }
                    }
                    #endregion

                    #region handler perpare area data
                    if (PerpareMsgRecordModels.Count > 0)
                    {
                        GetMsgModel getMsgModel = PerpareGetMsgModels[0];
                        try
                        {

                            IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(getMsgModel.MDS_IP), getMsgModel.MDS_Port);
                            tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                            tempSocket.Connect(ipe);

                            string messageStr = "MCSMessage" + CommonVariables.serializer.Serialize(getMsgModel);

                            byte[] buffer = new byte[_maxSize];
                            int bytesCount = Encoding.UTF8.GetBytes(messageStr, 0, messageStr.Length, buffer, 0);
                            tempSocket.Send(buffer, bytesCount, 0);
                            bytesCount = tempSocket.Receive(buffer);

                            messageStr = Encoding.UTF8.GetString(buffer, 0, bytesCount);
                            IList<MsgRecordModel> retrunStrs = CommonVariables.serializer.Deserialize<IList<MsgRecordModel>>(messageStr);

                            if (retrunStrs != null && retrunStrs.Count > 0)
                            {
                                foreach (MsgRecordModel msgRecordModel in retrunStrs)
                                {
                                    OutMsgRecordModels.Add(msgRecordModel);
                                }

                            }
                        }
                        catch (Exception ex)
                        {
                            CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
                            if (tempSocket != null)
                            {
                                tempSocket.Close();
                                tempSocket = null;
                            }
                        }

                    }
                    #endregion

                    tempMessagePriority--;
                }
            }
        }


        private void HandlerMsgReturnData(string returnData)
        {
            IList<string> retrunStrs = CommonVariables.serializer.Deserialize<IList<string>>(returnData);

            if (retrunStrs != null && retrunStrs.Count > 0)
            {
                var msgQuery = from aa in PerpareMsgRecordModels
                           join bb in retrunStrs on aa.MessageID equals bb
                           select aa;
                IList<MsgRecordModel> toMDSList = msgQuery.ToList();
                foreach (MsgRecordModel _msgRecordModel in toMDSList)
                {
                    PerpareMsgRecordModels.Remove(_msgRecordModel);
                }
            }
        }


        private void HandlerGetMsgReturnData(string returnData)
        {

        }
    }

}

