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
using System.Text;
using Apache.NMS.Util;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Server-side listener for sessionless input channels.
	/// </summary>
	public class NmsInputChannelListener : ChannelListenerBase<IInputChannel>
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsInputChannelListener"/> class.
		/// </summary>
		/// <param name="transportElement">The binding element.</param>
		/// <param name="context">The context.</param>
		internal NmsInputChannelListener(NmsTransportBindingElement transportElement, BindingContext context)
			: base(context.Binding)
		{
			_bufferManager = BufferManager.CreateBufferManager(transportElement.MaxBufferPoolSize, (int) transportElement.MaxReceivedMessageSize);

			MessageEncodingBindingElement messageEncoderBindingElement = context.BindingParameters.Remove<MessageEncodingBindingElement>();
			_messageEncoderFactory = (messageEncoderBindingElement != null)
				? messageEncoderBindingElement.CreateMessageEncoderFactory()
				: NmsConstants.DefaultMessageEncoderFactory;

			_channelQueue = new InputQueue<IInputChannel>();
			_currentChannelLock = new object();
			_destinationName = transportElement.Destination;
			_destinationType = transportElement.DestinationType;
			_uri = new Uri(context.ListenUriBaseAddress, context.ListenUriRelativeAddress);
			Tracer.DebugFormat("Listening to {0} at {1}/{2}", _destinationType, _uri, _destinationName);
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets the message encoder factory.
		/// </summary>
		/// <value>The message encoder factory.</value>
		public MessageEncoderFactory MessageEncoderFactory
		{
			get { return _messageEncoderFactory; }
		}

		/// <summary>
		/// Gets or sets the destination.
		/// </summary>
		/// <value>The destination.</value>
		public string Destination
		{
			get { return _destinationName; }
			set { _destinationName = value; }
		}

		/// <summary>
		/// Gets or sets the type of the destination.
		/// </summary>
		/// <value>The type of the destination.</value>
		public DestinationType DestinationType
		{
			get { return _destinationType; }
			set { _destinationType = value; }
		}

		#endregion

		#region Implementation of CommunicationObject

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the closing state 
		/// due to the invocation of a synchronous abort operation.
		/// </summary>
		/// <remarks>
		/// Abort can be called at any time, so we can't assume that we've been Opened successfully 
		/// (and thus may not have any listen sockets).
		/// </remarks>
		protected override void OnAbort()
		{
			OnClose(TimeSpan.Zero);
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the closing state due to the invocation of a synchronous close operation.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on close operation has to complete before timing out.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		protected override void OnClose(TimeSpan timeout)
		{
			lock(ThisLock)
			{
				if(_consumer != null)
				{
					Tracer.Debug("Listener is terminating consumer...");
					_consumer.Close();
					_consumer.Dispose();
					Tracer.Debug("Listener has terminated consumer");
				}

				if(_session != null)
				{
					Tracer.Debug("Listener is terminating session...");
					_session.Close();
					Tracer.Debug("Listener has terminated session");
				}

				if(_connection != null)
				{
					Tracer.Debug("Listener is terminating connection...");
					_connection.Stop();
					_connection.Close();
					_connection.Dispose();
					Tracer.Debug("Listener has terminated connection");
				}

				_channelQueue.Close();
			}
		}

		/// <summary>
		/// Inserts processing after a communication object transitions to the closing state due to the invocation of an asynchronous close operation.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous on close operation. 
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on close operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives notification of the completion of the asynchronous on close operation.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous on close operation.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		protected override IAsyncResult OnBeginClose(TimeSpan timeout, AsyncCallback callback, object state)
		{
			OnClose(timeout);
			return new CompletedAsyncResult(callback, state);
		}

		/// <summary>
		/// Completes an asynchronous operation on the close of a communication object.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> that is returned by a call to the <see cref="M:System.ServiceModel.Channels.CommunicationObject.OnEndClose(System.IAsyncResult)" /> method.</param>
		protected override void OnEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions into the opening state which must complete within a specified interval of time.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on open operation has to complete before timing out.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		/// <exception cref="T:System.TimeoutException">The interval of time specified by <paramref name="timeout" /> that was allotted for the operation was exceeded before the operation was completed.</exception>
		protected override void OnOpen(TimeSpan timeout)
		{
			if(Uri == null)
			{
				throw new InvalidOperationException("Uri must be set before ChannelListener is opened.");
			}
			NmsChannelHelper.ValidateTimeout(timeout);
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the opening state due to the invocation of an asynchronous open operation.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous on open operation. 
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on open operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives notification of the completion of the asynchronous on open operation.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous on open operation.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		protected override IAsyncResult OnBeginOpen(TimeSpan timeout, AsyncCallback callback, object state)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			OnOpen(timeout);
			return new CompletedAsyncResult(callback, state);
		}

		/// <summary>
		/// Completes an asynchronous operation on the open of a communication object.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> that is returned by a call to the <see cref="M:System.ServiceModel.Channels.CommunicationObject.OnEndOpen(System.IAsyncResult)" /> method.</param>
		protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		#endregion

		#region Implementation of ChannelListenerBase

		/// <summary>
		/// When implemented in derived class, gets the URI on which the channel listener listens for an incoming channel.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.Uri" /> on which the channel listener listens for incoming channels.
		/// </returns>
		public override Uri Uri
		{
			get { return _uri; }
		}

		/// <summary>
		/// When overridden in a derived class, provides a point of extensibility when waiting for a channel to arrive.
		/// </summary>
		/// <returns>
		/// true if the method completed before the interval of time specified by the <paramref name="timeout" /> expired; otherwise false.
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on wait for a channel operation has to complete before timing out.</param>
		protected override bool OnWaitForChannel(TimeSpan timeout)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			return _channelQueue.WaitForItem(timeout);
		}

		/// <summary>
		/// When implemented in a derived class, provides a point of extensibility when starting to wait for a channel to arrive.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous on begin wait operation. 
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on begin wait operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives the notification of the asynchronous operation on begin wait completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous on begin wait operation.</param>
		protected override IAsyncResult OnBeginWaitForChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			return _channelQueue.BeginWaitForItem(timeout, callback, state);
		}

		/// <summary>
		/// When implemented in a derived class, provides a point of extensibility when ending the waiting for a channel to arrive.
		/// </summary>
		/// <returns>
		/// true if the method completed before the timeout expired; otherwise false.
		/// </returns>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> returned by a call to the <see cref="M:System.ServiceModel.Channels.ChannelListenerBase.OnBeginWaitForChannel(System.TimeSpan,System.AsyncCallback,System.Object)" /> method.</param>
		protected override bool OnEndWaitForChannel(IAsyncResult result)
		{
			return _channelQueue.EndWaitForItem(result);
		}

		/// <summary>
		/// When implemented in a derived class, provides an extensibility point when accepting a channel.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.Channels.IChannel" /> accepted.
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the accept channel operation has to complete before timing out.</param>
		protected override IInputChannel OnAcceptChannel(TimeSpan timeout)
		{
			Tracer.Debug("Accepting channel");
			NmsChannelHelper.ValidateTimeout(timeout);
			if(!IsDisposed)
			{
				EnsureChannelAvailable();
			}

			IInputChannel channel;
			if(_channelQueue.Dequeue(timeout, out channel))
			{
				return channel;
			}
			throw new TimeoutException(String.Format("Accept on listener at address {0} timed out after {1}.", Uri.AbsoluteUri, timeout));
		}

		/// <summary>
		/// When implemented in a derived class, provides an asynchronous extensibility point when beginning to accept a channel.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous accept channel operation. 
		/// </returns>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the accept channel operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives the notification of the asynchronous completion of the accept channel operation.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous accept channel operation.</param>
		protected override IAsyncResult OnBeginAcceptChannel(TimeSpan timeout, AsyncCallback callback, object state)
		{
			NmsChannelHelper.ValidateTimeout(timeout);
			if(!IsDisposed)
			{
				EnsureChannelAvailable();
			}
			return _channelQueue.BeginDequeue(timeout, callback, state);
		}

		/// <summary>
		/// When implemented in a derived class, provides an asynchronous extensibility point when completing the acceptance a channel.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.Channels.IChannel" /> accepted by the listener.
		/// </returns>
		/// <param name="result">The <see cref="T:System.IAsyncResult" /> returned by a call to the <see cref="M:System.ServiceModel.Channels.ChannelListenerBase`1.OnBeginAcceptChannel(System.TimeSpan,System.AsyncCallback,System.Object)" /> method.</param>
		protected override IInputChannel OnEndAcceptChannel(IAsyncResult result)
		{
			IInputChannel channel;
			if(_channelQueue.EndDequeue(result, out channel))
			{
				return channel;
			}
			throw new TimeoutException();
		}

		#endregion

		/// <summary>
		/// Dispatches the callback.
		/// </summary>
		/// <param name="state">The state.</param>
		internal void DispatchCallback(object state)
		{
			Dispatch((Message) state);
		}

		/// <summary>
		/// Matches an incoming message to its waiting listener,
		/// using the FilterTable to dispatch the message to the correct
		/// listener. If no listener is waiting for the message, it is silently
		/// discarded.
		/// </summary>
		internal void Dispatch(Message message)
		{
			if(message == null)
			{
				return;
			}

			try
			{
				NmsInputChannel newChannel;
				bool channelCreated = CreateOrRetrieveChannel(out newChannel);

				Tracer.Debug("Dispatching incoming message");
				newChannel.Dispatch(message);

				if(channelCreated)
				{
					//Hand the channel off to whomever is waiting for AcceptChannel() to complete
					Tracer.Debug("Handing off channel");
					_channelQueue.EnqueueAndDispatch(newChannel);
				}
			}
			catch(Exception e)
			{
				Tracer.ErrorFormat("Error dispatching Message: {0}", e.ToString());
			}
		}

		/// <summary>
		/// Creates or retrieves the channel.
		/// </summary>
		/// <param name="newChannel">The channel.</param>
		private bool CreateOrRetrieveChannel(out NmsInputChannel newChannel)
		{
			bool channelCreated = false;

			if((newChannel = _currentChannel) == null)
			{
				lock(_currentChannelLock)
				{
					if((newChannel = _currentChannel) == null)
					{
						newChannel = CreateNmsChannel(Uri);
						newChannel.Closed += OnChannelClosed;
						_currentChannel = newChannel;
						channelCreated = true;
					}
				}
			}

			return channelCreated;
		}

		/// <summary>
		/// Called when the channel is closed.
		/// </summary>
		/// <param name="sender">The sender.</param>
		/// <param name="args">The <see cref="System.EventArgs"/> instance containing the event data.</param>
		private void OnChannelClosed(object sender, EventArgs args)
		{
			NmsInputChannel channel = (NmsInputChannel) sender;

			lock(_currentChannelLock)
			{
				if(channel == _currentChannel)
				{
					_currentChannel = null;
				}
			}
		}

		/// <summary>
		/// Creates the <see cref="NmsInputChannel" /> that will wait for inbound messages.
		/// </summary>
		/// <param name="uri">The URI for the message queue.</param>
		private NmsInputChannel CreateNmsChannel(Uri uri)
		{
			_connection = OpenConnection(uri);
			_session = OpenSession(_connection);
			_destination = SessionUtil.GetDestination(_session, Destination, DestinationType);
			_consumer = CreateConsumer(_session, _destination);

			EndpointAddress address = new EndpointAddress(uri);
			return new NmsInputChannel(this, address);
		}

		/// <summary>
		/// Opens the connection to the message broker.
		/// </summary>
		/// <param name="uri">The URI.</param>
		/// <returns>An active connection to the ActiveMQ message broker specified by the URI;
		/// exceptions will be caught by the attached ExceptionListener.</returns>
		private IConnection OpenConnection(Uri uri)
		{
			IConnection connection = ConnectionFactoryManager.GetInstance().CreateConnection(uri);
			connection.ExceptionListener += OnExceptionThrown;
			connection.Start();
			Tracer.Debug("Connection open");
			return connection;
		}

		/// <summary>
		/// Opens a session to communicate with a message queue.
		/// </summary>
		/// <param name="connection">The connection to the ActiveMQ message broker.</param>
		/// <returns>A session.</returns>
		/// <exception cref="InvalidOperationException">the <paramref name="connection" /> has not yet
		/// been started.</exception>
		private ISession OpenSession(IConnection connection)
		{
			if(!connection.IsStarted)
			{
				throw new InvalidOperationException("The connection has not yet been opened");
			}

			Tracer.Debug("Opening session...");
			ISession session = connection.CreateSession();
			Tracer.Debug("Session open");
			return session;
		}

		/// <summary>
		/// Creates the consumer of messages received on the <paramref name="session"/>.
		/// </summary>
		/// <param name="session">The session.</param>
		/// <param name="destination">The destination.</param>
		/// <returns>A consumer for any messages received during the session;
		/// messages will be consumed by the attached Listener.</returns>
		private IMessageConsumer CreateConsumer(ISession session, IDestination destination)
		{
			Tracer.Debug("Creating message listener...");
			IMessageConsumer consumer = session.CreateConsumer(destination);
			consumer.Listener += OnReceiveMessage;
			Tracer.Debug("Created message listener");
			return consumer;
		}

		/// <summary>
		/// Event handler that processes a received message.
		/// </summary>
		/// <param name="message">The message.</param>
		private void OnReceiveMessage(IMessage message)
		{
			Tracer.Debug("Decoding message");
			string soapMsg = ((ITextMessage) message).Text;
			byte[] buffer = Encoding.ASCII.GetBytes(soapMsg);
			int dataLength = buffer.Length;
			byte[] data1 = _bufferManager.TakeBuffer(dataLength);
			Array.Copy(buffer, data1, dataLength);

			ArraySegment<byte> data = new ArraySegment<byte>(data1, 0, dataLength);

			byte[] msgContents = new byte[data.Count];
			Array.Copy(data.Array, data.Offset, msgContents, 0, msgContents.Length);
			Message msg = _messageEncoderFactory.Encoder.ReadMessage(data, _bufferManager);

			Tracer.Debug(msg);
			Dispatch(msg);
		}

		/// <summary>
		/// Called when an exception is thrown by the ActiveMQ listener.
		/// </summary>
		/// <remarks>
		/// <see cref="NMSException" />s will be caught and logged; all other exceptions will
		/// be thrown back up to the WCF service.
		/// </remarks>
		/// <param name="exception">The exception that was thrown.</param>
		private void OnExceptionThrown(Exception exception)
		{
			if(exception is NMSException)
			{
				Tracer.ErrorFormat("{0} thrown : {1}\n{2}",
					exception.GetType().Name,
					exception.Message,
					exception.StackTrace);
				return;
			}

			// TODO: can we recover from the exception? Do we convert to WCF exceptions?
			throw exception;
		}

		/// <summary>
		/// Guarantees that a channel is attached to this listener.
		/// </summary>
		private void EnsureChannelAvailable()
		{
			NmsInputChannel newChannel;
			if(CreateOrRetrieveChannel(out newChannel))
			{
				_channelQueue.EnqueueAndDispatch(newChannel);
			}
		}

		#region Private members

		private readonly Uri _uri;
		private IConnection _connection;
		private ISession _session;
		private IDestination _destination;
		private IMessageConsumer _consumer;
		private readonly InputQueue<IInputChannel> _channelQueue;
		private NmsInputChannel _currentChannel;
		private readonly object _currentChannelLock;
		private readonly MessageEncoderFactory _messageEncoderFactory;
		private readonly BufferManager _bufferManager;
		private string _destinationName;
		private DestinationType _destinationType;

		#endregion
	}
}
