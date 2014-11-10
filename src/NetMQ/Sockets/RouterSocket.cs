using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    /// <summary>
    /// Router socket, the first message is always the identity of the sender
    /// </summary>
    public class RouterSocket : NetMQSocket
    {
        public RouterSocket(Ctx parent, int threadId, int socketId)
            : base(ZmqSocketType.Router, parent, threadId, socketId)
        {
        }
    }
}
