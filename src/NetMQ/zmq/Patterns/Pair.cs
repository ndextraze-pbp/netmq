/*
    Copyright (c) 2009-2011 250bpm s.r.o.
    Copyright (c) 2007-2009 iMatix Corporation
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

namespace NetMQ.zmq.Patterns
{
    public class Pair : BasePattern
    {

        public class PairSession : SessionBase
        {
            public PairSession(IOThread ioThread, bool connect,
                               SocketBase socket, Options options,
                               Address addr)
                : base(ioThread, connect, socket, options, addr)
            {

            }
        }

        private Pipe m_pipe;

        public Pair(SocketBase socket)
            : base(socket)
        {
            Options.SocketType = ZmqSocketType.Pair;
        }

        public override  void AddPipe(Pipe pipe, bool subscribeToAll)
        {
            Debug.Assert(pipe != null);

            //  ZMQ_PAIR socket can only be connected to a single peer.
            //  The socket rejects any further connection requests.
            if (m_pipe == null)
                m_pipe = pipe;
            else
                pipe.Terminate(false);
        }

        public override void RemovePipe(Pipe pipe)
        {
            if (pipe == m_pipe)
                m_pipe = null;
        }

        public override void ReadActivated(Pipe pipe)
        {
            //  There's just one pipe. No lists of active and inactive pipes.
            //  There's nothing to do here.
        }


        public override void WriteActivated(Pipe pipe)
        {
            //  There's just one pipe. No lists of active and inactive pipes.
            //  There's nothing to do here.
        }

        public override bool Send(ref Msg msg, SendReceiveOptions flags)
        {
            if (m_pipe == null || !m_pipe.Write(ref msg))
            {
                return false;
            }

            if ((flags & SendReceiveOptions.SendMore) == 0)
                m_pipe.Flush();

            //  Detach the original message from the data buffer.
            msg.InitEmpty();

            return true;
        }

        public override bool Receive(SendReceiveOptions flags, ref Msg msg)
        {
            //  Deallocate old content of the message.

            msg.Close();
            if (m_pipe == null || !m_pipe.Read(ref msg))
            {
                msg.InitEmpty();
                return false;
            }
            return true;
        }


        public override bool HasIn()
        {
            if (m_pipe == null)
                return false;

            return m_pipe.CheckRead();
        }

        public override bool HasOut()
        {
            if (m_pipe == null)
                return false;

            return m_pipe.CheckWrite();
        }

        public override void Hiccuped(Pipe pipe)
        {
            throw new System.NotSupportedException();
        }
    }
}
