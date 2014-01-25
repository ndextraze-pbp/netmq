using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net.Sockets;
using System.Text;

namespace NetMQ.zmq
{
    public class TPub : SocketBase
    {
        public class TPubSession : SessionBase
        {
            public TPubSession(IOThread ioThread, bool connect,
                                                 SocketBase socket, Options options, Address addr) :
                base(ioThread, connect, socket, options, addr)
            {

            }

        }

        //  We keep a set of pipes that have not been identified yet.
        private readonly HashSet<Pipe> m_anonymousPipes;

        //  Outbound pipes indexed by the peer IDs.
        private readonly Dictionary<Blob, Pipe> m_pipes;

        //  Peer ID are generated. It's a simple increment and wrap-over
        //  algorithm. This value is the next ID to use (if not used already).
        private int m_nextPeerId;

        //  List of all subscriptions mapped to corresponding pipes.
        private readonly Mtrie m_subscriptions;

        //  Distributor of messages holding the list of outbound pipes.
        private readonly Dist m_dist;

        //  Fair queueing object for inbound pipes.
        private readonly FQ m_fq;

        //  True if we are in the middle of sending a multi-part message.
        private bool m_moreOut;

        //  True iff there is a message held in the pre-fetch buffer.
        private bool m_prefetched;

        //  If true, the receiver got the message part with
        //  the peer's identity.
        private bool m_identitySent;

        //  Holds the prefetched identity.
        private Msg m_prefetchedId;

        //  Holds the prefetched message.
        private Msg m_prefetchedMsg;

        //  If true, more incoming message parts are expected.
        private bool m_moreIn;

        private Pipe m_selectedPipe;

        private static readonly Mtrie.MtrieDelegate s_markAsMatching;
        private static readonly Mtrie.MtrieDelegate s_SendUnsubscription;

        static TPub()
        {
            s_markAsMatching = (pipe, data, arg) =>
            {
                TPub self = (TPub)arg;
                self.m_dist.Match(pipe);
            };

            s_SendUnsubscription = (pipe, data, o) => { };
        }

        public TPub(Ctx parent, int threadId, int sid)
            : base(parent, threadId, sid)
        {
            m_options.SocketType = ZmqSocketType.Tpub;
            m_moreOut = false;

            m_subscriptions = new Mtrie();
            m_dist = new Dist();
            m_fq = new FQ();

            m_anonymousPipes = new HashSet<Pipe>();
            m_pipes = new Dictionary<Blob, Pipe>();
            m_nextPeerId = Utils.GenerateRandom();

            m_prefetched = false;
            m_identitySent = false;
            m_prefetchedId = new Msg();
            m_prefetchedMsg = new Msg();

            m_options.RecvIdentity = true;
        }

        protected override void XAttachPipe(Pipe pipe, bool icanhasall)
        {
            Debug.Assert(pipe != null);

            m_dist.Attach(pipe);

            bool identityOk = IdentifyPeer(pipe);
            if (identityOk)
                m_fq.Attach(pipe);
            else
                m_anonymousPipes.Add(pipe);
        }

        protected override void XReadActivated(Pipe pipe)
        {
            if (!m_anonymousPipes.Contains(pipe))
                m_fq.Activated(pipe);
            else
            {
                bool identityOk = IdentifyPeer(pipe);
                if (identityOk)
                {
                    m_anonymousPipes.Remove(pipe);
                    m_fq.Attach(pipe);
                }
            }
        }

        protected override void XWriteActivated(Pipe pipe)
        {
            m_dist.Activated(pipe);
        }

        protected override void XTerminated(Pipe pipe)
        {
            m_subscriptions.RemoveHelper(pipe, s_SendUnsubscription, this);

            m_dist.Terminated(pipe);

            if (!m_anonymousPipes.Remove(pipe))
            {
                Pipe old;

                m_pipes.TryGetValue(pipe.Identity, out  old);
                m_pipes.Remove(pipe.Identity);

                Debug.Assert(old != null);

                m_fq.Terminated(pipe);
            }
        }

        protected override bool XSend(Msg msg, SendReceiveOptions flags)
        {
            bool msgMore = msg.HasMore;

            //  For the first part of multi-part message, find the matching pipes.
            if (!m_moreOut)
                m_subscriptions.Match(msg.Data, msg.Size,
                                                        s_markAsMatching, this);

            //  Send the message to all the pipes that were marked as matching
            //  in the previous step.
            m_dist.SendToMatching(msg, flags);

            //  If we are at the end of multi-part message we can mark all the pipes
            //  as non-matching.
            if (!msgMore)
                m_dist.Unmatch();

            m_moreOut = msgMore;

            return true;
        }

        protected override bool XHasOut()
        {
            return m_dist.HasOut();
        }

        protected override bool XRecv(SendReceiveOptions flags, out Msg msg)
        {
            msg = null;
            if (m_prefetched)
            {
                if (!m_identitySent)
                {
                    msg = m_prefetchedId;
                    m_prefetchedId = null;
                    m_identitySent = true;
                }
                else
                {
                    msg = m_prefetchedMsg;
                    m_prefetchedMsg = null;
                    m_prefetched = false;
                }
                m_moreIn = msg.HasMore;
                return true;
            }

            Pipe[] pipe = new Pipe[1];

            bool isMessageAvailable = m_fq.RecvPipe(pipe, out msg);

            //  It's possible that we receive peer's identity. That happens
            //  after reconnection. The current implementation assumes that
            //  the peer always uses the same identity.
            //  TODO: handle the situation when the peer changes its identity.
            while (isMessageAvailable && msg != null && msg.IsIdentity)
                isMessageAvailable = m_fq.RecvPipe(pipe, out msg);

            if (!isMessageAvailable)
            {
                return false;
            }
            else if (msg == null)
            {
                return true;
            }

            Debug.Assert(pipe[0] != null);

            //  If we are in the middle of reading a message, just return the next part.
            if (m_moreIn)
                m_moreIn = msg.HasMore;
            else
            {
                //  We are at the beginning of a message.
                //  Keep the message part we have in the prefetch buffer
                //  and return the ID of the peer instead.
                m_prefetchedMsg = msg;

                m_prefetched = true;

                Blob identity = pipe[0].Identity;
                msg = new Msg(identity.Data);
                msg.SetFlags(MsgFlags.More);
                m_identitySent = true;
            }

            return true;
        }

        protected override bool XHasIn()
        {
            //  If we are in the middle of reading the messages, there are
            //  definitely more parts available.
            if (m_moreIn)
                return true;

            //  We may already have a message pre-fetched.
            if (m_prefetched)
                return true;

            //  Try to read the next message.
            //  The message, if read, is kept in the pre-fetch buffer.
            Pipe[] pipe = new Pipe[1];

            bool isMessageAvailable = m_fq.RecvPipe(pipe, out m_prefetchedMsg);

            if (!isMessageAvailable)
            {
                return false;
            }


            //  It's possible that we receive peer's identity. That happens
            //  after reconnection. The current implementation assumes that
            //  the peer always uses the same identity.
            //  TODO: handle the situation when the peer changes its identity.
            while (m_prefetchedMsg != null && m_prefetchedMsg.IsIdentity)
            {
                isMessageAvailable = m_fq.RecvPipe(pipe, out m_prefetchedMsg);

                if (!isMessageAvailable)
                {
                    return false;
                }
            }

            if (m_prefetchedMsg == null)
            {
                return false;
            }

            Debug.Assert(pipe[0] != null);

            Blob identity = pipe[0].Identity;
            m_prefetchedId = new Msg(identity.Data);
            m_prefetchedId.SetFlags(MsgFlags.More);

            m_prefetched = true;
            m_identitySent = false;

            return true;
        }

        private bool IdentifyPeer(Pipe pipe)
        {
            Blob identity;

            if (m_options.RawSocket)
            {
                byte[] buf = new byte[5];
                buf[0] = 0;
                byte[] result = BitConverter.GetBytes(m_nextPeerId++);
                Buffer.BlockCopy(result, 0, buf, 1, 4);
                identity = new Blob(buf);
            }
            else
            {
                Msg msg = pipe.Read();
                if (msg == null)
                    return false;

                if (msg.Size == 0)
                {
                    //  Fall back on the auto-generation
                    byte[] buf = new byte[5];

                    buf[0] = 0;

                    byte[] result = BitConverter.GetBytes(m_nextPeerId++);

                    Buffer.BlockCopy(result, 0, buf, 1, 4);
                    identity = new Blob(buf);
                }
                else
                {
                    identity = new Blob(msg.Data);

                    //  Ignore peers with duplicate ID.
                    if (m_pipes.ContainsKey(identity))
                        return false;
                }
            }

            pipe.Identity = identity;
            //  Add the record into output pipes lookup table            
            m_pipes.Add(identity, pipe);

            return true;
        }

        protected override void XSetSocketOption(ZmqSocketOptions option, object optval)
        {
            if (option != ZmqSocketOptions.TPubSelect &&
                option != ZmqSocketOptions.TPubSubscribe &&
                option != ZmqSocketOptions.TPubUnsubscribe &&
                option != ZmqSocketOptions.TPubClear)
            {
                throw InvalidException.Create();
            }

            byte[] val;

            if (option == ZmqSocketOptions.TPubSelect || option == ZmqSocketOptions.TPubClear)
            {                
                if (optval is byte[])
                    val = (byte[]) optval;
                else
                    throw InvalidException.Create();

                Blob blob = new Blob(val);

                Pipe pipe;

                if (!m_pipes.TryGetValue(blob, out pipe))
                {
                    throw NetMQException.Create(ErrorCode.EHOSTUNREACH);
                }

                if (option == ZmqSocketOptions.TPubSelect)
                {
                    m_selectedPipe = pipe;
                }
                else
                {
                    m_subscriptions.RemoveHelper(pipe, s_SendUnsubscription, this);
                }
            }
            else
            {
                if (m_selectedPipe == null)
                    throw InvalidException.Create();

                if (optval is String)
                    val = Encoding.ASCII.GetBytes((String)optval);
                else if (optval is byte[])
                    val = (byte[])optval;
                else
                    throw InvalidException.Create();

                if (option == ZmqSocketOptions.TPubSubscribe)
                {
                    m_subscriptions.Add(val, m_selectedPipe);
                }
                else
                {
                    m_subscriptions.Remove(val, 0, m_selectedPipe);
                }                                                             
            }
        }
    }
}
