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
using System.Collections;
using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS;

namespace Apache.NMS.ActiveMQ
{
	/// <summary>
	/// Default provider of ISession
	/// </summary>
	public class Session : ISession
	{
		private long consumerCounter;
		private readonly IDictionary consumers = Hashtable.Synchronized(new Hashtable());
		private readonly IDictionary producers = Hashtable.Synchronized(new Hashtable());
		private readonly DispatchingThread dispatchingThread;
		private DispatchingThread.ExceptionHandler dispatchingThread_ExceptionHandler;
		private readonly SessionInfo info;
		private long producerCounter;
		internal bool startedAsyncDelivery = false;
		private bool disposed = false;
        private bool closed = false;

		public Session(Connection connection, SessionInfo info, AcknowledgementMode acknowledgementMode)
		{
			this.connection = connection;
			this.info = info;
			this.acknowledgementMode = acknowledgementMode;
			this.AsyncSend = connection.AsyncSend;
			this.requestTimeout = connection.RequestTimeout;
			this.PrefetchSize = 1000;
			this.transactionContext = new TransactionContext(this);
			this.dispatchingThread = new DispatchingThread(new DispatchingThread.DispatchFunction(DispatchAsyncMessages));
			this.dispatchingThread_ExceptionHandler = new DispatchingThread.ExceptionHandler(dispatchingThread_ExceptionListener);
		}

		~Session()
		{
			Dispose(false);
		}

		/// <summary>
		/// Sets the prefetch size, the maximum number of messages a broker will dispatch to consumers
		/// until acknowledgements are received.
		/// </summary>
		public int PrefetchSize;

		/// <summary>
		/// Sets the maximum number of messages to keep around per consumer
		/// in addition to the prefetch window for non-durable topics until messages
		/// will start to be evicted for slow consumers.
		/// Must be > 0 to enable this feature
		/// </summary>
		public int MaximumPendingMessageLimit;

		/// <summary>
		/// Enables or disables whether asynchronous dispatch should be used by the broker
		/// </summary>
		public bool DispatchAsync;

		/// <summary>
		/// Enables or disables exclusive consumers when using queues. An exclusive consumer means
		/// only one instance of a consumer is allowed to process messages on a queue to preserve order
		/// </summary>
		public bool Exclusive;

		/// <summary>
		/// Enables or disables retroactive mode for consumers; i.e. do they go back in time or not?
		/// </summary>
		public bool Retroactive;

		/// <summary>
		/// Sets the default consumer priority for consumers
		/// </summary>
		public byte Priority;

		/// <summary>
		/// This property indicates whether or not async send is enabled.
		/// </summary>
		public bool AsyncSend;

		private Connection connection;
		public Connection Connection
		{
			get { return this.connection; }
		}

		public SessionId SessionId
		{
			get { return info.SessionId; }
		}

		private TransactionContext transactionContext;
		public TransactionContext TransactionContext
		{
			get { return this.transactionContext; }
		}

		#region ISession Members

		public void Dispose()
		{
			Dispose(true);
			GC.SuppressFinalize(this);
		}

		protected void Dispose(bool disposing)
		{
			if(this.disposed)
			{
				return;
			}

			if(disposing)
			{
				// Dispose managed code here.
			}

			try
			{
				Close();
			}
			catch
			{
				// Ignore network errors.
			}

			this.disposed = true;
		}

		public void Close()
		{
			lock(this)
			{
				if(this.closed)
				{
					return;
				}

				try
				{
					StopAsyncDelivery();
					Connection.RemoveSession(this);
					foreach(MessageConsumer consumer in GetConsumers())
					{
						consumer.Close();
					}
					consumers.Clear();

					foreach(MessageProducer producer in GetProducers())
					{
						producer.Close();
					}
					producers.Clear();
				}
				catch(Exception ex)
				{
					Tracer.ErrorFormat("Error during session close: {0}", ex);
				}

				this.connection = null;
				this.closed = true;
			}
		}

		public IMessageProducer CreateProducer()
		{
			return CreateProducer(null);
		}

		public IMessageProducer CreateProducer(IDestination destination)
		{
			ProducerInfo command = CreateProducerInfo(destination);
			ProducerId producerId = command.ProducerId;
			MessageProducer producer = null;

			try
			{
				producer = new MessageProducer(this, command);
				producers[producerId] = producer;
				this.DoSend(command);
			}
			catch(Exception)
			{
				if(producer != null)
				{
					producer.Close();
				}

				throw;
			}

			return producer;
		}

		public IMessageConsumer CreateConsumer(IDestination destination)
		{
			return CreateConsumer(destination, null, false);
		}

		public IMessageConsumer CreateConsumer(IDestination destination, string selector)
		{
			return CreateConsumer(destination, selector, false);
		}

		public IMessageConsumer CreateConsumer(IDestination destination, string selector, bool noLocal)
		{
			ConsumerInfo command = CreateConsumerInfo(destination, selector);
			command.NoLocal = noLocal;
			command.AcknowledgementMode = this.AcknowledgementMode;

			ConsumerId consumerId = command.ConsumerId;
			MessageConsumer consumer = null;

			try
			{
				consumer = new MessageConsumer(this, command, this.AcknowledgementMode);
				// lets register the consumer first in case we start dispatching messages immediately
				consumers[consumerId] = consumer;
				this.DoSend(command);
				return consumer;
			}
			catch(Exception)
			{
				if(consumer != null)
				{
					consumer.Close();
				}

				throw;
			}
		}

		public IMessageConsumer CreateDurableConsumer(ITopic destination, string name, string selector, bool noLocal)
		{
			ConsumerInfo command = CreateConsumerInfo(destination, selector);
			ConsumerId consumerId = command.ConsumerId;
			command.SubscriptionName = name;
			command.NoLocal = noLocal;
			MessageConsumer consumer = null;

			try
			{
				consumer = new MessageConsumer(this, command, this.AcknowledgementMode);
				// lets register the consumer first in case we start dispatching messages immediately
				consumers[consumerId] = consumer;
				this.DoSend(command);
			}
			catch(Exception)
			{
				if(consumer != null)
				{
					consumer.Close();
				}

				throw;
			}

			return consumer;
		}

		public void DeleteDurableConsumer(string name)
		{
			RemoveSubscriptionInfo command = new RemoveSubscriptionInfo();
			command.ConnectionId = Connection.ConnectionId;
			command.ClientId = Connection.ClientId;
			command.SubcriptionName = name;

			this.DoSend(command);
		}

		public IQueue GetQueue(string name)
		{
			return new ActiveMQQueue(name);
		}

		public ITopic GetTopic(string name)
		{
			return new ActiveMQTopic(name);
		}

		public ITemporaryQueue CreateTemporaryQueue()
		{
			ActiveMQTempQueue answer = new ActiveMQTempQueue(Connection.CreateTemporaryDestinationName());
			CreateTemporaryDestination(answer);
			return answer;
		}

		public ITemporaryTopic CreateTemporaryTopic()
		{
			ActiveMQTempTopic answer = new ActiveMQTempTopic(Connection.CreateTemporaryDestinationName());
			CreateTemporaryDestination(answer);
			return answer;
		}


		public IMessage CreateMessage()
		{
			ActiveMQMessage answer = new ActiveMQMessage();
			Configure(answer);
			return answer;
		}


		public ITextMessage CreateTextMessage()
		{
			ActiveMQTextMessage answer = new ActiveMQTextMessage();
			Configure(answer);
			return answer;
		}

		public ITextMessage CreateTextMessage(string text)
		{
			ActiveMQTextMessage answer = new ActiveMQTextMessage(text);
			Configure(answer);
			return answer;
		}

		public IMapMessage CreateMapMessage()
		{
			return new ActiveMQMapMessage();
		}

		public IBytesMessage CreateBytesMessage()
		{
			return new ActiveMQBytesMessage();
		}

		public IBytesMessage CreateBytesMessage(byte[] body)
		{
			ActiveMQBytesMessage answer = new ActiveMQBytesMessage();
			answer.Content = body;
			return answer;
		}

		public IObjectMessage CreateObjectMessage(object body)
		{
			ActiveMQObjectMessage answer = new ActiveMQObjectMessage();
			answer.Body = body;
			return answer;
		}

		public void Commit()
		{
			if(!Transacted)
			{
				throw new InvalidOperationException(
						"You cannot perform a Commit() on a non-transacted session. Acknowlegement mode is: "
						+ this.AcknowledgementMode);
			}
			this.TransactionContext.Commit();
		}

		public void Rollback()
		{
			if(!Transacted)
			{
				throw new InvalidOperationException(
						"You cannot perform a Commit() on a non-transacted session. Acknowlegement mode is: "
						+ this.AcknowledgementMode);
			}
			this.TransactionContext.Rollback();

			// lets ensure all the consumers redeliver any rolled back messages
			foreach(MessageConsumer consumer in GetConsumers())
			{
				consumer.RedeliverRolledBackMessages();
			}
		}


		// Properties

		private TimeSpan requestTimeout;
		public TimeSpan RequestTimeout
		{
			get { return this.requestTimeout; }
			set { this.requestTimeout = value; }
		}

		public bool Transacted
		{
			get { return this.AcknowledgementMode == AcknowledgementMode.Transactional; }
		}

		private AcknowledgementMode acknowledgementMode;
		public AcknowledgementMode AcknowledgementMode
		{
			get { return this.acknowledgementMode; }
		}

		#endregion

		private void dispatchingThread_ExceptionListener(Exception exception)
		{
			if(null != Connection)
			{
				try
				{
					Connection.OnSessionException(this, exception);
				}
				catch
				{
				}
			}
		}

		protected void CreateTemporaryDestination(ActiveMQDestination tempDestination)
		{
			DestinationInfo command = new DestinationInfo();
			command.ConnectionId = Connection.ConnectionId;
			command.OperationType = 0; // 0 is add
			command.Destination = tempDestination;

			this.DoSend(command);
		}

		protected void DestroyTemporaryDestination(ActiveMQDestination tempDestination)
		{
			DestinationInfo command = new DestinationInfo();
			command.ConnectionId = Connection.ConnectionId;
			command.OperationType = 1; // 1 is remove
			command.Destination = tempDestination;

			this.DoSend(command);
		}

		public void DoSend(Command message)
		{
			this.DoSend(message, this.RequestTimeout);
		}

		public void DoSend(Command message, TimeSpan requestTimeout)
		{
			if(AsyncSend)
			{
				Connection.OneWay(message);
			}
			else
			{
				Connection.SyncRequest(message, requestTimeout);
			}
		}

		/// <summary>
		/// Ensures that a transaction is started
		/// </summary>
		public void DoStartTransaction()
		{
			if(Transacted)
			{
				this.TransactionContext.Begin();
			}
		}

		public void DisposeOf(ConsumerId objectId)
		{
			Connection.DisposeOf(objectId);
			consumers.Remove(objectId);
		}

		public void DisposeOf(ProducerId objectId)
		{
			Connection.DisposeOf(objectId);
			producers.Remove(objectId);
		}

		public bool DispatchMessage(ConsumerId consumerId, Message message)
		{
			bool dispatched = false;
			MessageConsumer consumer = (MessageConsumer) consumers[consumerId];

			if(consumer != null)
			{
				consumer.Dispatch((ActiveMQMessage) message);
				dispatched = true;
			}

			return dispatched;
		}

		/// <summary>
		/// Private method called by the dispatcher thread in order to perform
		/// asynchronous delivery of queued (inbound) messages.
		/// </summary>
		private void DispatchAsyncMessages()
		{
			// lets iterate through each consumer created by this session
			// ensuring that they have all pending messages dispatched
			foreach(MessageConsumer consumer in GetConsumers())
			{
				consumer.DispatchAsyncMessages();
			}
		}

		/// <summary>
		/// Returns a copy of the current consumers in a thread safe way to avoid concurrency
		/// problems if the consumers are changed in another thread
		/// </summary>
		protected ICollection GetConsumers()
		{
			lock(consumers.SyncRoot)
			{
				return new ArrayList(consumers.Values);
			}
		}

		/// <summary>
		/// Returns a copy of the current consumers in a thread safe way to avoid concurrency
		/// problems if the consumers are changed in another thread
		/// </summary>
		protected ICollection GetProducers()
		{
			lock(producers.SyncRoot)
			{
				return new ArrayList(producers.Values);
			}
		}

		protected virtual ConsumerInfo CreateConsumerInfo(IDestination destination, string selector)
		{
			ConsumerInfo answer = new ConsumerInfo();
			ConsumerId id = new ConsumerId();
			id.ConnectionId = info.SessionId.ConnectionId;
			id.SessionId = info.SessionId.Value;
			lock(this)
			{
				id.Value = ++consumerCounter;
			}
			answer.ConsumerId = id;
			answer.Destination = ActiveMQDestination.Transform(destination);
			answer.Selector = selector;
			answer.PrefetchSize = this.PrefetchSize;
			answer.Priority = this.Priority;
			answer.Exclusive = this.Exclusive;
			answer.DispatchAsync = this.DispatchAsync;
			answer.Retroactive = this.Retroactive;

			// If the destination contained a URI query, then use it to set public properties
			// on the ConsumerInfo
			ActiveMQDestination amqDestination = destination as ActiveMQDestination;
			if(amqDestination != null && amqDestination.Options != null)
			{
				Util.URISupport.SetProperties(answer, amqDestination.Options, "consumer.");
			}

			return answer;
		}

		protected virtual ProducerInfo CreateProducerInfo(IDestination destination)
		{
			ProducerInfo answer = new ProducerInfo();
			ProducerId id = new ProducerId();
			id.ConnectionId = info.SessionId.ConnectionId;
			id.SessionId = info.SessionId.Value;
			lock(this)
			{
				id.Value = ++producerCounter;
			}
			answer.ProducerId = id;
			answer.Destination = ActiveMQDestination.Transform(destination);

			// If the destination contained a URI query, then use it to set public
			// properties on the ProducerInfo
			ActiveMQDestination amqDestination = destination as ActiveMQDestination;
			if(amqDestination != null && amqDestination.Options != null)
			{
				Util.URISupport.SetProperties(answer, amqDestination.Options, "producer.");
			}

			return answer;
		}

		/// <summary>
		/// Configures the message command
		/// </summary>
		protected void Configure(ActiveMQMessage message)
		{
		}

		internal void StopAsyncDelivery()
		{
			if(startedAsyncDelivery)
			{
				this.dispatchingThread.ExceptionListener -= this.dispatchingThread_ExceptionHandler;
				dispatchingThread.Stop(5000);
				startedAsyncDelivery = false;
			}
		}

		internal void StartAsyncDelivery()
		{
			if(!startedAsyncDelivery)
			{
				this.dispatchingThread.ExceptionListener += this.dispatchingThread_ExceptionHandler;
				dispatchingThread.Start();
				startedAsyncDelivery = true;
			}
		}

		internal void RegisterConsumerDispatcher(Dispatcher dispatcher)
		{
			dispatcher.SetAsyncDelivery(this.dispatchingThread.EventHandle);
		}
	}
}
