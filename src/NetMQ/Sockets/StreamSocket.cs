using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    public class StreamSocket : NetMQSocket
    {
        public StreamSocket(Ctx parent, int threadId, int socketId) : 
            base(ZmqSocketType.Stream, parent, threadId, socketId)
        {
        }
    }
}
