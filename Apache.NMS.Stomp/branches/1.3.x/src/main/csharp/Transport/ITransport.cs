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
using Apache.NMS.Stomp.Commands;

namespace Apache.NMS.Stomp.Transport
{
	public delegate void CommandHandler(ITransport sender, Command command);
	public delegate void ExceptionHandler(ITransport sender, Exception command);
	public delegate void InterruptedHandler(ITransport sender);
	public delegate void ResumedHandler(ITransport sender);

	/// <summary>
	/// Represents the logical networking transport layer.  Transports implment the low
    /// level protocol specific portion of the Communication between the Client and a Broker
    /// such as TCP, UDP, etc.  Transports make use of WireFormat objects to handle translateing
    /// the cononical OpenWire Commands used in this client into binary wire level packets that
    /// can be sent to the Broker or Service that the Transport connects to.
	/// </summary>
	public interface ITransport : IStartable, IDisposable, IStoppable
	{
        /// <summary>
        /// Sends a Command object on the Wire but does not wait for any response from the
        /// receiver before returning.  
        /// </summary>
        /// <param name="command">
        /// A <see cref="Command"/>
        /// </param>
		void Oneway(Command command);

        /// <summary>
        /// Sends a Command object which requires a response from the Broker but does not
        /// wait for the response, instead a FutureResponse object is returned that the
        /// caller can use to wait on the Broker's response.
        /// </summary>
        /// <param name="command">
        /// A <see cref="Command"/>
        /// </param>
        /// <returns>
        /// A <see cref="FutureResponse"/>
        /// </returns>
		FutureResponse AsyncRequest(Command command);

        /// <summary>
        /// Sends a Command to the Broker and waits for a Response to that Command before
        /// returning, this version waits indefinitely for a response.
        /// </summary>
        /// <param name="command">
        /// A <see cref="Command"/>
        /// </param>
        /// <returns>
        /// A <see cref="Response"/>
        /// </returns>
		Response Request(Command command);

        /// <summary>
        /// Sends a Command to the Broker and waits for the given TimeSpan to expire for a
        /// response before returning.  
        /// </summary>
        /// <param name="command">
        /// A <see cref="Command"/>
        /// </param>
        /// <param name="timeout">
        /// A <see cref="TimeSpan"/>
        /// </param>
        /// <returns>
        /// A <see cref="Response"/>
        /// </returns>
		Response Request(Command command, TimeSpan timeout);

        /// <summary>
        /// Allows a caller to find a specific type of Transport in the Chain of
        /// Transports that is created.  This allows a caller to find a specific
        /// object in the Transport chain and set or get properties on that specific
        /// instance.  If the requested type isn't in the chain than Null is returned.
        /// </summary>
        /// <param name="type">
        /// A <see cref="Type"/>
        /// </param>
        /// <returns>
        /// A <see cref="System.Object"/>
        /// </returns>
        Object Narrow(Type type);            
        
        /// <value>
        /// The time that the Transport waits before considering a request to have
        /// failed and throwing an exception.
        /// </value>
        TimeSpan RequestTimeout
        {
            get;
            set;
        }

		CommandHandler Command
		{
			get;
			set;
		}

		ExceptionHandler Exception
		{
			get;
			set;
		}

		InterruptedHandler Interrupted
		{
			get;
			set;
		}		

		ResumedHandler Resumed
		{
			get;
			set;
		}		

        /// <value>
        /// Indicates if this Transport has already been disposed and can no longer
        /// be used.
        /// </value>
		bool IsDisposed
		{
			get;
		}

        /// <value>
        /// Indicates if this Transport is Fault Tolerant or not.  A fault Tolerant
        /// Transport handles low level connection errors internally allowing a client
        /// to remain unaware of wire level disconnection and reconnection details.
        /// </value>
        bool IsFaultTolerant
        {
            get;
        }

        /// <value>
        /// Indiciates if the Transport is current Connected to is assigned URI.
        /// </value>
        bool IsConnected
        {
            get;
        }

        /// <value>
        /// The Remote Address that this transport is currently connected to.
        /// </value>
        Uri RemoteAddress
        {
            get;
        }
        
	}
}

