using System;
using NetMQ.zmq;

namespace NetMQ.Sockets
{
    /// <summary>
    /// Dealer socket, the dealer send messages in load balancing and receive in fair queueing.
    /// </summary>
    public class DealerSocket : NetMQSocket
    {
        public DealerSocket(Ctx parent, int threadId, int socketId) : base(ZmqSocketType.Dealer, parent, threadId, socketId)
        {
        }
    }
}
