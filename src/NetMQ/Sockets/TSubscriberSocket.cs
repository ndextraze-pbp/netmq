using NetMQ.zmq;

namespace NetMQ.Sockets
{
    public class TSubscriberSocket : NetMQSocket
    {
        public TSubscriberSocket(SocketBase socketHandle)
            : base(socketHandle)
        {
        }

        public void SetToken(byte[] token)
        {
            SetSocketOption(ZmqSocketOptions.TSubToken, token);
        }

        public void SetToken(string token)
        {
            SetSocketOption(ZmqSocketOptions.TSubToken, token);
        }
    }
}
