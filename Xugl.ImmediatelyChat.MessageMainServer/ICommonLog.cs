using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.MessageMainServer
{
    public interface ICommonLog
    {
        void Log(string msg);

        bool IsRecord { get; set; }

        string LogMsg { get; }
    }
}
