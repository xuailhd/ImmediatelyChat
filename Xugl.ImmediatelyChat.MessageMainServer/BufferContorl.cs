using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
//using System.ServiceModel.Channels;
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

        private IList<ContactDataWithMCS> BufferUAModels1 = new List<ContactDataWithMCS>();
        private IList<ContactDataWithMCS> BufferUAModels2 = new List<ContactDataWithMCS>();

        private IList<ContactDataWithMCS> ExeingUAModels = new List<ContactDataWithMCS>();

        private bool UsingTagForUAMode = false;

        private AsyncSocketClient asyncSocketClient;
        private readonly IContactPersonService contactPersonService;

        private int _maxSize = 1024;
        private int _maxConnnections = 10;

        public bool IsRunning = false;


        public BufferContorl()
        {
            contactPersonService = ObjectContainerFactory.CurrentContainer.Resolver<IContactPersonService>();
        }


        /// <summary>
        /// send UA's update time to MCS,verify whether need send UA's information to MCS
        /// </summary>
        /// <param name="clientStatusModel"></param>
        public void AddUAModelIntoBuffer(ClientStatusModel clientStatusModel)
        {
            if (asyncSocketClient == null)
            {
                return;
            }

            asyncSocketClient.SendMsg(clientStatusModel.MCS_IP, clientStatusModel.MCS_Port, CommonFlag.F_MCSReceiveMMSUAUpdateTime + CommonVariables.serializer.Serialize(clientStatusModel),
                 clientStatusModel.ObjectID, HandlerUpdateTimeReturnData);
        }


        private string HandlerUpdateTimeReturnData(string returnData, bool IsError)
        {
            if (IsError)
            {
                return string.Empty;
            }

            ClientStatusModel clientStatusModel = null;
            ContactDataWithMCS contactDataWithMCS = null;
            try
            {

                clientStatusModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(returnData);
            }
            catch
            {

            }

            if (clientStatusModel != null)
            {
                ContactPerson contactPerson = contactPersonService.FindContactPerson(clientStatusModel.ObjectID);

                if (contactPerson==null)
                {
                    return string.Empty;
                }

                if (contactPerson.UpdateTime.CompareTo(clientStatusModel.UpdateTime) > 0)
                {
                    IList<ContactData> contactDatas = PreparContactData(clientStatusModel.ObjectID, clientStatusModel.UpdateTime).OrderBy(t=>t.UpdateTime).ToList();

                    foreach (ContactData contactData in contactDatas)
                    {
                        contactDataWithMCS = new ContactDataWithMCS();
                        contactDataWithMCS.ContactData = contactData;
                        contactDataWithMCS.MCS_IP = clientStatusModel.MCS_IP;
                        contactDataWithMCS.MCS_Port = clientStatusModel.MCS_Port;
                        GetUsingUAModelBuffer.Add(contactDataWithMCS);
                    }
                }
            }
            return string.Empty;
        }

        #region using unusing buffer manage

        private IList<ContactDataWithMCS> GetUsingUAModelBuffer
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

        private IList<ContactDataWithMCS> GetUnUsingUAModelBuffer
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

        public IList<ContactData> PreparContactData(string objectID, string updateTime)
        {
            IList<ContactData> tempContactDatas = null;
            ContactData tempContactData;

            IList<ContactPersonList> contactPersonLists = contactPersonService.GetLastestContactPersonList(objectID, updateTime);
            IList<ContactGroup> contactGroups = contactPersonService.GetLastestContactGroup(objectID, updateTime);
            IList<ContactGroupSub> contactGroupSubs = contactPersonService.GetLastestContactGroupSub(objectID, updateTime);

            tempContactDatas = new List<ContactData>();

            if (contactPersonLists != null && contactPersonLists.Count > 0)
            {
                foreach (ContactPersonList contactPersonList in contactPersonLists)
                {
                    tempContactData = new ContactData();

                    tempContactData.DestinationObjectID = contactPersonList.DestinationObjectID;
                    tempContactData.ContactPersonName = contactPersonList.ContactPersonName;
                    tempContactData.ObjectID = contactPersonList.ObjectID;
                    tempContactData.IsDelete = contactPersonList.IsDelete;
                    tempContactData.UpdateTime = contactPersonList.UpdateTime;
                    tempContactData.ContactDataID = Guid.NewGuid().ToString();
                    tempContactData.DataType = 1;
                    tempContactDatas.Add(tempContactData);
                }
            }
            if (contactGroups != null && contactGroups.Count > 0)
            {
                foreach (ContactGroup contactGroup in contactGroups)
                {
                    tempContactData = new ContactData();
                    tempContactData.GroupObjectID = contactGroup.GroupObjectID;
                    tempContactData.GroupName = contactGroup.GroupName;
                    tempContactData.IsDelete = contactGroup.IsDelete;
                    tempContactData.UpdateTime = contactGroup.UpdateTime;
                    tempContactData.ContactDataID = Guid.NewGuid().ToString();
                    tempContactData.DataType = 2;
                    tempContactData.ObjectID = objectID;
                    tempContactDatas.Add(tempContactData);
                }
            }
            if (contactGroupSubs != null && contactGroupSubs.Count > 0)
            {
                foreach (ContactGroupSub contactGroupSub in contactGroupSubs)
                {
                    tempContactData = new ContactData();
                    tempContactData.ContactGroupID = contactGroupSub.ContactGroupID;
                    tempContactData.ContactPersonObjectID = contactGroupSub.ContactPersonObjectID;
                    tempContactData.IsDelete = contactGroupSub.IsDelete;
                    tempContactData.UpdateTime = contactGroupSub.UpdateTime;
                    tempContactData.ContactDataID = Guid.NewGuid().ToString();
                    tempContactData.DataType = 3;
                    tempContactData.ObjectID = objectID;
                    tempContactDatas.Add(tempContactData);
                }
            }
            return tempContactDatas;
        }

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
                            ContactDataWithMCS contactDataWithMCS = GetUnUsingUAModelBuffer[0];
                            try
                            {
                                string messageStr = CommonFlag.F_MCSReceiveUAInfo + CommonVariables.serializer.Serialize(contactDataWithMCS.ContactData);
                                //CommonVariables.LogTool.Log("begin send mds " + msgRecordModel.MDS_IP + " port:" + msgRecordModel.MDS_Port + messageStr);
                                asyncSocketClient.SendMsg(contactDataWithMCS.MCS_IP, contactDataWithMCS.MCS_Port, messageStr, contactDataWithMCS.ContactData.ContactDataID, HandlerMsgReturnData);

                                ExeingUAModels.Add(contactDataWithMCS);
                            }
                            catch (Exception ex)
                            {
                                GetUsingUAModelBuffer.Add(contactDataWithMCS);
                                CommonVariables.LogTool.Log(contactDataWithMCS.ContactData.ObjectID + ex.Message + ex.StackTrace);
                            }
                            GetUnUsingUAModelBuffer.RemoveAt(0);
                        }
                    }
                    Thread.Sleep(1000);
                }
            }
            catch(Exception ex)
            {
                CommonVariables.LogTool.Log(ex.Message + ex.StackTrace);
            }
        }



        private string HandlerMsgReturnData(string returnData, bool IsError)
        {
            if (!string.IsNullOrEmpty(returnData))
            {
                ContactDataWithMCS tempmodel =  ExeingUAModels.FirstOrDefault(t => t.ContactData.ContactDataID == returnData);

                if (tempmodel != null)
                {
                    if (IsError)
                    {
                        //GetUsingUAModelBuffer.Add(tempmodel);
                    }
                    ExeingUAModels.Remove(tempmodel);
                }
            }
            return string.Empty;
        }

    }

}

