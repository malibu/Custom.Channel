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
using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Client-side implementation of the sessioned one-way channel.
	/// </summary>
	internal class NmsOutputSessionChannel : NmsOutputChannel, IOutputSessionChannel
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsOutputSessionChannel"/> class.
		/// </summary>
		/// <param name="factory">The factory that created this channel.</param>
		/// <param name="address">The address of this channel.</param>
		/// <param name="via">The URI that contains the transport address to which messages are sent on the output channel.</param>
		/// <param name="bufferManager">The buffer manager.</param>
		/// <param name="encoderFactory">The encoder factory.</param>
		/// <param name="destination">The name of the ActiveMQ destination.</param>
		/// <param name="destinationType">The type of the ActiveMQ destination (either a queue or a topic, permanent or temporary).</param>
		public NmsOutputSessionChannel(ChannelManagerBase factory, Uri via, EndpointAddress address, BufferManager bufferManager, MessageEncoderFactory encoderFactory, string destination, DestinationType destinationType)
			: base(factory, address, via, bufferManager, encoderFactory, destination, destinationType)
		{
		}

		#endregion

		#region ISessionChannel<IOutputSession> Members

		/// <summary>
		/// Gets the type of session associated with this channel.
		/// </summary>
		/// <value></value>
		/// <returns>The type of <see cref="T:System.ServiceModel.Channels.ISession"/> associated with this channel. </returns>
		public IOutputSession Session
		{
			get { return _session; }
		}

		/// <summary>
		/// Internal implementation of a session, with tracking ID.
		/// </summary>
		private class OutputSession : IOutputSession, System.ServiceModel.Channels.ISession
		{
			private string _sessionId = NmsChannelHelper.CreateUniqueSessionId();

			/// <summary>
			/// Gets the ID that uniquely identifies the session.
			/// </summary>
			/// <value></value>
			/// <returns>The ID that uniquely identifies the session. </returns>
			public string Id
			{
				get { return _sessionId; }
			}
		}

		#endregion

		#region Private members

		private IOutputSession _session = new OutputSession();

		#endregion
	}
}
