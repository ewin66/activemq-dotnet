/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;
using Apache.NMS.ActiveMQ.Commands;
using Apache.NMS.Util;

namespace Apache.NMS.ActiveMQ.State
{
	public class ConnectionState
	{

		ConnectionInfo info;
		private AtomicDictionary<TransactionId, TransactionState> transactions = new AtomicDictionary<TransactionId, TransactionState>();
		private AtomicDictionary<SessionId, SessionState> sessions = new AtomicDictionary<SessionId, SessionState>();
		private AtomicCollection<DestinationInfo> tempDestinations = new AtomicCollection<DestinationInfo>();
		private AtomicBoolean _shutdown = new AtomicBoolean(false);

		public ConnectionState(ConnectionInfo info)
		{
			this.info = info;
			// Add the default session id.
			addSession(new SessionInfo(info, -1));
		}

		public override String ToString()
		{
			return info.ToString();
		}

		public void reset(ConnectionInfo info)
		{
			this.info = info;
			transactions.Clear();
			sessions.Clear();
			tempDestinations.Clear();
			_shutdown.Value = false;
		}

		public void addTempDestination(DestinationInfo info)
		{
			checkShutdown();
			tempDestinations.Add(info);
		}

		public void removeTempDestination(IDestination destination)
		{
			for(int i = tempDestinations.Count - 1; i >= 0; i--)
			{
				DestinationInfo di = tempDestinations[i];
				if(di.Destination.Equals(destination))
				{
					tempDestinations.RemoveAt(i);
				}
			}
		}

		public void addTransactionState(TransactionId id)
		{
			checkShutdown();
			transactions.Add(id, new TransactionState(id));
		}

		/*
		public TransactionState getTransactionState(TransactionId id) {
			return transactions[id];
		}

		public SynchronizedCollection<TransactionState> getTransactionStates() {
			return transactions.Values;
		}

		public SessionState getSessionState(SessionId id) {
			return sessions[id];
		}

		*/

		public TransactionState this[TransactionId id]
		{
			get
			{
				return transactions[id];
			}
		}

		public AtomicCollection<TransactionState> TransactionStates
		{
			get
			{
				return transactions.Values;
			}
		}

		public SessionState this[SessionId id]
		{
			get
			{
				#if DEBUG
				try
				{
				#endif
					return sessions[id];
				#if DEBUG
				}
				catch(System.Collections.Generic.KeyNotFoundException ex)
				{
					// Useful for dignosing missing session ids
					string sessionList = string.Empty;
					foreach(SessionId sessionId in sessions.Keys)
					{
						sessionList += sessionId.ToString() + "\n";
					}
					System.Diagnostics.Debug.Assert(false,
						string.Format("Session '{0}' did not exist in the sessions collection.\n\nSessions:-\n{1}", id, sessionList));
					throw ex;
				}
				#endif
			}
		}

		public TransactionState removeTransactionState(TransactionId id)
		{
			TransactionState ret = transactions[id];
			transactions.Remove(id);
			return ret;
		}

		public void addSession(SessionInfo info)
		{
			checkShutdown();
			sessions.Add(info.SessionId, new SessionState(info));
		}

		public SessionState removeSession(SessionId id)
		{
			SessionState ret = sessions[id];
			sessions.Remove(id);
			return ret;
		}

		public ConnectionInfo Info
		{
			get
			{
				return info;
			}
		}

		public AtomicCollection<SessionId> SessionIds
		{
			get
			{
				return sessions.Keys;
			}
		}

		public AtomicCollection<DestinationInfo> TempDestinations
		{
			get
			{
				return tempDestinations;
			}
		}

		public AtomicCollection<SessionState> SessionStates
		{
			get
			{
				return sessions.Values;
			}
		}

		private void checkShutdown()
		{
			if(_shutdown.Value)
			{
				throw new ApplicationException("Disposed");
			}
		}

		public void shutdown()
		{
			if(_shutdown.CompareAndSet(false, true))
			{
				foreach(SessionState ss in sessions.Values)
				{
					ss.shutdown();
				}
			}
		}
	}
}
