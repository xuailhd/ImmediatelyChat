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
using Xugl.ImmediatelyChat.Model.QueryCondition;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageDataServer
{
    public class BufferContorl
    {
        private readonly int maxBufferRecordCount;

        private IList<MsgRecord> bufferMsgRecords1 = new List<MsgRecord>();

        private IList<MsgRecord> bufferMsgRecords2 = new List<MsgRecord>();

        bool Isbuffer1inUsed = true;

        private Thread mainThread = null;

        /// <summary>
        /// which time before or equal this time, will be save into database
        /// </summary>
        private DateTime savedIntoDataBase;

        private readonly IMsgRecordService msgRecordService;
        public bool IsRunning = false;


        public BufferContorl()
        {
            msgRecordService = ObjectContainerFactory.CurrentContainer.Resolver<IMsgRecordService>();
            maxBufferRecordCount = 1000;
        }


        public void AddMSgRecordIntoBuffer(MsgRecordModel msgRecordModel)
        {
            MsgRecord msgRecord = new MsgRecord();
            msgRecord.GroupID = msgRecordModel.RecivedGroupID;
            msgRecord.MsgContent = msgRecordModel.Content;
            msgRecord.MsgID = msgRecordModel.MessageID;
            msgRecord.MsgRecipientObjectID = msgRecordModel.RecivedObjectID;
            msgRecord.MsgSenderName = msgRecordModel.ObjectName;
            msgRecord.MsgSenderObjectID = msgRecordModel.ObjectID;
            msgRecord.MsgType = msgRecordModel.MsgType;
            msgRecord.SendTime = msgRecordModel.SendTime;
            GetUsingBufferContainer.Add(msgRecord);
        }


        private IList<MsgRecord> GetUsingBufferContainer
        {
            get{
                return Isbuffer1inUsed ? bufferMsgRecords1 : bufferMsgRecords2;
            }
        }

        private IList<MsgRecord> GetUnUsingBufferContainer
        {
            get
            {
                return Isbuffer1inUsed ? bufferMsgRecords2 : bufferMsgRecords1;
            }
        }

        public IList<MsgRecord> GetMSG(GetMsgModel getMsgModel)
        {

            IList<MsgRecord> msgRecords=new List<MsgRecord>();

            if(DateTime.Compare(getMsgModel.LatestTime,savedIntoDataBase)<=0)
            {
                MsgRecordQuery query = new MsgRecordQuery();
                query.MsgRecipientObjectID = getMsgModel.ObjectID;
                query.MsgRecordtime = getMsgModel.LatestTime;

                msgRecords.Union(msgRecordService.LoadMsgRecord(query).OrderBy(t=>t.SendTime));
            }

            msgRecords.Union(GetUsingBufferContainer.Where(t => DateTime.Compare(t.SendTime, getMsgModel.LatestTime) <= 0
                && t.MsgRecipientObjectID == getMsgModel.ObjectID).OrderBy(t=>t.SendTime));

            return msgRecords;
        }

        public void StartMainThread()
        {
            IsRunning = true;
            ThreadStart threadStart = new ThreadStart(MainSaveRecordThread);
            Thread thread = new Thread(threadStart);
            thread.Start();
        }

        public void StopMainThread()
        {
            IsRunning = false;
        }

        private void MainSaveRecordThread()
        {
            CommonVariables.LogTool.Log("begin buffer contorl");

            while(IsRunning)
            {
                if (GetUsingBufferContainer.Count >= maxBufferRecordCount)
                {
                    Isbuffer1inUsed = !Isbuffer1inUsed;

                    msgRecordService.BatchSave(GetUnUsingBufferContainer);

                    savedIntoDataBase = GetUnUsingBufferContainer.Select(t => t.SendTime).Max();

                    GetUnUsingBufferContainer.Clear();
                }
                Thread.Sleep(1000);
            }
        }


    }

}

