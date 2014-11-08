using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    /// <summary>
    /// Request socket
    /// </summary>
    public class RequestSocket : NetMQSocket
    {
        public RequestSocket(SocketBase socketBase)
            : base(socketBase)
        {
        }
    }
}
