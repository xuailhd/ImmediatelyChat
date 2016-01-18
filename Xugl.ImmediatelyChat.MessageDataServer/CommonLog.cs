using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.MessageDataServer
{
    public class CommonLog:ICommonLog
    {
        private StringBuilder _LogMsgs = new StringBuilder();
        private object temp = new object();
        public string LogMsg
        {
            get
            {
                lock (temp)
                {
                    return _LogMsgs.ToString();
                }
            }
        }


        private bool _isRecord;
        public bool IsRecord {
            get {
                lock (temp)
                {
                    return _isRecord;
                }
            }
            set
            {
                lock (temp)
                {
                    //if (value)
                    //{
                    //    _LogMsgs.Clear();
                    //}
                    _isRecord = value;
                }
            }
        }

        public void Log(string msg)
        {
            lock (temp)
            {
                _isRecord = false;
                if (_LogMsgs.Length > 0)
                {
                    _LogMsgs.Append("\r\n" + msg);
                }
                else
                {
                    _LogMsgs.Append(msg);
                }
            }
        }
    }
}
