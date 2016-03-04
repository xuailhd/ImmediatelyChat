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
using Xugl.ImmediatelyChat.IServices;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageChildServer
{

    internal class SocketListener:Xugl.ImmediatelyChat.SocketEngine.AsyncSocketListener<MsgRecord>
    {
        private readonly IContactPersonService contactPersonService;

        public SocketListener()
            : base(1024, 100, CommonVariables.LogTool)
        {
            contactPersonService = Xugl.ImmediatelyChat.Core.DependencyResolution.ObjectContainerFactory.CurrentContainer.Resolver<IContactPersonService>();

        }

        protected override void HandleError(ListenerToken<MsgRecord> token)
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

        protected override string HandleRecivedMessage(string inputMessage, ListenerToken<MsgRecord> token)
        {
            if (string.IsNullOrEmpty(inputMessage))
            {
                return string.Empty;
            }

            string data = inputMessage;

            if (token == null)
            {
                return string.Empty;
            }

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
                            ContactPerson contactPerson= contactPersonService.FindContactPerson(clientModel.ObjectID);
                            if(contactPerson!=null)
                            {
                                if(DateTime.Compare(contactPerson.UpdateTime,clientModel.UpdateTime)==0)
                                {
                                    return "ok";
                                }

                            }
                            return "wait";
                        }
                    }
                }


                if(data.StartsWith(CommonFlag.F_MCSReceiveUAInfo))
                {
                    ContactData contactData = CommonVariables.serializer.Deserialize<ContactData>(data.Remove(0, CommonFlag.F_MCSReceiveUAInfo.Length));

                    return HandleMMSUAInfo(contactData);

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


        private string HandleMMSUAInfo(ContactData contactData)
        {
            try
            {
                if (contactData.DataType == 0)
                {
                    ContactPerson contactPerson = contactPersonService.FindContactPerson(contactData.ObjectID);
                    if (contactPerson == null)
                    {
                        contactPerson = new ContactPerson();
                        contactPerson.ContactName = contactData.ContactName;
                        contactPerson.ImageSrc = contactData.ImageSrc;
                        contactPerson.LatestTime = contactData.LatestTime;
                        contactPerson.ObjectID = contactData.ObjectID;
                        contactPerson.Password = contactData.Password;
                        contactPerson.UpdateTime = contactData.UpdateTime;
                        contactPersonService.InsertNewPerson(contactPerson);
                    }
                    else
                    {
                        contactPerson.ContactName = contactData.ContactName;
                        contactPerson.ImageSrc = contactData.ImageSrc;
                        contactPerson.LatestTime = contactData.LatestTime;
                        contactPerson.Password = contactData.Password;
                        contactPerson.UpdateTime = contactData.UpdateTime;
                        contactPersonService.UpdatePerson(contactPerson);
                    }

                }
                else if (contactData.DataType == 1)
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
                }
                else if (contactData.DataType==2)
                {
                    ContactGroup contactGroup=contactPersonService.FindContactPerson
                }
                return CommonFlag.F_MMSVerifyMCSFBGetUAInfo + contactData.ObjectID;
            }
            catch(Exception ex)
            {
                CommonVariables.LogTool.Log("get UAInfor " + ex.Message + ex.StackTrace);
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
