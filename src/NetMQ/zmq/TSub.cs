using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Configuration;
using System.Text;

namespace NetMQ.zmq
{
    public class TSub : SocketBase
    {
        public class TSubSession : SessionBase
        {
            public TSubSession(IOThread ioThread, bool connect, SocketBase socket, Options options, Address addr)
                : base(ioThread, connect, socket, options, addr)
            {
            }
        }

        //  Fair queueing object for inbound pipes.
        private readonly FQ m_fq;

        //  Object for distributing the subscriptions upstream.
        private readonly Dist m_dist;

        private byte[] m_token;

        public TSub(Ctx parent, int threadId, int sid)
            : base(parent, threadId, sid)
        {
            m_options.SocketType = ZmqSocketType.Tsub;

            m_options.Linger = 0;
            m_fq = new FQ();
            m_dist = new Dist();

            // default is empty token
            m_token = new byte[0];
        }

        private void SendToken(Pipe pipe)
        {
            // send the token to the new upstream peer      
            Msg msg = new Msg(m_token, true);

            pipe.Write(msg);
            pipe.Flush();
        }

        protected override void XAttachPipe(Pipe pipe, bool icanhasall)
        {
            Debug.Assert(pipe != null);
            m_fq.Attach(pipe);
            m_dist.Attach(pipe);
           
            SendToken(pipe);
        }

        protected override void XReadActivated(Pipe pipe)
        {
            m_fq.Activated(pipe);
        }

        protected override void XWriteActivated(Pipe pipe)
        {
            m_dist.Activated(pipe);
        }

        protected override void XTerminated(Pipe pipe)
        {
            m_fq.Terminated(pipe);
            m_dist.Terminated(pipe);
        }

        protected override void XHiccuped(Pipe pipe)
        {
            // resend the token 
            SendToken(pipe);
        }

        protected override bool XSend(Msg msg, SendReceiveOptions flags)
        {
            byte[] data = msg.Data;
            // Malformed subscriptions.
            if (data.Length < 1 || (data[0] != 0 && data[0] != 1))
            {
                throw InvalidException.Create();
            }

            // save the new token.
            if (data[0] == 0)
            {
                m_token = data;
            }

            // distribute the message to the all peers 
            m_dist.SendToAll(msg, flags);

            return true;
        }

        protected override bool XHasOut()
        {
            return true;
        }

        protected override bool XRecv(SendReceiveOptions flags, out Msg msg)
        {
            //  Get a message using fair queueing algorithm.
            bool isMessageAvailable = m_fq.Recv(out msg);

            //  If there's no message available, return immediately.
            if (!isMessageAvailable)
            {
                return false;
            }

            return true;
        }

        protected override bool XHasIn()
        {
            return m_fq.HasIn();
        }                

        protected override void XSetSocketOption(ZmqSocketOptions option, object optval)
        {
            if (option != ZmqSocketOptions.TSubToken)
            {
                throw InvalidException.Create();
            }

            byte[] val;

            if (optval is String)
                val = Encoding.ASCII.GetBytes((String)optval);
            else if (optval is byte[])
                val = (byte[])optval;
            else
                throw InvalidException.Create();

            Msg msg = new Msg(val.Length + 1);
            msg.Put((byte)0);
            msg.Put(val, 1);

            //  Pass it further on in the stack.
            bool isMessageSent = XSend(msg, 0);

            if (!isMessageSent)
            {
                throw AgainException.Create();
            }
        }
    }
}
