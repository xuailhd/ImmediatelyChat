﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.ServiceModel.Channels;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xugl.ImmediatelyChat.Core;
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageChildServer
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
                CommonVariables.MessageContorl.ReturnMsg(token.Models);
                CommonVariables.InRunningUAList.Remove(token.UAObjectID);
                token.Models.Clear();
                token.Models = null;
                token.UAObjectID = string.Empty;
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
            if (data.StartsWith(CommonFlag.F_MCSReciveUAMSGFB))
            {
                string tempStr = data.Remove(0, CommonFlag.F_MCSReciveUAMSGFB.Length);
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
                        CommonVariables.LogTool.Log("Account " + clientModel.ObjectID + " connect");
                        return "ok";
                    }
                }
            }


            if (data.StartsWith(CommonFlag.F_MCSVerifyUAMSG))
            {
                string tempStr = data.Remove(0, CommonFlag.F_MCSVerifyUAMSG.Length);
                MsgRecordModel msgModel = CommonVariables.serializer.Deserialize<MsgRecordModel>(tempStr);
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
                if (getMsgModel != null)
                {
                    if (!string.IsNullOrEmpty(getMsgModel.ObjectID))
                    {
                        if (!CommonVariables.InRunningUAList.Contains(getMsgModel.ObjectID))
                        {
                            CommonVariables.InRunningUAList.Add(getMsgModel.ObjectID);
                            CommonVariables.MessageContorl.AddGetMsgIntoBuffer(getMsgModel);
                            token.Models = CommonVariables.MessageContorl.GetMSG(getMsgModel);
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


        public void BeginService()
        {
            base.BeginService(CommonVariables.MCSIP,CommonVariables.MCSPort);
        }
    }

}
