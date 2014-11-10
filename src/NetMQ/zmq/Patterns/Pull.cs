/*
    Copyright (c) 2007-2012 iMatix Corporation
    Copyright (c) 2009-2011 250bpm s.r.o.
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

using System.Diagnostics;
using NetMQ.zmq.Patterns.Utils;

namespace NetMQ.zmq.Patterns
{
    class Pull : SocketBase
    {
        public class PullSession : SessionBase
        {
            public PullSession(IOThread ioThread, bool connect,
                               NetMQSocket socket, Options options,
                               Address addr) : base(ioThread, connect, socket, options, addr)
            {

            }
        }

        //  Fair queueing object for inbound pipes.
        private readonly FairQueueing m_fairQueueing;

        public Pull(NetMQSocket socket, Options options)
            : base(socket, options)
        {
            Options.SocketType = ZmqSocketType.Pull;

            m_fairQueueing = new FairQueueing();
        }

        public override void AddPipe(Pipe pipe, bool subscribeToAll)
        {
            Debug.Assert(pipe != null);
            m_fairQueueing.Attach(pipe);
        }

        public override void ReadActivated(Pipe pipe)
        {
            m_fairQueueing.Activated(pipe);
        }

        public override void RemovePipe(Pipe pipe)
        {
            m_fairQueueing.Terminated(pipe);
        }

        public override bool Receive(SendReceiveOptions flags, ref Msg msg)
        {
            return m_fairQueueing.Recv(ref msg);
        }

        public override bool HasIn()
        {
            return m_fairQueueing.HasIn();
        }

        public override bool HasOut()
        {
            return false;
        }

        public override bool Send(ref Msg msg, SendReceiveOptions flags)
        {
            throw new System.NotSupportedException();
        }

        public override void WriteActivated(Pipe pipe)
        {
            throw new System.NotSupportedException();
        }

        public override void Hiccuped(Pipe pipe)
        {
            throw new System.NotSupportedException();
        }
    }
}
