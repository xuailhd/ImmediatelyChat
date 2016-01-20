using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Script.Serialization;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Core.DependencyResolution;
using Xugl.ImmediatelyChat.IServices;
using Xugl.ImmediatelyChat.Model;

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

        private Thread mainThread = null;

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
                            break;
                        }
                    }
                }

            }
            else
            {
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

        private void MainConnectMDSThread()
        {
            Socket tempSocket;

            while (IsRunning)
            {

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

                if (PerpareMsgRecordModels.Count > 0)
                {
                    MsgRecordModel msgRecordModel = PerpareMsgRecordModels[0];

                    IPEndPoint ipe = new IPEndPoint(IPAddress.Parse(msgRecordModel.MDS_IP), msgRecordModel.MDS_Port);
                    tempSocket = new Socket(ipe.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                    tempSocket.Connect(ipe);

                    byte[] bytesSent = Encoding.UTF8.GetBytes("MDS" + CommonVariables.serializer.Serialize(msgRecordModel));
                    tempSocket.Send(bytesSent, bytesSent.Length, 0);
                }

            }

        }
    }


}

