using NetMQ.zmq;

namespace NetMQ.Sockets
{
    public class TPublisherSocket : NetMQSocket
    {
        public TPublisherSocket(SocketBase socketHandle)
            : base(socketHandle)
        {
        }

        public void SelectPeer(byte[] identity)
        {
            SetSocketOption(ZmqSocketOptions.TPubSelect, identity);
        }

        public void ClearPeerSubscription(byte[] identity)
        {
            SetSocketOption(ZmqSocketOptions.TPubClear, identity);
        }

        public void SubscribePeer(string topic)
        {
            SetSocketOption(ZmqSocketOptions.TPubSubscribe, topic);
        }

        public void SubscribePeer(byte[] topic)
        {
            SetSocketOption(ZmqSocketOptions.TPubSubscribe, topic);
        }

        public void UnsubscribePeer(string topic)
        {
            SetSocketOption(ZmqSocketOptions.TPubUnsubscribe, topic);
        }

        public void UnsubscribePeer(byte[] topic)
        {
            SetSocketOption(ZmqSocketOptions.TPubUnsubscribe, topic);
        }
    }
}
