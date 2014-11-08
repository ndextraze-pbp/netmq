/*      
    Copyright (c) 2009-2011 250bpm s.r.o.
    Copyright (c) 2007-2010 iMatix Corporation
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
using NetMQ.zmq.Patterns.Utils;

namespace NetMQ.zmq.Patterns
{
    class Push : BasePattern
    {
        public class PushSession : SessionBase
        {
            public PushSession(IOThread ioThread, bool connect,
                               SocketBase socket, Options options,
                               Address addr)
                : base(ioThread, connect, socket, options, addr)
            {
            }
        }

        //  Load balancer managing the outbound pipes.
        private readonly LoadBalancer m_loadBalancer;

        public Push(SocketBase socket)
            : base(socket)
        {
            Options.SocketType = ZmqSocketType.Push;

            m_loadBalancer = new LoadBalancer();
        }

        public override void AddPipe(Pipe pipe, bool subscribeToAll)
        {
            Debug.Assert(pipe != null);
            m_loadBalancer.Attach(pipe);
        }

        public override void WriteActivated(Pipe pipe)
        {
            m_loadBalancer.Activated(pipe);
        }

        public override void RemovePipe(Pipe pipe)
        {
            m_loadBalancer.Terminated(pipe);
        }

        public override bool Send(ref Msg msg, SendReceiveOptions flags)
        {
            return m_loadBalancer.Send(ref msg, flags);
        }

        public override bool HasOut()
        {
            return m_loadBalancer.HasOut();
        }

        public override bool HasIn()
        {
            return false;
        }

        public override bool Receive(SendReceiveOptions flags, ref Msg msg)
        {
            throw new NotSupportedException();
        }

        public override void ReadActivated(Pipe pipe)
        {
            throw new NotSupportedException();
        }

        public override void Hiccuped(Pipe pipe)
        {
            throw new NotSupportedException();
        }
    }
}
