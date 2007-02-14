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

namespace NMS
{
	
	/// <summary>
	/// An object capable of sending messages to some destination
	/// </summary>
	public interface IMessageProducer : System.IDisposable
	{
		
        /// <summary>
        /// Sends the message to the default destination for this producer
        /// </summary>
        void Send(IMessage message);
		
        /// <summary>
        /// Sends the message to the given destination
        /// </summary>
        void Send(IDestination destination, IMessage message);
		
        /// <summary>
        /// Sends the message to the given destination with the explicit QoS configuration
        /// </summary>
		void Send(IDestination destination, IMessage message, bool persistent, byte priority, TimeSpan timeToLive);
        

		bool Persistent
		{
			get;
			set;
		}
		
		TimeSpan TimeToLive
		{
			get;
			set;
		}
		
		byte Priority
		{
			get;
			set;
		}
		
		bool DisableMessageID
		{
			get;
			set;
		}
		
		bool DisableMessageTimestamp
		{
			get;
			set;
		}
    }
}


