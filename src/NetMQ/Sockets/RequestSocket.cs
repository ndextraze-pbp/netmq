using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    /// <summary>
    /// Request socket
    /// </summary>
    public class RequestSocket : NetMQSocket
    {
        public RequestSocket(Ctx parent, int threadId, int socketId) : base(ZmqSocketType.Req, parent, threadId, socketId)
        {
        }
    }
}
