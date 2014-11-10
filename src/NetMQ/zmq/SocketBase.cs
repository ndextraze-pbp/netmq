using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ.zmq.Patterns;

namespace NetMQ.zmq
{
    public abstract class SocketBase : IDisposable
    {        
        private readonly NetMQSocket m_socket;

        public SocketBase(NetMQSocket socket, Options options)
        {
            Options = options;
            m_socket = socket;
        }

        public static SocketBase Create(ZmqSocketType type, NetMQSocket socket, Options options)
        {
            switch (type)
            {
                case ZmqSocketType.Pair:
                    return new Pair(socket, options);
                    
                case ZmqSocketType.Pub:
                    return new Pub(socket, options);

                case ZmqSocketType.Sub:
                    return new Sub(socket, options);

                case ZmqSocketType.Req:
                    return new Req(socket, options);

                case ZmqSocketType.Rep:
                    return new Rep(socket, options);

                case ZmqSocketType.Dealer:
                    return new Dealer(socket, options);

                case ZmqSocketType.Router:
                    return new Router(socket, options);

                case ZmqSocketType.Pull:
                    return new Pull(socket, options);

                case ZmqSocketType.Push:
                    return new Push(socket, options);

                case ZmqSocketType.Xpub:
                    return new XPub(socket, options);

                case ZmqSocketType.Xsub:
                    return new XSub(socket, options);

                case ZmqSocketType.Stream:
                    return new Stream(socket, options);

                default:
                    throw new InvalidException("type=" + type);
            }
        }

        protected Options Options { get; private set; }

        //  Concrete algorithms for the x- methods are to be defined by
        //  individual socket types.
        public abstract void AddPipe(Pipe pipe, bool subscribeToAll);

        public abstract void RemovePipe(Pipe pipe);

        public abstract bool HasOut();

        public abstract bool HasIn();

        public abstract bool Send(ref Msg msg, SendReceiveOptions flags);        

        public abstract bool Receive(SendReceiveOptions flags, ref Msg msg);

        public abstract void ReadActivated(Pipe pipe);

        public abstract void WriteActivated(Pipe pipe);

        public abstract void Hiccuped(Pipe pipe);

        public virtual void Dispose()
        {
            
        }

        public virtual bool SetOption(ZmqSocketOptions option, Object optval)
        {
            return false;
        }       
    }
}
