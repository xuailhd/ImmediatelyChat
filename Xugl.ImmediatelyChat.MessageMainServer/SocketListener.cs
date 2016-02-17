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
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageMainServer
{

    internal class SocketListener:Xugl.ImmediatelyChat.SocketEngine.AsyncSocketListener
    {
        private readonly IRepository<ContactPerson> contactPersonRepository;


        public SocketListener()
            : base(1024, 100, CommonVariables.LogTool)
        {
            contactPersonRepository = Xugl.ImmediatelyChat.Core.DependencyResolution.ObjectContainerFactory.CurrentContainer.Resolver<IRepository<ContactPerson>>();
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
            MCSModel tempMCSModel = null;
            MDSModel tempMDSModel = null;
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

            if (CommonVariables.IsBeginMessageService)
            {
                //UA
                if (data.StartsWith(CommonFlag.F_MMSVerifyUA))
                {
                    ClientStatusModel clientStatusModel = CommonVariables.serializer.Deserialize<ClientStatusModel>(data.Remove(0, CommonFlag.F_MMSVerifyUA.Length));

                    CommonVariables.LogTool.Log(clientStatusModel.LatestTime.ToString());
                    //Find MCS
                    foreach (string mcs_id in CommonVariables.GetMCSs.Keys)
                    {
                        if (CommonVariables.GetMCSs[mcs_id].ArrangeChars.Contains(clientStatusModel.ObjectID.Substring(0, 1)))
                        {
                            clientStatusModel.MCS_IP = CommonVariables.GetMCSs[mcs_id].MCS_IP;
                            clientStatusModel.MCS_Port = CommonVariables.GetMCSs[mcs_id].MCS_Port;

                            tempContactPerson = contactPersonRepository.Table.Where(t => t.ObjectID == clientStatusModel.ObjectID).FirstOrDefault();
                            if (tempContactPerson==null)
                            {
                                tempContactPerson = new ContactPerson();
                                tempContactPerson.ObjectID = clientStatusModel.ObjectID;
                                tempContactPerson.LatestTime = DateTime.Now;
                                contactPersonRepository.Insert(tempContactPerson);
                            }

                            if (DateTime.Compare(clientStatusModel.LatestTime, tempContactPerson.LatestTime.GetValueOrDefault()) <= 0)
                            {
                                clientStatusModel.LatestTime = tempContactPerson.LatestTime.GetValueOrDefault();
                            }
                            else
                            {
                                tempContactPerson.LatestTime = clientStatusModel.LatestTime;
                                contactPersonRepository.Upade(tempContactPerson);
                            }

                            break;
                        }
                    }

                    //Send MCS
                    return CommonVariables.serializer.Serialize(clientStatusModel);
                }
            }


            if (data.StartsWith(CommonFlag.F_MMSVerifyMCS))
            {
                //tempMCSModel = UnboxMCSMsg(strMsg);

                tempMCSModel = CommonVariables.serializer.Deserialize<MCSModel>(data.Remove(0, CommonFlag.F_MMSVerifyMCS.Length));
                CommonVariables.AddMCS(tempMCSModel);
                CommonVariables.LogTool.Log("Message Child Server " + tempMCSModel.MCS_ID + " connect");
                return "ok";
            }

            if (data.StartsWith(CommonFlag.F_MMSVerifyMDS))
            {
                //tempMDSModel = UnboxMDSMsg(strMsg);

                tempMDSModel = CommonVariables.serializer.Deserialize<MDSModel>(data.Remove(0, CommonFlag.F_MMSVerifyMDS.Length));
                CommonVariables.AddMDS(tempMDSModel);
                CommonVariables.LogTool.Log("Message Data Server " + tempMDSModel.MDS_ID + " connect");
                return "ok";
            }


            if (data.StartsWith(CommonFlag.F_MMSReciveStopMCS))
            {
                CommonVariables.RemoveMCS(data.Replace(CommonFlag.F_MMSReciveStopMCS, ""));
                CommonVariables.LogTool.Log("Message Child Server " + data.Replace("stopMCS", "") + " disconnect");
            }

            if (data.StartsWith(CommonFlag.F_MMSReciveStopMDS))
            {
                CommonVariables.RemoveMCS(data.Replace(CommonFlag.F_MMSReciveStopMDS, ""));
                CommonVariables.LogTool.Log("Message Data Server " + data.Replace("stopMDS", "") + " disconnect");
            }

            return string.Empty;
        }


        public void BeginService()
        {
            base.BeginService(CommonVariables.MMSIP,CommonVariables.MMSPort);
        }
    }

}
