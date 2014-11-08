/*
    Copyright (c) 2009-2011 250bpm s.r.o.
    Copyright (c) 2011 VMware, Inc.
    Copyright (c) 2007-2011 Other contributors as noted in the AUTHORS file

    This file is part of 0MQ.

    0MQ is free software; you can redistribute it and/or modify it under
    the terms of the GNU Lesser General Public License as published by
    the Free Software Foundation; either version 3 of the License, or
    (at your option) any later version.

    0MQ is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Lesser General Public License for more details.

    You should have received a copy of the GNU Lesser General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/

using System;
using System.Diagnostics;
using System.Net.Sockets;
using NetMQ.zmq.Patterns.Utils;

namespace NetMQ.zmq.Patterns
{
    class Dealer : BasePattern
    {

        public class DealerSession : SessionBase
        {
            public DealerSession(IOThread ioThread, bool connect,
                                                     SocketBase socket, Options options,
                                                     Address addr)
                : base(ioThread, connect, socket, options, addr)
            {

            }
        }

        //  Messages are fair-queued from inbound pipes. And load-balanced to
        //  the outbound pipes.
        private readonly FairQueueing m_fairQueueing;
        private readonly LoadBalancer m_loadBalancer;

        //  Have we prefetched a message.
        private bool m_prefetched;

        private Msg m_prefetchedMsg;

        //  Holds the prefetched message.
        public Dealer(SocketBase socket)
            : base(socket)
        {

            m_prefetched = false;
            Options.SocketType = ZmqSocketType.Dealer;

            m_fairQueueing = new FairQueueing();
            m_loadBalancer = new LoadBalancer();            

            Options.RecvIdentity = true;

            m_prefetchedMsg = new Msg();
            m_prefetchedMsg.InitEmpty();
        }

        public override void Dispose()
        {
            base.Dispose();

            m_prefetchedMsg.Close();
        }

        public override void AddPipe(Pipe pipe, bool subscribeToAll)
        {

            Debug.Assert(pipe != null);
            m_fairQueueing.Attach(pipe);
            m_loadBalancer.Attach(pipe);
        }

        public override bool Send(ref Msg msg, SendReceiveOptions flags)
        {
            return m_loadBalancer.Send(ref msg, flags);
        }

        public override bool Receive(SendReceiveOptions flags, ref Msg msg)
        {
            return ReceiveInternal(flags, ref msg);
        }

        private bool ReceiveInternal(SendReceiveOptions flags, ref Msg msg)
        {            
            //  If there is a prefetched message, return it.
            if (m_prefetched)
            {
                msg.Move(ref m_prefetchedMsg);
                
                m_prefetched = false;
                
                return true;
            }

            //  DEALER socket doesn't use identities. We can safely drop it and 
            while (true)
            {
                bool isMessageAvailable = m_fairQueueing.Recv(ref msg);

                if (!isMessageAvailable)
                {
                    return false;
                }
             
                if ((msg.Flags & MsgFlags.Identity) == 0)
                    break;
            }

            return true;
        }

        public override bool HasIn()
        {
            //  We may already have a message pre-fetched.
            if (m_prefetched)
                return true;

            //  Try to read the next message to the pre-fetch buffer.

            bool isMessageAvailable = ReceiveInternal(SendReceiveOptions.DontWait, ref m_prefetchedMsg);

            if (!isMessageAvailable)
            {
                return false;
            }
            else
            {
                m_prefetched = true;
                return true;
            }
        }

        public override bool HasOut()
        {
            return m_loadBalancer.HasOut();
        }

        public override void ReadActivated(Pipe pipe)
        {
            m_fairQueueing.Activated(pipe);
        }

        public override void WriteActivated(Pipe pipe)
        {
            m_loadBalancer.Activated(pipe);
        }

        public override void RemovePipe(Pipe pipe)
        {
            m_fairQueueing.Terminated(pipe);
            m_loadBalancer.Terminated(pipe);
        }

        public override void Hiccuped(Pipe pipe)
        {
            throw new NotSupportedException();
        }
    }
}
