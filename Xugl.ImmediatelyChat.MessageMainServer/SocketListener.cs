using System;
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
            ContactPerson tempContactPerson = null;



            if (string.IsNullOrEmpty(inputMessage) || token == null)
            {
                return string.Empty;
            }

            string data = inputMessage;


            if (data.StartsWith(CommonFlag.F_PSSendMMSUser))
            {
                ContactPerson contactPerson = CommonVariables.serializer.Deserialize<ContactPerson>(data.Remove(0, CommonFlag.F_PSSendMMSUser.Length));
                if (contactPerson != null && !string.IsNullOrEmpty(contactPerson.ObjectID))
                {
                    if (CommonVariables.ContactPersonService.InsertNewPerson(contactPerson) > 0)
                    {
                        if (CommonVariables.ContactPersonService.InsertDefaultGroup(contactPerson.ObjectID) > 0)
                        {
                            return contactPerson.ObjectID;
                        }
                    }
                }
                return "failed";
            }


            if (data.StartsWith(CommonFlag.F_PSCallMMSStart))
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

            if (CommonVariables.IsBeginMessageService)
            {
                //UA
                if (data.StartsWith(CommonFlag.F_MMSVerifyUA))
                {
                    ClientStatusModel clientStatusModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(data.Remove(0, CommonFlag.F_MMSVerifyUA.Length));

                    CommonVariables.LogTool.Log("UA:" + clientStatusModel.ObjectID + " connected  " + CommonVariables.MCSServers.Count);
                    //Find MCS
                    for (int i = 0; i < CommonVariables.MCSServers.Count;i++ )
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
                            
                            if(string.IsNullOrEmpty(mcs_UpdateTime))
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

                if(data.StartsWith(CommonFlag.F_MMSVerifyUAGetUAInfo))
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

                if(data.StartsWith(CommonFlag.F_MMSVerifyFBUAGetUAInfo))
                {
                    string contactDataID = data.Remove(0, CommonFlag.F_MMSVerifyFBUAGetUAInfo.Length);
                    if(token.Models[0].ContactDataID!=contactDataID)
                    {
                        CommonVariables.LogTool.Log("data transfer error " + token.Models[0].ContactDataID + "  vs " + contactDataID);
                    }
                    token.Models.RemoveAt(0);
                    if (token.Models.Count<=0)
                    {
                        return string.Empty;
                    }
                    return CommonVariables.serializer.Serialize(token.Models[0]);
                }


                if(data.StartsWith(CommonFlag.F_MMSVerifyUASearch))
                {
                    ClientSearchModel clientSearchModel = CommonVariables.serializer.Deserialize<ClientSearchModel>(data.Remove(0, CommonFlag.F_MMSVerifyUASearch.Length));
                    if(clientSearchModel!=null && !string.IsNullOrEmpty(clientSearchModel.ObjectID))
                    {
                        if(clientSearchModel.Type==1)
                        {
                            token.Models = ContactPersonToContacData(token.ContactPersonService.SearchPerson(clientSearchModel.SearchKey));
                        }
                        else if (clientSearchModel.Type==2)
                        {
                            token.Models = ContactGroupToContacData(token.ContactPersonService.SearchGroup(clientSearchModel.SearchKey));
                        }

                        if(token.Models!=null && token.Models.Count>0)
                        {
                            return CommonVariables.serializer.Serialize(token.Models[0]);
                        }
                    }

                    return string.Empty;
                }

                if(data.StartsWith(CommonFlag.F_MMSVerifyUAFBSearch))
                {
                    string contactDataID = data.Remove(0, CommonFlag.F_MMSVerifyUAFBSearch.Length);
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
            }


            //if (data.StartsWith(CommonFlag.F_MMSVerifyMCS))
            //{
            //    //tempMCSModel = UnboxMCSMsg(strMsg);

            //    tempMCSModel = CommonVariables.serializer.Deserialize<MCSModel>(data.Remove(0, CommonFlag.F_MMSVerifyMCS.Length));
            //    CommonVariables.AddMCS(tempMCSModel);
            //    CommonVariables.LogTool.Log("Message Child Server " + tempMCSModel.MCS_ID + " connect");
            //    return "ok";
            //}

            //if (data.StartsWith(CommonFlag.F_MMSVerifyMDS))
            //{
            //    //tempMDSModel = UnboxMDSMsg(strMsg);

            //    tempMDSModel = CommonVariables.serializer.Deserialize<MDSModel>(data.Remove(0, CommonFlag.F_MMSVerifyMDS.Length));
            //    CommonVariables.AddMDS(tempMDSModel);
            //    CommonVariables.LogTool.Log("Message Data Server " + tempMDSModel.MDS_ID + " connect");
            //    return "ok";
            //}


            //if (data.StartsWith(CommonFlag.F_MMSReciveStopMCS))
            //{
            //    CommonVariables.RemoveMCS(data.Replace(CommonFlag.F_MMSReciveStopMCS, ""));
            //    CommonVariables.LogTool.Log("Message Child Server " + data.Replace("stopMCS", "") + " disconnect");
            //}

            //if (data.StartsWith(CommonFlag.F_MMSReciveStopMDS))
            //{
            //    CommonVariables.RemoveMCS(data.Replace(CommonFlag.F_MMSReciveStopMDS, ""));
            //    CommonVariables.LogTool.Log("Message Data Server " + data.Replace("stopMDS", "") + " disconnect");
            //}

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
