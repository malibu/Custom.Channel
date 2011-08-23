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

using System.ServiceModel;
using System.ServiceModel.Channels;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Server-side implementation of the sessioned one-way channel.
	/// </summary>
	public class NmsInputSessionChannel : NmsInputChannel, IInputSessionChannel
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsInputSessionChannel"/> class.
		/// </summary>
		/// <param name="factory">The factory that was used to create the channel.</param>
		/// <param name="localAddress">The local address of the channel.</param>
		internal NmsInputSessionChannel(ChannelListenerBase factory, EndpointAddress localAddress)
			: base(factory, localAddress)
		{
		}

		#endregion

		#region ISessionChannel<IInputSession> Members

		/// <summary>
		/// Gets the type of session associated with this channel.
		/// </summary>
		/// <value></value>
		/// <returns>The type of <see cref="T:System.ServiceModel.Channels.ISession"/> associated with this channel. </returns>
		public IInputSession Session
		{
			get { return _session; }
		}

		/// <summary>
		/// Internal implementation of a session, with tracking ID.
		/// </summary>
		private class InputSession : IInputSession, System.ServiceModel.Channels.ISession
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

		private IInputSession _session = new InputSession();

		#endregion
	}
}
