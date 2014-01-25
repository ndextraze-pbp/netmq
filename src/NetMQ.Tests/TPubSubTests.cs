using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NetMQ.Sockets;
using NetMQ.zmq;
using NUnit.Framework;

namespace NetMQ.Tests
{
    [TestFixture]
    public class TPubSubTests
    {
        [Test]
        public void Simple()
        {
            using (NetMQContext context = NetMQContext.Create())
            {
                using (TPublisherSocket publisherSocket = context.CreateTPublisherSocket())
                {
                    publisherSocket.Bind("tcp://127.0.0.1:5557");

                    using (TSubscriberSocket subscriberSocket = context.CreateTSubscriberSocket())
                    {
                        subscriberSocket.SetToken("all");
                        subscriberSocket.Connect("tcp://127.0.0.1:5557");                        

                        // first is the identity
                        byte[] identity = publisherSocket.Receive();

                        // now is the token, token always start with zero
                        byte[] token = publisherSocket.Receive();                        

                        Assert.AreEqual(token[0], 0);

                        string tokenString = Encoding.ASCII.GetString(token, 1, token.Length - 1);

                        Assert.AreEqual(tokenString, "all");

                        publisherSocket.SelectPeer(identity);
                        publisherSocket.SubscribePeer("");

                        publisherSocket.Send("Hello");

                        string messsage = subscriberSocket.ReceiveString();

                        Assert.AreEqual("Hello", messsage);
                    }
                }
            }
        }
    }
}
