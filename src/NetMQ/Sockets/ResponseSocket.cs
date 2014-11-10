using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    /// <summary>
    /// Response socket
    /// </summary>
    public class ResponseSocket : NetMQSocket
    {
        public ResponseSocket(Ctx parent, int threadId, int socketId)
            : base(ZmqSocketType.Rep, parent, threadId, socketId)
        {
        }
    }
}
