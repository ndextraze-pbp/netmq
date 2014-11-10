using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using NetMQ.Sockets;
using NetMQ.zmq;
using NetMQ.Monitoring;

namespace NetMQ
{
    /// <summary>
    /// Context class of the NetMQ, you should have only one context in your application
    /// </summary>
    public class NetMQContext : IDisposable
    {
        readonly Ctx m_ctx;
        private int m_isClosed = 0;


        private NetMQContext(Ctx ctx)
        {
            m_ctx = ctx;
        }

        /// <summary>
        /// Create a new context
        /// </summary>
        /// <returns>The new context</returns>
        public static NetMQContext Create()
        {
            return new NetMQContext(new Ctx());
        }

        /// <summary>
        /// Number of IO Threads in the context, default is 1, 1 is good for most cases
        /// </summary>
        public int ThreadPoolSize
        {
            get
            {
                m_ctx.CheckDisposed();

                return m_ctx.Get(ContextOption.IOThreads);                 
            }
            set
            {
                m_ctx.CheckDisposed();

                m_ctx.Set(ContextOption.IOThreads, value);                               
            }
        }

        /// <summary>
        /// Maximum number of sockets
        /// </summary>
        public int MaxSockets
        {
            get
            {
                m_ctx.CheckDisposed();

                return m_ctx.Get(ContextOption.MaxSockets);
            }
            set
            {
                m_ctx.CheckDisposed();

                m_ctx.Set(ContextOption.MaxSockets, value);
            }
        }

        private NetMQSocket CreateHandle(ZmqSocketType socketType)
        {
            m_ctx.CheckDisposed();

            return m_ctx.CreateSocket(socketType);
        }

        public NetMQSocket CreateSocket(ZmqSocketType socketType)
        {
            return CreateHandle(socketType);          
        }

        /// <summary>
        /// Create request socket
        /// </summary>
        /// <returns></returns>
        public RequestSocket CreateRequestSocket()
        {
            return (RequestSocket) CreateHandle(ZmqSocketType.Req);            
        }

        /// <summary>
        /// Create response socket
        /// </summary>
        /// <returns></returns>
        public ResponseSocket CreateResponseSocket()
        {
            return (ResponseSocket) CreateHandle(ZmqSocketType.Rep);            
        }

        /// <summary>
        /// Create dealer socket
        /// </summary>
        /// <returns></returns>
        public DealerSocket CreateDealerSocket()
        {
            return (DealerSocket) CreateHandle(ZmqSocketType.Dealer);            
        }

        /// <summary>
        /// Create router socket
        /// </summary>
        /// <returns></returns>
        public RouterSocket CreateRouterSocket()
        {
            return (RouterSocket) CreateHandle(ZmqSocketType.Router);            
        }

        /// <summary>
        /// Create xpublisher socket
        /// </summary>
        /// <returns></returns>
        public XPublisherSocket CreateXPublisherSocket()
        {
            return (XPublisherSocket)CreateHandle(ZmqSocketType.Xpub);            
        }

        /// <summary>
        /// Create pair socket
        /// </summary>
        /// <returns></returns>
        public PairSocket CreatePairSocket()
        {
            return (PairSocket)CreateHandle(ZmqSocketType.Pair);            
        }

        /// <summary>
        /// Create push socket
        /// </summary>
        /// <returns></returns>
        public PushSocket CreatePushSocket()
        {
            return (PushSocket) CreateHandle(ZmqSocketType.Push);            
        }

        /// <summary>
        /// Create publisher socket
        /// </summary>
        /// <returns></returns>
        public PublisherSocket CreatePublisherSocket()
        {
            return (PublisherSocket) CreateHandle(ZmqSocketType.Pub);            
        }

        /// <summary>
        /// Create pull socket
        /// </summary>
        /// <returns></returns>
        public PullSocket CreatePullSocket()
        {
            return (PullSocket)CreateHandle(ZmqSocketType.Pull);            
        }

        /// <summary>
        /// Create subscriber socket
        /// </summary>
        /// <returns></returns>
        public SubscriberSocket CreateSubscriberSocket()
        {
            return (SubscriberSocket) CreateHandle(ZmqSocketType.Sub);            
        }

        /// <summary>
        /// Create xsub socket
        /// </summary>
        /// <returns></returns>
        public XSubscriberSocket CreateXSubscriberSocket()
        {
            return (XSubscriberSocket) CreateHandle(ZmqSocketType.Xsub);            
        }

        public StreamSocket CreateStreamSocket()
        {
            return (StreamSocket) CreateHandle(ZmqSocketType.Stream);            
        }

        public NetMQMonitor CreateMonitorSocket(string endpoint)
        {
            if (endpoint == null)
            {
                throw new ArgumentNullException("endpoint");
            }

            if (endpoint == string.Empty)
            {
                throw new ArgumentException("Unable to monitor to an empty endpoint.", "endpoint");
            }

            return new NetMQMonitor(CreatePairSocket(), endpoint);
        }

        /// <summary>
        /// Close the context
        /// </summary>
        public void Terminate()
        {
            if (Interlocked.CompareExchange(ref m_isClosed, 1, 0) == 0)
            {
                m_ctx.CheckDisposed();

                m_ctx.Terminate();                
            }
        }

        public void Dispose()
        {
            Terminate();
        }
    }
}
