using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    /// <summary>
    /// Part of the push pull pattern, will push messages to push sockets
    /// </summary>
    public class PushSocket : NetMQSocket
    {
        public PushSocket(Ctx parent, int threadId, int socketId) : base(ZmqSocketType.Push, parent, threadId, socketId)
        {
        }

        public override void Receive(ref Msg msg, SendReceiveOptions options)
        {
            throw new NotSupportedException("Push socket doesn't support receiving");
        }
    }
}
