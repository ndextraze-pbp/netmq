using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace NetMQ.zmq
{
    public abstract class BasePattern : IDisposable
    {
        private readonly SocketBase m_socketBase;

        public BasePattern(SocketBase socketBase)
        {
            m_socketBase = socketBase;
        }

        public Options Options
        {
            get { return m_socketBase.Options; }
        }

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
