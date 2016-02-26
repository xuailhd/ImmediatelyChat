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

    internal class SocketListener:Xugl.ImmediatelyChat.SocketEngine.AsyncSocketListener
    {
        private readonly IRepository<ContactPerson> contactPersonRepository;
        private readonly IContactPersonService contactPersonService;


        public SocketListener()
            : base(1024, 100, CommonVariables.LogTool)
        {
            contactPersonRepository = ObjectContainerFactory.CurrentContainer.Resolver<IRepository<ContactPerson>>();
            contactPersonService = ObjectContainerFactory.CurrentContainer.Resolver<IContactPersonService>();
        }

        protected override void HandleError(ListenerToken token)
        {
            if (token.Models != null && token.Models.Count > 0)
            {
                token.Models.Clear();
                token.Models = null;
            }
        }
        protected override string HandleRecivedMessage(string inputMessage, ListenerToken token)
        {
            //MCSModel tempMCSModel = null;
            //MDSModel tempMDSModel = null;
            ContactPerson tempContactPerson = null;

            if (string.IsNullOrEmpty(inputMessage))
            {
                return string.Empty;
            }

            string data = inputMessage;

            if (token == null)
            {
                return string.Empty;
            }


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

                    CommonVariables.LogTool.Log(clientStatusModel.LatestTime.ToString());
                    //Find MCS
                    for (int i = 0; i < CommonVariables.MCSServers.Count;i++ )
                    {
                        if (CommonVariables.MCSServers[i].ArrangeStr.Contains(clientStatusModel.ObjectID.Substring(0, 1)))
                        {
                            clientStatusModel.MCS_IP = CommonVariables.MCSServers[i].MCS_IP;
                            clientStatusModel.MCS_Port = CommonVariables.MCSServers[i].MCS_Port;

                            tempContactPerson = contactPersonRepository.Table.Where(t => t.ObjectID == clientStatusModel.ObjectID).FirstOrDefault();
                            if (tempContactPerson == null)
                            {
                                return string.Empty;
                            }

                            if (DateTime.Compare(clientStatusModel.LatestTime, tempContactPerson.LatestTime.GetValueOrDefault()) <= 0)
                            {
                                clientStatusModel.LatestTime = tempContactPerson.LatestTime.GetValueOrDefault();
                            }
                            else
                            {
                                tempContactPerson.LatestTime=clientStatusModel.LatestTime;
                                contactPersonRepository.Upade(tempContactPerson);
                            }

                            if(DateTime.Compare(clientStatusModel.LatestTime, tempContactPerson.LatestTime.GetValueOrDefault()) < 0)
                            {
                                IList<ContactGroupSub> contactGroupSubs = contactPersonService.GetLastestContactGroupSub(clientStatusModel.ObjectID, clientStatusModel.UpdateTime);
                                IList<ContactPersonList> contactPersonLists = contactPersonService.GetLastestContactPersonList(clientStatusModel.ObjectID, clientStatusModel.UpdateTime);
                            }
                            

                                

                            break;
                        }
                    }

                    //Send MCS
                    return CommonVariables.serializer.Serialize(clientStatusModel);
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


        public void BeginService()
        {
            base.BeginService(CommonVariables.MMSIP,CommonVariables.MMSPort);
        }
    }

}
