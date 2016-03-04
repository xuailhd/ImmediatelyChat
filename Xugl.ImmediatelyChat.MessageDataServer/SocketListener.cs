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
using Xugl.ImmediatelyChat.Model;
using Xugl.ImmediatelyChat.SocketEngine;

namespace Xugl.ImmediatelyChat.MessageDataServer
{

    internal class SocketListener:Xugl.ImmediatelyChat.SocketEngine.AsyncSocketListener<MsgRecord>
    {
        public SocketListener()
            : base(1024, 50, CommonVariables.LogTool)
        {
            
        }

        protected override void HandleError(ListenerToken<MsgRecord> token)
        {
            if (token.Models != null && token.Models.Count > 0)
            {
                token.Models.Clear();
                token.Models = null;
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

            if(data.StartsWith(CommonFlag.F_PSCallMDSStart))
            {
                MDSServer mdsServer = CommonVariables.serializer.Deserialize<MDSServer>(data.Remove(0, CommonFlag.F_PSCallMDSStart.Length));
                CommonVariables.ArrangeStr = mdsServer.ArrangeStr;
                CommonVariables.OperateFile.SaveConfig(CommonVariables.ConfigFilePath, "ArrangeStr", CommonVariables.ArrangeStr);
                CommonVariables.LogTool.Log("Start MDS service:" + CommonVariables.MDSIP + ", Port:" + CommonVariables.MDSPort.ToString());
                CommonVariables.IsBeginMessageService = true;
                return string.Empty;
            }

            if (CommonVariables.IsBeginMessageService)
            {
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
                        CommonVariables.LogTool.Log("get mcs msg " + msgModel.Content + " " + msgModel.RecivedGroupID);
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
                    //CommonVariables.LogTool.Log("recive mcs get msg:" + tempStr);

                    GetMsgModel getMsgModel = CommonVariables.serializer.Deserialize<GetMsgModel>(tempStr);


                    //CommonVariables.LogTool.Log("recive mcs get msg:" + getMsgModel.ObjectID + " " + getMsgModel.GroupIDs[0] + "  " + string.Format(getMsgModel.LatestTime.ToString(),"yyyy-MM-dd HH:mm:ss:SSS"));
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
            CommonVariables.MessageContorl.StartMainThread();
            base.BeginService(CommonVariables.MDSIP,CommonVariables.MDSPort);
        }
    }
}
