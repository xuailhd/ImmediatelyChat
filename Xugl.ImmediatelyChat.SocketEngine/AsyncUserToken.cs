using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.SocketEngine
{
    internal class AsyncUserToken
    {
        private Socket m_Socket;

        public Socket Socket
        {
            get
            {
                return m_Socket;
            }
            set
            {
                m_Socket = value;
            }
        }

        public AsyncUserToken()
            : this(null)
        {

        }

        public AsyncUserToken(Socket socket)
        {
            m_Socket = socket;
        }
    }
}
