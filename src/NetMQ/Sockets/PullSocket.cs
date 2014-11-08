using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    /// <summary>
    /// Part of the push pull pattern, will pull messages from push socket
    /// </summary>
    public class PullSocket : NetMQSocket
    {
        public PullSocket(SocketBase socketBase)
            : base(socketBase)
        {
        }

        public override void Send(ref Msg msg, SendReceiveOptions options)
        {        
            throw new NotSupportedException("Pull socket doesn't support sending");
        }
    }
}
