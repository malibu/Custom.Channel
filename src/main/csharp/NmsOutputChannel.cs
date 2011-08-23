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
using System.Xml;
using Apache.NMS.Util;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Client-side implementation of the sessionless one-way channel.
	/// </summary>
	public class NmsOutputChannel : NmsOutputChannelBase, IOutputChannel
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsOutputChannel"/> class.
		/// </summary>
		/// <param name="factory">The factory that created the channel.</param>
		/// <param name="remoteAddress">The remote address of the channel.</param>
		/// <param name="via">The URI that contains the transport address to which messages are sent on the output channel.</param>
		/// <param name="bufferManager">The buffer manager.</param>
		/// <param name="encoderFactory">The encoder factory.</param>
		/// <param name="destination">The name of the ActiveMQ destination.</param>
		/// <param name="destinationType">The type of the ActiveMQ destination (either a queue or a topic, permanent or temporary).</param>
		public NmsOutputChannel(ChannelManagerBase factory, EndpointAddress remoteAddress, Uri via, BufferManager bufferManager, MessageEncoderFactory encoderFactory, string destination, DestinationType destinationType)
			: base(factory, remoteAddress, via, bufferManager, encoderFactory, destination, destinationType)
		{
			_connection = ConnectionFactoryManager.GetInstance().CreateConnection(via);
			_connection.Start();
		}

		#endregion

		#region Implementation of IOutputChannel

		/// <summary>
		/// Transmits a message to the destination of the output channel. 
		/// </summary>
		/// <param name="message">The <see cref="T:System.ServiceModel.Channels.Message" /> being sent on the output channel.</param>
		public void Send(Message message)
		{
			Send(message, DefaultSendTimeout);
		}

		/// <summary>
		/// Sends a message on the current output channel within a specified interval of time.
		/// </summary>
		/// <param name="message">The <see cref="T:System.ServiceModel.Channels.Message" /> being sent on the output channel.</param>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the send operation has to complete before timing out.</param>
		public void Send(Message message, TimeSpan timeout)
		{
			ThrowIfDisposedOrNotOpen();
			RemoteAddress.ApplyTo(message);

			using(NMS.ISession session = _connection.CreateSession())
			{
				IDestination destination = SessionUtil.GetDestination(session, Destination, DestinationType);
				using(IMessageProducer producer = session.CreateProducer(destination))
				{
					producer.DeliveryMode = MsgDeliveryMode.Persistent;

					ITextMessage request = session.CreateTextMessage(TranslateMessage(message));
					producer.Send(request);
					producer.Close();

					Tracer.Info("Sending message:");
					Tracer.Info(request.Text);
				}
			}
		}

		/// <summary>
		/// Translates the message using the appropriate SOAP versioning scheme.
		/// </summary>
		/// <param name="message">The message to be translated.</param>
		private string TranslateMessage(Message message)
		{
			return (this.Encoder.MessageVersion == MessageVersion.Soap11)
				? TranslateMessageAsSoap11(message)
				: TranslateMessageAsSoap12(message);
		}

		/// <summary>
		/// Translates the message using the SOAP 1.1 schema.
		/// </summary>
		/// <param name="message">The message to be translated.</param>
		private static string TranslateMessageAsSoap11(Message message)
		{
			StringBuilder sb = new StringBuilder();
			XmlDictionaryWriter writer = XmlDictionaryWriter.CreateDictionaryWriter(XmlWriter.Create(sb));
			message.WriteStartEnvelope(writer);
			message.WriteBody(writer);
			writer.Flush();

			string raw = sb.ToString();
			//to get past the damn utf 16 header
			raw = raw.Substring(raw.LastIndexOf("?>") + 2).Trim();

			//well there is no WriteEndEnvelope(writer) method:-)
			return raw + "</s:Envelope>";
		}

		/// <summary>
		/// Translates the message using the SOAP 1.2 schema.
		/// </summary>
		/// <param name="message">The message to be translated.</param>
		private static string TranslateMessageAsSoap12(Message message)
		{
			string raw = message.ToString();
			raw = raw.Substring(raw.LastIndexOf("?>") + 1).Trim();
			return raw;
		}

		/// <summary>
		/// Begins an asynchronous operation to transmit a message to the destination of the output channel. 
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous message transmission. 
		/// </returns>
		/// <param name="message">The <see cref="T:System.ServiceModel.Channels.Message" /> being sent on the output channel. </param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate. </param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous send operation.</param>
		public IAsyncResult BeginSend(Message message, AsyncCallback callback, object state)
		{
			return BeginSend(message, DefaultSendTimeout, callback, state);
		}

		/// <summary>
		/// Begins an asynchronous operation to transmit a message to the destination of the output channel within a specified interval of time.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult" /> that references the asynchronous send operation.
		/// </returns>
		/// <param name="message">The <see cref="T:System.ServiceModel.Channels.Message" /> being sent on the output channel.</param>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the send operation has to complete before timing out.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback" /> delegate that receives the notification of the asynchronous operation send completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous send operation.</param>
		public IAsyncResult BeginSend(Message message, TimeSpan timeout, AsyncCallback callback, object state)
		{
			ThrowIfDisposedOrNotOpen();
			return new NmsAsyncResult(this, message, callback, state);
		}

		/// <summary>
		/// Completes an asynchronous operation to transmit a message to the destination of the output channel.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult"/> returned by a call to the <see cref="System.ServiceModel.Channels.IOutputChannel.BeginSend(System.ServiceModel.Channels.Message, System.AsyncCallback, object)"/>  method.</param>
		public void EndSend(IAsyncResult result)
		{
			NmsAsyncResult.End(result);
		}

		#endregion

		#region Implementation of CommunicationObject

		/// <summary>
		/// Gets the property.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <returns></returns>
		public override T GetProperty<T>()
		{
			if(typeof(T) == typeof(IOutputChannel))
			{
				return (T) (object) this;
			}

			T messageEncoderProperty = Encoder.GetProperty<T>();
			if(messageEncoderProperty != null)
			{
				return messageEncoderProperty;
			}

			return base.GetProperty<T>();
		}

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the closing state due to the invocation of a synchronous abort operation.
		/// </summary>
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
			if(_connection != null)
			{
				_connection.Close();
				_connection.Dispose();
			}
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
		/// Inserts processing on a communication object after it transitions into the opening state which must complete within a specified interval of time.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.TimeSpan" /> that specifies how long the on open operation has to complete before timing out.</param>
		/// <exception cref="T:System.ArgumentOutOfRangeException"><paramref name="timeout" /> is less than zero.</exception>
		/// <exception cref="T:System.TimeoutException">The interval of time specified by <paramref name="timeout" /> that was allotted for the operation was exceeded before the operation was completed.</exception>
		protected override void OnOpen(TimeSpan timeout)
		{
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

		/// <summary>
		/// Encodes the message.
		/// </summary>
		/// <param name="message">The message.</param>
		public ArraySegment<byte> EncodeMessage(Message message)
		{
			try
			{
				return Encoder.WriteMessage(message, Int32.MaxValue, BufferManager);
			}
			finally
			{
				// The message is consumed by serialising it, so clean up here.
				message.Close();
			}
		}

		#region Private members

		private readonly IConnection _connection;

		#endregion
	}
}
