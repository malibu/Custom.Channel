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
	/// Base class for NMS output channels.
	/// </summary>
	public abstract class NmsOutputChannelBase : ChannelBase
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsOutputChannelBase"/> class.
		/// </summary>
		/// <param name="factory">The factory that created the channel.</param>
		/// <param name="remoteAddress">The remote address for the channel.</param>
		/// <param name="via">The URI that contains the transport address to which messages are sent on the output channel.</param>
		/// <param name="bufferManager">The buffer manager.</param>
		/// <param name="encoderFactory">The encoder factory.</param>
		/// <param name="destination">The name of the ActiveMQ destination.</param>
		/// <param name="destinationType">The type of the ActiveMQ destination (either a queue or a topic, permanent or temporary).</param>
		internal NmsOutputChannelBase(ChannelManagerBase factory, EndpointAddress remoteAddress, Uri via, BufferManager bufferManager, MessageEncoderFactory encoderFactory, string destination, DestinationType destinationType)
			: base(factory)
		{
			_remoteAddress = remoteAddress;
			_via = via;
			_bufferManager = bufferManager;
			_encoder = encoderFactory;
			_destination = destination;
			_destinationType = destinationType;
		}

		#endregion

		#region NullRequestContextCollection

		//public NmsAsyncRequestContextCollection PendingRequests
		//{
		//    get { return _pendingRequests; }
		//}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets the remote address.
		/// </summary>
		/// <value>The remote address.</value>
		public EndpointAddress RemoteAddress
		{
			get { return _remoteAddress; }
		}

		/// <summary>
		/// Gets the routing address.
		/// </summary>
		/// <value>The routing address.</value>
		public Uri Via
		{
			get { return _via; }
		}

		/// <summary>
		/// Gets the buffer manager.
		/// </summary>
		/// <value>The buffer manager.</value>
		public BufferManager BufferManager
		{
			get { return _bufferManager; }
		}

		/// <summary>
		/// Gets the encoder.
		/// </summary>
		/// <value>The encoder.</value>
		public MessageEncoder Encoder
		{
			get { return _encoder.Encoder; }
		}

		/// <summary>
		/// Gets the name of the destination (either a queue or a topic).
		/// </summary>
		/// <value>The name of the destination.</value>
		public string Destination
		{
			get { return _destination; }
		}

		/// <summary>
		/// Gets the type of the destination.
		/// </summary>
		/// <value>The type of the destination.</value>
		public DestinationType DestinationType
		{
			get { return _destinationType; }
		}

		#endregion

		#region Abort

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the closing state due to the invocation of a synchronous abort operation.
		/// </summary>
		protected override void OnAbort()
		{
			//_pendingRequests.AbortAll();
		}

		#endregion

		#region Close

		/// <summary>
		/// Inserts processing after a communication object transitions to the closing state due to the invocation of an asynchronous close operation.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> that specifies how long the on close operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback"/> delegate that receives notification of the completion of the asynchronous on close operation.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous on close operation.</param>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult"/> that references the asynchronous on close operation.
		/// </returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// 	<paramref name="timeout"/> is less than zero.</exception>
		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			OnClose(timeout);
			return new CompletedAsyncResult(callback, state);
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the closing state due to the invocation of a synchronous close operation.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> that specifies how long the on close operation has to complete before timing out.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// 	<paramref name="timeout"/> is less than zero.</exception>
		protected override void OnClose(TimeSpan timeout)
		{
			//_pendingRequests.AbortAll();
		}

	    protected override void OnEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		#endregion

		#region Open

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the opening state due to the invocation of an asynchronous open operation.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> that specifies how long the on open operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback"/> delegate that receives notification of the completion of the asynchronous on open operation.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous on open operation.</param>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult"/> that references the asynchronous on open operation.
		/// </returns>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// 	<paramref name="timeout"/> is less than zero.</exception>
		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			OnOpen(timeout);
			return new CompletedAsyncResult(callback, state);
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions into the opening state which must complete within a specified interval of time.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> that specifies how long the on open operation has to complete before timing out.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException">
		/// 	<paramref name="timeout"/> is less than zero.</exception>
		/// <exception cref="T:System.TimeoutException">The interval of time specified by <paramref name="timeout"/> that was allotted for the operation was exceeded before the operation was completed.</exception>
		protected override void OnOpen(TimeSpan timeout)
		{
		}

		/// <summary>
		/// Completes an asynchronous operation on the open of a communication object.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult"/> that is returned by a call to the <see cref="M:System.ServiceModel.Channels.CommunicationObject.OnEndOpen(System.IAsyncResult)"/> method.</param>
		/// <exception cref="T:System.TimeoutException">The interval of time specified by <paramref name="timeout"/> that was allotted for the operation was exceeded before the operation was completed.</exception>
		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		#endregion

		#region GetProperty

		/// <summary>
		/// Gets the property.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public override T GetProperty<T>()
		{
			if(typeof(T) == typeof(FaultConverter))
			{
				return FaultConverter.GetDefaultFaultConverter(MessageVersion.Soap12WSAddressing10) as T;
			}
			return base.GetProperty<T>();
		}

		#endregion

		#region Private members

		private EndpointAddress _remoteAddress;
		private Uri _via;
		private BufferManager _bufferManager;
		private MessageEncoderFactory _encoder;
		private string _destination;
		private DestinationType _destinationType;

		// for request/reply pattern
		//NullAsyncRequestContextCollection _pendingRequests = new NullAsyncRequestContextCollection();

		#endregion
	}
}
