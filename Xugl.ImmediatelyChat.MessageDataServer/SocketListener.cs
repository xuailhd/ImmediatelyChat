using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageDataServer
{

    internal class SocketListener:Xugl.ImmediatelyChat.SocketEngine.AsyncSocketListener
    {
        public SocketListener()
            : base(1024, 2, CommonVariables.LogTool)
        {
            
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
            if (string.IsNullOrEmpty(inputMessage))
            {
                return string.Empty;
            }

            string data = inputMessage;

            if (token == null)
            {
                return string.Empty;
            }

            //handle UA feedback
            if (data.StartsWith(CommonFlag.F_MDSReciveMCSMSGFB))
            {
                string tempStr = data.Remove(0, CommonFlag.F_MDSReciveMCSMSGFB.Length);
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


            if (data.StartsWith(CommonFlag.F_MDSVerifyMCSMSG))
            {
                string tempStr = data.Remove(0, CommonFlag.F_MDSVerifyMCSMSG.Length);
                MsgRecordModel msgModel = CommonVariables.serializer.Deserialize<MsgRecordModel>(tempStr);
                if (msgModel != null)
                {
                    if (!string.IsNullOrEmpty(msgModel.ObjectID))
                    {
                        CommonVariables.MessageContorl.AddMSgRecordIntoBuffer(msgModel);
                        return msgModel.MessageID;
                    }
                }
            }

            if (data.StartsWith(CommonFlag.F_MDSVerifyMCSGetMSG))
            {
                string tempStr = data.Remove(0, CommonFlag.F_MDSVerifyMCSGetMSG.Length);
                GetMsgModel getMsgModel = CommonVariables.serializer.Deserialize<GetMsgModel>(tempStr);
                if (getMsgModel != null)
                {
                    if (!string.IsNullOrEmpty(getMsgModel.ObjectID))
                    {
                        token.Models = CommonVariables.MessageContorl.GetMSG(getMsgModel);
                        if (token.Models != null && token.Models.Count > 0)
                        {
                            return CommonFlag.F_MCSVerfiyMDSMSG + CommonVariables.serializer.Serialize(token.Models[0]);
                        }
                    }
                }
                return string.Empty;
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
            //            return msgRecord.MsgID;
            //        }
            //    }
            //}

            return string.Empty;
        }


        public void BeginService()
        {
            base.BeginService(CommonVariables.MDSIP,CommonVariables.MDSPort);
        }
    }

}
