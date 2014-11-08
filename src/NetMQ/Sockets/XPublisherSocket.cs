using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    public class XPublisherSocket : NetMQSocket
    {
        public XPublisherSocket(SocketBase socketBase)
            : base(socketBase)
        {
        }
    }
}
