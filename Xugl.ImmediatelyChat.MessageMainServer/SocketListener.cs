﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Common;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Core.DependencyResolution;
using Xugl.ImmediatelyChat.IServices;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageMainServer
{


    public class MMSListenerToken : AsyncUserToken
    {
        private readonly IContactPersonService _contactPersonService;

        public MMSListenerToken()
        {
            _contactPersonService=ObjectContainerFactory.CurrentContainer.Resolver<IContactPersonService>();
        }

        public IList<ContactData> Models { get; set; }

        public string UAObjectID { get; set; }

        public IContactPersonService ContactPersonService
        {
            get
            {
                return _contactPersonService;
            }
        }
    }

    internal class SocketListener : AsyncSocketListener<MMSListenerToken>
    {
        public SocketListener()
            : base(1024, 100, CommonVariables.LogTool)
        {
        }

        protected override void HandleError(MMSListenerToken token)
        {
            if (token.Models != null && token.Models.Count > 0)
            {
                token.Models.Clear();
                token.Models = null;
            }
        }
        protected override string HandleRecivedMessage(string inputMessage, MMSListenerToken token)
        {
            //MCSModel tempMCSModel = null;
            //MDSModel tempMDSModel = null;
            



            if (string.IsNullOrEmpty(inputMessage) || token == null)
            {
                return string.Empty;
            }

            string data = inputMessage;


            if (data.StartsWith(CommonFlag.F_PSSendMMSUser))
            {
                return HandlePSSendMMSUser(data, token);
            }

            if (data.StartsWith(CommonFlag.F_PSCallMMSStart))
            {
                return HandlePSCallMMSStart(data);
            }

            if (CommonVariables.IsBeginMessageService)
            {
                //UA
                if (data.StartsWith(CommonFlag.F_MMSVerifyUA))
                {
                    return HandleMMSVerifyUA(data, token);
                }

                if(data.StartsWith(CommonFlag.F_MMSVerifyUAGetUAInfo))
                {
                    return HandleMMSVerifyUAGetUAInfo(data, token);
                }

                if(data.StartsWith(CommonFlag.F_MMSVerifyFBUAGetUAInfo))
                {
                    return HandleMMSVerifyFBUAGetUAInfo(data, token);
                }

                if(data.StartsWith(CommonFlag.F_MMSVerifyUASearch))
                {
                    return HandleMMSVerifyUASearch(data, token);
                }

                if(data.StartsWith(CommonFlag.F_MMSVerifyUAFBSearch))
                {
                    return HandleMMSVerifyUAFBSearch(data, token);
                }

                if(data.StartsWith(CommonFlag.F_MMSVerifyUAAddPerson))
                {
                    return HandleMMSVerifyUAAddPerson(data, token);
                }

                if(data.StartsWith(CommonFlag.F_MMSVerifyUAAddGroup))
                {
                    return HandleMMSVerifyUAAddGroup(data, token);
                }
            }
            return string.Empty;
        }


        private string HandleMMSVerifyUAAddGroup(string data, MMSListenerToken token)
        {
            ClientAddGroup model = CommonVariables.serializer.Deserialize<ClientAddGroup>(data.Remove(0, CommonFlag.F_MMSVerifyUAAddGroup.Length));
            if (model != null && !string.IsNullOrEmpty(model.ObjectID))
            {
                if (token.ContactPersonService.FindContactGroupSub(model.ObjectID, model.GroupObjectID) != null)
                {
                    model.Status = 1;
                    return CommonVariables.serializer.Serialize(model);
                }

                ContactPerson contactPerson = token.ContactPersonService.FindContactPerson(model.ObjectID);
                if (contactPerson != null)
                {
                    ContactGroup contactGroup = token.ContactPersonService.FindContactGroup(model.GroupObjectID);
                    if (contactGroup != null)
                    {
                        ContactGroupSub contactGroupSub = new ContactGroupSub();
                        contactGroupSub.ContactGroupID = contactGroup.GroupObjectID;
                        contactGroupSub.ContactPersonObjectID = contactGroupSub.ContactPersonObjectID;
                        contactGroupSub.UpdateTime = DateTime.Now.ToString(CommonFlag.F_DateTimeFormat);

                        if (token.ContactPersonService.InsertContactGroupSub(contactGroupSub) == 1)
                        {
                            contactPerson.UpdateTime = contactGroupSub.UpdateTime;
                            token.ContactPersonService.UpdateContactPerson(contactPerson);

                            ContactData contactData = new ContactData();
                            contactData.ContactGroupID = contactGroupSub.ContactGroupID;
                            contactData.ContactPersonObjectID = contactGroupSub.ContactPersonObjectID;
                            contactData.UpdateTime = contactGroupSub.UpdateTime;
                            contactData.DataType = 3;
                            CommonVariables.SyncSocketClientIntance.SendMsg(model.MCS_IP, model.MCS_Port,
                            CommonFlag.F_MCSReceiveUAInfo + CommonVariables.serializer.Serialize(contactData));

                            model.Status = 0;
                            return CommonVariables.serializer.Serialize(model);
                        }

                    }
                }
            }
            model.Status = 2;
            return CommonVariables.serializer.Serialize(model);
        }

        private string HandleMMSVerifyUAAddPerson(string data, MMSListenerToken token)
        {
            ClientAddPerson clientAddPerson = CommonVariables.serializer.Deserialize<ClientAddPerson>(data.Remove(0, CommonFlag.F_MMSVerifyUAAddPerson.Length));
            if( clientAddPerson!=null && !string.IsNullOrEmpty(clientAddPerson.ObjectID))
            {
                if(token.ContactPersonService.FindContactPersonList(clientAddPerson.ObjectID,clientAddPerson.DestinationObjectID)!=null)
                {
                    clientAddPerson.Status=1;
                    return CommonVariables.serializer.Serialize(clientAddPerson);
                }

                ContactPerson contactPerson = token.ContactPersonService.FindContactPerson(clientAddPerson.ObjectID);
                if(contactPerson!=null)
                {
                    ContactPerson contactPerson2 = token.ContactPersonService.FindContactPerson(clientAddPerson.DestinationObjectID);
                    if(contactPerson2!=null)
                    {
                        ContactPersonList contactPersonList = new ContactPersonList();
                        contactPersonList.ContactPersonName = contactPerson2.ContactName;
                        contactPersonList.DestinationObjectID = contactPerson2.ObjectID;
                        contactPersonList.ObjectID = contactPerson.ObjectID;
                        contactPersonList.UpdateTime = DateTime.Now.ToString(CommonFlag.F_DateTimeFormat);

                        if( token.ContactPersonService.InsertContactPersonList(contactPersonList)==1)
                        {
                            contactPerson.UpdateTime = contactPersonList.UpdateTime;
                            token.ContactPersonService.UpdateContactPerson(contactPerson);

                            ContactData contactData = new ContactData();
                            contactData.ContactPersonName = contactPersonList.ContactPersonName;
                            contactData.ContactPersonObjectID = contactPersonList.DestinationObjectID;
                            contactData.ObjectID = contactPersonList.ObjectID;
                            contactData.UpdateTime = contactPersonList.UpdateTime;
                            contactData.DataType = 1;

                            CommonVariables.SyncSocketClientIntance.SendMsg(clientAddPerson.MCS_IP, clientAddPerson.MCS_Port,
                            CommonFlag.F_MCSReceiveUAInfo + CommonVariables.serializer.Serialize(contactData));

                            clientAddPerson.Status = 0;
                            return CommonVariables.serializer.Serialize(clientAddPerson);
                        }

                    }
                }
            }
            clientAddPerson.Status = 2;
            return CommonVariables.serializer.Serialize(clientAddPerson);
        }

        private string HandlePSSendMMSUser(string data, MMSListenerToken token)
        {
            ContactPerson contactPerson = CommonVariables.serializer.Deserialize<ContactPerson>(data.Remove(0, CommonFlag.F_PSSendMMSUser.Length));
            if (contactPerson != null && !string.IsNullOrEmpty(contactPerson.ObjectID))
            {
                if (token.ContactPersonService.InsertNewPerson(contactPerson) > 0)
                {
                    if (token.ContactPersonService.InsertDefaultGroup(contactPerson.ObjectID) > 0)
                    {
                        return contactPerson.ObjectID;
                    }
                }
            }
            return "failed";
        }

        private string HandleMMSVerifyUAFBSearch(string data, MMSListenerToken token)
        {
            string contactDataID = data.Remove(0, CommonFlag.F_MMSVerifyUAFBSearch.Length);
            CommonVariables.LogTool.Log("UA F_MMSVerifyUAFBSearch:" + contactDataID);
            if (token.Models[0].ContactDataID != contactDataID)
            {
                CommonVariables.LogTool.Log("Search data transfer error " + token.Models[0].ContactDataID + "  vs " + contactDataID);
            }
            token.Models.RemoveAt(0);
            if (token.Models.Count <= 0)
            {
                return string.Empty;
            }
            return CommonVariables.serializer.Serialize(token.Models[0]);
        }

        private string HandleMMSVerifyUASearch(string data, MMSListenerToken token)
        {
            ClientSearchModel clientSearchModel = CommonVariables.serializer.Deserialize<ClientSearchModel>(data.Remove(0, CommonFlag.F_MMSVerifyUASearch.Length));
            CommonVariables.LogTool.Log("UA:" + clientSearchModel.ObjectID + " Search request  " + clientSearchModel.SearchKey);
            if (clientSearchModel != null && !string.IsNullOrEmpty(clientSearchModel.ObjectID))
            {
                if (clientSearchModel.Type == 1)
                {
                    token.Models = ContactPersonToContacData(token.ContactPersonService.SearchPerson(clientSearchModel.SearchKey));
                }
                else if (clientSearchModel.Type == 2)
                {
                    token.Models = ContactGroupToContacData(token.ContactPersonService.SearchGroup(clientSearchModel.SearchKey));
                }

                CommonVariables.LogTool.Log("token.Models count:" + token.Models.Count);
                if (token.Models != null && token.Models.Count > 0)
                {
                    return CommonVariables.serializer.Serialize(token.Models[0]);
                }
            }

            return string.Empty;
        }

        private string HandleMMSVerifyFBUAGetUAInfo(string data, MMSListenerToken token)
        {
            string contactDataID = data.Remove(0, CommonFlag.F_MMSVerifyFBUAGetUAInfo.Length);
            if (token.Models[0].ContactDataID != contactDataID)
            {
                CommonVariables.LogTool.Log("data transfer error " + token.Models[0].ContactDataID + "  vs " + contactDataID);
            }
            token.Models.RemoveAt(0);
            if (token.Models.Count <= 0)
            {
                return string.Empty;
            }
            return CommonVariables.serializer.Serialize(token.Models[0]);
        }

        private string HandleMMSVerifyUAGetUAInfo(string data, MMSListenerToken token)
        {
            ClientStatusModel clientStatusModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(data.Remove(0, CommonFlag.F_MMSVerifyUAGetUAInfo.Length));

            if (clientStatusModel == null)
            {
                return string.Empty;
            }

            if (string.IsNullOrEmpty(clientStatusModel.ObjectID) || string.IsNullOrEmpty(clientStatusModel.UpdateTime))
            {
                return string.Empty;
            }

            CommonVariables.LogTool.Log("UA:" + clientStatusModel.ObjectID + " UAInfo request  " + clientStatusModel.UpdateTime);

            token.Models = CommonVariables.UAInfoContorl.PreparContactData(clientStatusModel.ObjectID, clientStatusModel.UpdateTime);

            return CommonVariables.serializer.Serialize(token.Models[0]);
        }

        private string HandleMMSVerifyUA(string data, MMSListenerToken token)
        {
            ContactPerson tempContactPerson = null;
            ClientStatusModel clientStatusModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(data.Remove(0, CommonFlag.F_MMSVerifyUA.Length));

            CommonVariables.LogTool.Log("UA:" + clientStatusModel.ObjectID + " connected  " + CommonVariables.MCSServers.Count);
            //Find MCS
            for (int i = 0; i < CommonVariables.MCSServers.Count; i++)
            {
                if (CommonVariables.MCSServers[i].ArrangeStr.Contains(clientStatusModel.ObjectID.Substring(0, 1)))
                {
                    clientStatusModel.MCS_IP = CommonVariables.MCSServers[i].MCS_IP;
                    clientStatusModel.MCS_Port = CommonVariables.MCSServers[i].MCS_Port;

                    tempContactPerson = token.ContactPersonService.FindContactPerson(t => t.ObjectID == clientStatusModel.ObjectID);
                    if (tempContactPerson == null)
                    {
                        return string.Empty;
                    }

                    if (clientStatusModel.LatestTime.CompareTo(tempContactPerson.LatestTime) <= 0)
                    {
                        clientStatusModel.LatestTime = tempContactPerson.LatestTime;
                    }
                    else
                    {
                        tempContactPerson.LatestTime = clientStatusModel.LatestTime;
                        token.ContactPersonService.UpdateContactPerson(tempContactPerson);
                    }

                    clientStatusModel.UpdateTime = tempContactPerson.UpdateTime;

                    string mcs_UpdateTime = CommonVariables.SyncSocketClientIntance.SendMsg(clientStatusModel.MCS_IP, clientStatusModel.MCS_Port,
                        CommonFlag.F_MCSReceiveMMSUAUpdateTime + CommonVariables.serializer.Serialize(clientStatusModel));

                    if (string.IsNullOrEmpty(mcs_UpdateTime))
                    {
                        return string.Empty;
                    }

                    IList<ContactData> contactDatas = CommonVariables.UAInfoContorl.PreparContactData(clientStatusModel.ObjectID, mcs_UpdateTime);

                    foreach (ContactData contactData in contactDatas)
                    {
                        CommonVariables.SyncSocketClientIntance.SendMsg(clientStatusModel.MCS_IP, clientStatusModel.MCS_Port,
                            CommonFlag.F_MCSReceiveUAInfo + CommonVariables.serializer.Serialize(contactData));
                    }

                    //CommonVariables.UAInfoContorl.AddUAModelIntoBuffer(clientStatusModel);
                    break;
                }
            }

            //Send MCS
            return CommonVariables.serializer.Serialize(clientStatusModel);
        }

        private string HandlePSCallMMSStart(string data)
        {
            IList<MCSServer> mcsServers = CommonVariables.serializer.Deserialize<IList<MCSServer>>(data.Remove(0, CommonFlag.F_PSCallMMSStart.Length));
            CommonVariables.LogTool.Log("MCS count:" + mcsServers.Count);
            foreach (MCSServer mcsServer in mcsServers)
            {
                CommonVariables.MCSServers.Add(mcsServer);
                CommonVariables.LogTool.Log("IP:" + mcsServer.MCS_IP + " Port:" + mcsServer.MCS_Port + "  ArrangeStr:" + mcsServer.ArrangeStr);
            }

            CommonVariables.LogTool.Log("Start Message Main Server:" + CommonVariables.MMSIP + ", Port:" + CommonVariables.MMSPort.ToString());
            CommonVariables.IsBeginMessageService = true;
            return string.Empty;
        }

        private IList<ContactData> ContactPersonToContacData(IList<ContactPerson> entitys)
        {
            if (entitys != null && entitys.Count>0)
            {
                ContactData contactData;
                IList<ContactData> contactDatas = new List<ContactData>();

                foreach(ContactPerson entity in entitys )
                {
                    contactData = new ContactData();
                    contactData.ContactDataID = Guid.NewGuid().ToString();
                    contactData.ContactName = entity.ContactName;
                    contactData.ObjectID = entity.ObjectID;
                    contactData.ImageSrc = entity.ImageSrc;
                    contactDatas.Add(contactData);
                }

                return contactDatas;
            }

            return null;
        }

        private IList<ContactData> ContactGroupToContacData(IList<ContactGroup> entitys)
        {
            if (entitys != null && entitys.Count > 0)
            {
                ContactData contactData;
                IList<ContactData> contactDatas = new List<ContactData>();

                foreach (ContactGroup entity in entitys)
                {
                    contactData = new ContactData();
                    contactData.ContactDataID = Guid.NewGuid().ToString();
                    contactData.GroupObjectID = entity.GroupObjectID;
                    contactData.GroupName = entity.GroupName;
                    contactDatas.Add(contactData);
                }

                return contactDatas;
            }

            return null;
        }


        public void BeginService()
        {
            CommonVariables.UAInfoContorl.StartMainThread();
            base.BeginService(CommonVariables.MMSIP,CommonVariables.MMSPort);
        }

    }
}
