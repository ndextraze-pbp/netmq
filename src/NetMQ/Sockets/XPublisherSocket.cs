using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    public class XPublisherSocket : NetMQSocket
    {
        public XPublisherSocket(Ctx parent, int threadId, int socketId) : base(ZmqSocketType.Xpub, parent, threadId, socketId)
        {
        }
    }
}
