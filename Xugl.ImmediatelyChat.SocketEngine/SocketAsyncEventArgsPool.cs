using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;

namespace Xugl.ImmediatelyChat.SocketEngine
{
    internal class SocketAsyncEventArgsPool
    {
        Stack<SocketAsyncEventArgs> _Pool;

        public SocketAsyncEventArgsPool()
        {

            _Pool = new Stack<SocketAsyncEventArgs>();

        }

        public void Push(SocketAsyncEventArgs item)
        {

            if (item == null) return;

            lock (_Pool)
            {

                _Pool.Push(item);

            }

        }

        public SocketAsyncEventArgs PopOrNew()
        {

            if (Count == 0)

                return new SocketAsyncEventArgs();

            return Pop();

        }

        public SocketAsyncEventArgs Pop()
        {

            lock (_Pool)
            {

                return _Pool.Pop();

            }

        }

        public int Count
        {

            get { return _Pool.Count; }

        }

        public void Clear()
        {

            while (Count > 0)
            {

                Pop().Dispose();

            }

        }
    }
}
