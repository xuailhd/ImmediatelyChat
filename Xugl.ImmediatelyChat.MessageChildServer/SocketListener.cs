using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Common;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Core.DependencyResolution;
using Xugl.ImmediatelyChat.IServices;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageChildServer
{
    public class MCSListenerToken : AsyncUserToken
    {
        private readonly IContactPersonService _contactPersonService;

        public MCSListenerToken()
        {
            _contactPersonService = ObjectContainerFactory.CurrentContainer.Resolver<IContactPersonService>();
        }

        public IList<MsgRecord> Models { get; set; }

        public string UAObjectID { get; set; }

        public IContactPersonService ContactPersonService
        {
            get
            {
                return _contactPersonService;
            }
        }
    }

    internal class SocketListener : AsyncSocketListener<MCSListenerToken>
    {
        public SocketListener()
            : base(1024, 100, CommonVariables.LogTool)
        {
        }

        protected override void HandleError(MCSListenerToken token)
        {
            if (token.Models != null && token.Models.Count > 0)
            {
                CommonVariables.MessageContorl.ReturnMsg(token.Models);
                CommonVariables.InRunningUAList.Remove(token.UAObjectID);
                token.Models.Clear();
                token.Models = null;
                token.UAObjectID = string.Empty;
            }
        }

        protected override string HandleRecivedMessage(string inputMessage, MCSListenerToken token)
        {
            if (string.IsNullOrEmpty(inputMessage) || token == null)
            {
                return string.Empty;
            }

            string data = inputMessage;

            if (data.StartsWith(CommonFlag.F_PSCallMCSStart))
            {
                data = data.Remove(0, CommonFlag.F_PSCallMCSStart.Length);
                IList<MDSServer> mdsServers = CommonVariables.serializer.Deserialize<IList<MDSServer>>(data.Substring(0, data.IndexOf("&&")));

                if (mdsServers != null && mdsServers.Count > 0)
                {
                    data = data.Remove(0, data.IndexOf("&&") + 2);
                    CommonVariables.ArrangeStr = CommonVariables.serializer.Deserialize<MCSServer>(data).ArrangeStr;
                    CommonVariables.OperateFile.SaveConfig(CommonVariables.ConfigFilePath, "ArrangeStr", CommonVariables.ArrangeStr);
                    CommonVariables.LogTool.Log("ArrangeStr:" + CommonVariables.ArrangeStr);
                    CommonVariables.LogTool.Log("MDS count:" + mdsServers.Count);
                    foreach (MDSServer mdsServer in mdsServers)
                    {
                        CommonVariables.MDSServers.Add(mdsServer);
                        CommonVariables.LogTool.Log("IP:" + mdsServer.MDS_IP + " Port:" + mdsServer.MDS_Port + "  ArrangeStr:" + mdsServer.ArrangeStr);
                    }
                    CommonVariables.LogTool.Log("Start MCS service:" + CommonVariables.MCSIP + ", Port:" + CommonVariables.MCSPort.ToString());
                    CommonVariables.IsBeginMessageService = true;
                }
                return string.Empty;
            }

            if (CommonVariables.IsBeginMessageService)
            {
                //handle UA feedback
                if (data.StartsWith(CommonFlag.F_MCSReceiveUAFBMSG))
                {
                    string tempStr = data.Remove(0, CommonFlag.F_MCSReceiveUAFBMSG.Length);
                    if (token.Models != null && token.Models.Count > 0)
                    {
                        if (token.Models[0].MsgID == tempStr)
                        {
                            token.Models.RemoveAt(0);
                        }
                        else
                        {
                            for (int i = 1; i < token.Models.Count; i++)
                            {
                                if (token.Models[i].MsgID == tempStr)
                                {
                                    token.Models.RemoveAt(i);
                                    break;
                                }
                            }
                        }
                    }

                    if (token.Models != null && token.Models.Count > 0)
                    {
                        return CommonVariables.serializer.Serialize(token.Models[0]);
                    }
                    else
                    {
                        return string.Empty;
                    }

                }

                if (data.StartsWith(CommonFlag.F_MCSVerifyUA))
                {
                    string tempStr = data.Remove(0, CommonFlag.F_MCSVerifyUA.Length);
                    ClientStatusModel clientModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(tempStr);
                    if (clientModel != null)
                    {
                        if (!string.IsNullOrEmpty(clientModel.ObjectID))
                        {
                            CommonVariables.LogTool.Log("F_MCSVerifyUA :" + clientModel.ObjectID + " time:" + clientModel.UpdateTime);
                            ContactPerson contactPerson= token.ContactPersonService.FindContactPerson(clientModel.ObjectID);
                            if(contactPerson!=null)
                            {
                                if(contactPerson.UpdateTime.CompareTo(clientModel.UpdateTime)==0)
                                {
                                    return "ok";
                                }

                            }
                            return "wait";
                        }
                    }
                }


                if(data.StartsWith(CommonFlag.F_MCSReceiveMMSUAUpdateTime))
                {
                    ClientStatusModel clientModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(data.Remove(0, CommonFlag.F_MCSReceiveMMSUAUpdateTime.Length));
                    ContactPerson contactPerson = token.ContactPersonService.FindContactPerson(clientModel.ObjectID);

                    if(contactPerson==null)
                    {
                        contactPerson = new ContactPerson();
                        contactPerson.ObjectID = clientModel.ObjectID;
                        contactPerson.LatestTime = clientModel.LatestTime;
                        contactPerson.UpdateTime = CommonFlag.F_MinDatetime;
                        token.ContactPersonService.InsertNewPerson(contactPerson);
                    }
                    //clientModel.UpdateTime = contactPerson.UpdateTime;
                    return contactPerson.UpdateTime;
                }

                if(data.StartsWith(CommonFlag.F_MCSReceiveUAInfo))
                {
                    ContactData contactData = CommonVariables.serializer.Deserialize<ContactData>(data.Remove(0, CommonFlag.F_MCSReceiveUAInfo.Length));

                    if (string.IsNullOrEmpty(contactData.ContactDataID))
                    {
                        return string.Empty;
                    }

                    CommonVariables.LogTool.Log("receive UA info:" + contactData.DataType + ", contactDATA ID:" + contactData.ContactDataID);
                    return HandleMMSUAInfo(contactData,token.ContactPersonService);
                }

                if (data.StartsWith(CommonFlag.F_MCSVerifyUAMSG))
                {
                    string tempStr = data.Remove(0, CommonFlag.F_MCSVerifyUAMSG.Length);
                    MsgRecordModel msgModel = CommonVariables.serializer.Deserialize<MsgRecordModel>(tempStr);
                    CommonVariables.LogTool.Log("get msg " + msgModel.ObjectID + " " + msgModel.ObjectName + " " + msgModel.Content + " " + msgModel.RecivedGroupID);

                    if (msgModel != null)
                    {
                        if (!string.IsNullOrEmpty(msgModel.ObjectID))
                        {
                            CommonVariables.MessageContorl.AddMSgRecordIntoBuffer(msgModel);
                            return "ok";
                        }
                    }
                }

                if (data.StartsWith(CommonFlag.F_MCSVerifyUAGetMSG))
                {
                    string tempStr = data.Remove(0, CommonFlag.F_MCSVerifyUAGetMSG.Length);
                    GetMsgModel getMsgModel = CommonVariables.serializer.Deserialize<GetMsgModel>(tempStr);
                    //CommonVariables.LogTool.Log("get msg " + getMsgModel.ObjectID.ToString() + " && " + getMsgModel.GroupIDs.Count.ToString() + "&&" + getMsgModel.LatestTime.ToString());
                    if (getMsgModel != null)
                    {
                        if (!string.IsNullOrEmpty(getMsgModel.ObjectID))
                        {
                            if (!CommonVariables.InRunningUAList.Contains(getMsgModel.ObjectID))
                            {
                                CommonVariables.InRunningUAList.Add(getMsgModel.ObjectID);
                                CommonVariables.MessageContorl.AddGetMsgIntoBuffer(getMsgModel);
                                token.Models = CommonVariables.MessageContorl.GetMSG(getMsgModel);
                                //CommonVariables.LogTool.Log("get msg account " + token.Models.Count.ToString());
                                if (token.Models != null && token.Models.Count > 0)
                                {
                                    token.UAObjectID = getMsgModel.ObjectID;
                                    return CommonVariables.serializer.Serialize(token.Models[0]);
                                }
                                else
                                {
                                    CommonVariables.InRunningUAList.Remove(getMsgModel.ObjectID);
                                }
                            }
                        }
                    }
                    return string.Empty;
                }
            }
            //if (data.StartsWith(CommonFlag.F_MCSVerfiyMDSMSG))
            //{
            //    string tempStr = data.Remove(0, CommonFlag.F_MCSVerifyUAGetMSG.Length);
            //    MsgRecord msgRecord = CommonVariables.serializer.Deserialize<MsgRecord>(tempStr);
            //    if (msgRecord != null)
            //    {
            //        if (!string.IsNullOrEmpty(msgRecord.MsgRecipientObjectID))
            //        {
            //            CommonVariables.MessageContorl.AddMsgIntoOutBuffer(msgRecord);
            //            return CommonFlag.F_MDSReciveMCSMSGFB + msgRecord.MsgID;
            //        }
            //    }
            //}

            return string.Empty;
        }


        private string HandleMMSUAInfo(ContactData contactData,IContactPersonService contactPersonService)
        {
            try
            {
                //if (contactData.DataType == 0)
                //{
                //    ContactPerson contactPerson = contactPersonService.FindContactPerson(contactData.ObjectID);
                //    if (contactPerson == null)
                //    {
                //        contactPerson = new ContactPerson();
                //        contactPerson.ContactName = contactData.ContactName;
                //        contactPerson.ImageSrc = contactData.ImageSrc;
                //        contactPerson.LatestTime = contactData.LatestTime;
                //        contactPerson.ObjectID = contactData.ObjectID;
                //        contactPerson.Password = contactData.Password;
                //        contactPerson.UpdateTime = contactData.UpdateTime;
                //        contactPersonService.InsertNewPerson(contactPerson);
                //    }
                //    else
                //    {
                //        contactPerson.ContactName = contactData.ContactName;
                //        contactPerson.ImageSrc = contactData.ImageSrc;
                //        contactPerson.LatestTime = contactData.LatestTime;
                //        contactPerson.Password = contactData.Password;
                //        contactPerson.UpdateTime = contactData.UpdateTime;
                //        contactPersonService.UpdateContactPerson(contactPerson);
                //    }
                //}
                int temp = 0;
                ContactPerson contactPerson = contactPersonService.FindContactPerson(contactData.ObjectID);

                if (contactPerson == null)
                {
                    CommonVariables.LogTool.Log("ContactPerson " + contactData.ObjectID + " can not find");
                    return string.Empty;
                }

                if (contactData.DataType == 1)
                {
                    ContactPersonList contactPersonList = contactPersonService.FindContactPersonList(contactData.ObjectID, contactData.DestinationObjectID);
                    if (contactPersonList == null)
                    {
                        contactPersonList = new ContactPersonList();
                        contactPersonList.DestinationObjectID = contactData.DestinationObjectID;
                        contactPersonList.IsDelete = contactData.IsDelete;
                        contactPersonList.ObjectID = contactData.ObjectID;
                        contactPersonList.UpdateTime = contactData.UpdateTime;
                        contactPersonService.InsertContactPersonList(contactPersonList);
                    }
                    else
                    {
                        contactPersonList.IsDelete = contactData.IsDelete;
                        contactPersonList.UpdateTime = contactData.UpdateTime;
                        contactPersonService.UpdateContactPersonList(contactPersonList);
                    }


                    CommonVariables.LogTool.Log("UpdateTime Compare " + contactPerson.UpdateTime + ":" + contactPersonList.UpdateTime);
                    if (contactPersonList.UpdateTime.CompareTo(contactPerson.UpdateTime) > 0)
                    {
                        contactPerson.UpdateTime = contactPersonList.UpdateTime;
                        CommonVariables.LogTool.Log("update contactPerson UpdateTime:" + contactPerson.UpdateTime);
                        temp = contactPersonService.UpdateContactPerson(contactPerson);
                        CommonVariables.LogTool.Log("update contactPerson UpdateTime:" + contactPerson.UpdateTime + " count=" + temp.ToString());
                    }

                }
                else if (contactData.DataType == 2)
                {
                    ContactGroup contactGroup = contactPersonService.FindContactGroup(contactData.GroupObjectID);
                    if (contactGroup == null)
                    {
                        contactGroup = new ContactGroup();
                        contactGroup.GroupName = contactData.GroupName;
                        contactGroup.GroupObjectID = contactData.GroupObjectID;
                        contactGroup.IsDelete = contactData.IsDelete;
                        contactGroup.UpdateTime = contactData.UpdateTime;
                        contactPersonService.InsertNewGroup(contactGroup);
                    }
                    else
                    {
                        contactGroup.GroupName = contactData.GroupName;
                        contactGroup.IsDelete = contactData.IsDelete;
                        contactGroup.UpdateTime = contactData.UpdateTime;
                        contactPersonService.UpdateContactGroup(contactGroup);
                    }

                    CommonVariables.LogTool.Log("UpdateTime Compare " + contactPerson.UpdateTime + ":" + contactGroup.UpdateTime);
                    if (contactGroup.UpdateTime.CompareTo(contactPerson.UpdateTime) > 0)
                    {
                        contactPerson.UpdateTime = contactGroup.UpdateTime;
                        CommonVariables.LogTool.Log("update contactPerson UpdateTime:" + contactPerson.UpdateTime);
                        temp = contactPersonService.UpdateContactPerson(contactPerson);
                        CommonVariables.LogTool.Log("update contactPerson UpdateTime:" + contactPerson.UpdateTime + " count=" + temp.ToString());
                    }
                }
                else if (contactData.DataType == 3)
                {
                    ContactGroupSub contactGroupSub = contactPersonService.FindContactGroupSub(contactData.ContactGroupID, contactData.ContactPersonObjectID);
                    if (contactGroupSub == null)
                    {
                        contactGroupSub = new ContactGroupSub();
                        contactGroupSub.ContactGroupID = contactData.ContactGroupID;
                        contactGroupSub.ContactPersonObjectID = contactData.ContactPersonObjectID;
                        contactGroupSub.IsDelete = contactData.IsDelete;
                        contactGroupSub.UpdateTime = contactData.UpdateTime;
                        contactPersonService.InsertContactGroupSub(contactGroupSub);
                    }
                    else
                    {
                        contactGroupSub.IsDelete = contactData.IsDelete;
                        contactGroupSub.UpdateTime = contactData.UpdateTime;
                        contactPersonService.UpdateContactGroupSub(contactGroupSub);
                    }

                    CommonVariables.LogTool.Log("UpdateTime Compare " + contactPerson.UpdateTime + ":" + contactGroupSub.UpdateTime);
                    if (contactGroupSub.UpdateTime.CompareTo(contactPerson.UpdateTime) > 0)
                    {
                        contactPerson.UpdateTime = contactGroupSub.UpdateTime;
                        CommonVariables.LogTool.Log("update contactPerson UpdateTime:" + contactPerson.UpdateTime);
                        temp = contactPersonService.UpdateContactPerson(contactPerson);
                        CommonVariables.LogTool.Log("update contactPerson UpdateTime:" + contactPerson.UpdateTime + " count=" + temp.ToString());
                    }
                }
                return contactData.ContactDataID;
            }
            catch (Exception ex)
            {
                CommonVariables.LogTool.Log("get UAInfo " + ex.Message + ex.StackTrace);
                return string.Empty;
            }
        }

        public void BeginService()
        {
            CommonVariables.MessageContorl.StartMainThread();
            base.BeginService(CommonVariables.MCSIP,CommonVariables.MCSPort);
        }
    }

}
