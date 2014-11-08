using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    /// <summary>
    /// Response socket
    /// </summary>
    public class ResponseSocket : NetMQSocket
    {
        public ResponseSocket(SocketBase socketBase)
            : base(socketBase)
        {
        }
    }
}
