/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using NUnit.Framework;
using NUnit.Framework.Extensions;
using Apache.NMS.Util;

namespace Apache.NMS.Test
{
	[TestFixture]
	public class DurableTest : NMSTestSupport
	{
		protected static string TEST_CLIENT_ID = "TestDurableConsumerClientId";
		protected static string SEND_CLIENT_ID = "TestDurableProducerClientId";
		protected static string DURABLE_TOPIC = "TestDurableConsumerTopic";
		protected static string CONSUMER_ID = "TestDurableConsumerConsumerId";
		protected static string DURABLE_SELECTOR = "2 > 1";

#if !NET_1_1
		[RowTest]
		[Row(MsgDeliveryMode.Persistent)]
		[Row(MsgDeliveryMode.NonPersistent)]
#endif
		public void TestDurableConsumerSelectorChange(MsgDeliveryMode deliveryMode)
		{
			try
			{
				using(IConnection connection = CreateConnection(TEST_CLIENT_ID))
				{
					connection.Start();
					using(ISession session = connection.CreateSession(AcknowledgementMode.AutoAcknowledge))
					{
						ITopic topic = SessionUtil.GetTopic(session, DURABLE_TOPIC);
						IMessageProducer producer = session.CreateProducer(topic);
						IMessageConsumer consumer = session.CreateDurableConsumer(topic, CONSUMER_ID, "color='red'", false);

						producer.DeliveryMode = deliveryMode;

						// Send the messages
						ITextMessage sendMessage = session.CreateTextMessage("1st");
						sendMessage.Properties["color"] = "red";
						producer.Send(sendMessage);

						ITextMessage receiveMsg = consumer.Receive(receiveTimeout) as ITextMessage;
						Assert.IsNotNull(receiveMsg, "Failed to retrieve 1st durable message.");
						Assert.AreEqual("1st", receiveMsg.Text);
						Assert.AreEqual(deliveryMode, receiveMsg.NMSDeliveryMode, "NMSDeliveryMode does not match");
						receiveMsg.Acknowledge();

						// Change the subscription.
						consumer.Dispose();
						consumer = session.CreateDurableConsumer(topic, CONSUMER_ID, "color='blue'", false);

						sendMessage = session.CreateTextMessage("2nd");
						sendMessage.Properties["color"] = "red";
						producer.Send(sendMessage);
						sendMessage = session.CreateTextMessage("3rd");
						sendMessage.Properties["color"] = "blue";
						producer.Send(sendMessage);

						// Selector should skip the 2nd message.
						receiveMsg = consumer.Receive(receiveTimeout) as ITextMessage;
						Assert.IsNotNull(receiveMsg, "Failed to retrieve durable message.");
						Assert.AreEqual("3rd", receiveMsg.Text, "Retrieved the wrong durable message.");
						Assert.AreEqual(deliveryMode, receiveMsg.NMSDeliveryMode, "NMSDeliveryMode does not match");
						receiveMsg.Acknowledge();

						// Make sure there are no pending messages.
						Assert.IsNull(consumer.ReceiveNoWait(), "Wrong number of messages in durable subscription.");
					}
				}
			}
			catch(Exception ex)
			{
				Assert.Fail(ex.Message);
			}
			finally
			{
				UnregisterDurableConsumer(TEST_CLIENT_ID, CONSUMER_ID);
			}
		}

#if !NET_1_1
		[RowTest]
		[Row(MsgDeliveryMode.Persistent, AcknowledgementMode.AutoAcknowledge)]
		[Row(MsgDeliveryMode.Persistent, AcknowledgementMode.ClientAcknowledge)]
		[Row(MsgDeliveryMode.Persistent, AcknowledgementMode.DupsOkAcknowledge)]
		[Row(MsgDeliveryMode.Persistent, AcknowledgementMode.Transactional)]

		[Row(MsgDeliveryMode.NonPersistent, AcknowledgementMode.AutoAcknowledge)]
		[Row(MsgDeliveryMode.NonPersistent, AcknowledgementMode.ClientAcknowledge)]
		[Row(MsgDeliveryMode.NonPersistent, AcknowledgementMode.DupsOkAcknowledge)]
		[Row(MsgDeliveryMode.NonPersistent, AcknowledgementMode.Transactional)]
#endif
		public void TestDurableConsumer(MsgDeliveryMode deliveryMode, AcknowledgementMode ackMode)
		{
			try
			{
				RegisterDurableConsumer(TEST_CLIENT_ID, DURABLE_TOPIC, CONSUMER_ID, DURABLE_SELECTOR, false);
				RunTestDurableConsumer(deliveryMode, ackMode);
				if(AcknowledgementMode.Transactional == ackMode)
				{
					RunTestDurableConsumer(deliveryMode, ackMode);
				}
			}
			finally
			{
				UnregisterDurableConsumer(TEST_CLIENT_ID, CONSUMER_ID);
			}
		}

		protected void RunTestDurableConsumer(MsgDeliveryMode deliveryMode, AcknowledgementMode ackMode)
		{
			SendDurableMessage(deliveryMode);

			using(IConnection connection = CreateConnection(TEST_CLIENT_ID))
			{
				connection.Start();
				using(ISession session = connection.CreateSession(AcknowledgementMode.Transactional))
				{
					ITopic topic = SessionUtil.GetTopic(session, DURABLE_TOPIC);
					using(IMessageConsumer consumer = session.CreateDurableConsumer(topic, CONSUMER_ID, DURABLE_SELECTOR, false))
					{
						IMessage msg = consumer.Receive(receiveTimeout);
						Assert.IsNotNull(msg, "Did not receive first durable message.");
						msg.Acknowledge();
						SendDurableMessage(deliveryMode);

						msg = consumer.Receive(receiveTimeout);
						Assert.IsNotNull(msg, "Did not receive second durable message.");
						msg.Acknowledge();

						if(AcknowledgementMode.Transactional == ackMode)
						{
							session.Commit();
						}
					}
				}
			}
		}

		protected void SendDurableMessage(MsgDeliveryMode deliveryMode)
		{
			using(IConnection connection = CreateConnection(SEND_CLIENT_ID))
			{
				connection.Start();
				using(ISession session = connection.CreateSession(AcknowledgementMode.DupsOkAcknowledge))
				{
					ITopic topic = SessionUtil.GetTopic(session, DURABLE_TOPIC);
					using(IMessageProducer producer = session.CreateProducer(topic))
					{
						ITextMessage message = session.CreateTextMessage("Durable Hello");

						producer.DeliveryMode = deliveryMode;
						producer.RequestTimeout = receiveTimeout;
						producer.Send(message);
					}
				}
			}
		}
	}
}
