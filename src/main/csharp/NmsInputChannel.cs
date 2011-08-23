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
	/// Server-side implementation of the sessionless one-way channel.
	/// </summary>
	public class NmsInputChannel : NmsInputQueueChannelBase<Message>, IInputChannel
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsInputChannel"/> class.
		/// </summary>
		/// <param name="factory">The factory that was used to create the channel.</param>
		/// <param name="localAddress">The local address of the channel.</param>
		internal NmsInputChannel(ChannelListenerBase factory, EndpointAddress localAddress)
			: base(factory, localAddress)
		{
		}

		#endregion

		#region Receive

		/// <summary>
		/// Begins an asynchronous operation to receive a message that has a state object associated with it.
		/// </summary>
		/// <param name="callback">The <see cref="T:System.AsyncCallback"/> delegate that receives the notification of the asynchronous operation completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous operation.</param>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult"/> that references the asynchronous message reception.
		/// </returns>
		public IAsyncResult BeginReceive(AsyncCallback callback, object state)
		{
			return BeginReceive(DefaultReceiveTimeout, callback, state);
		}

		/// <summary>
		/// Begins an asynchronous operation to receive a message that has a specified time out and state object associated with it.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> that specifies the interval of time to wait for a message to become available.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback"/> delegate that receives the notification of the asynchronous operation completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous operation.</param>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult"/> that references the asynchronous receive operation.
		/// </returns>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout"/> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public IAsyncResult BeginReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return BeginDequeue(timeout, callback, state);
		}

		/// <summary>
		/// Completes an asynchronous operation to receive a message.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult"/> returned by a call to one of the <c>System.ServiceModel.Channels.IInputChannel.BeginReceive</c> methods.</param>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.Channels.Message"/> received.
		/// </returns>
		public Message EndReceive(IAsyncResult result)
		{
			return EndDequeue(result);
		}

		/// <summary>
		/// Returns the message received, if one is available. If a message is not available, blocks for a default interval of time.
		/// </summary>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.Channels.Message"/> received.
		/// </returns>
		public Message Receive()
		{
			return Receive(DefaultReceiveTimeout);
		}

		/// <summary>
		/// Returns the message received, if one is available. If a message is not available, blocks for a specified interval of time.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> that specifies how long the receive operation has to complete before timing out and throwing a <see cref="T:System.TimeoutException"/>.</param>
		/// <returns>
		/// The <see cref="T:System.ServiceModel.Channels.Message"/> received.
		/// </returns>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout"/> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public Message Receive(TimeSpan timeout)
		{
			return this.Dequeue(timeout);
		}

		#endregion

		#region TryReceive

		/// <summary>
		/// Begins an asynchronous operation to receive a message that has a specified time out and state object associated with it.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> that specifies the interval of time to wait for a message to become available.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback"/> delegate that receives the notification of the asynchronous operation completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous operation.</param>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult"/> that references the asynchronous receive operation.
		/// </returns>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout"/> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public IAsyncResult BeginTryReceive(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return BeginDequeue(timeout, callback, state);
		}

		/// <summary>
		/// Completes the specified asynchronous operation to receive a message.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult"/> returned by a call to the <see cref="M:System.ServiceModel.Channels.IInputChannel.BeginTryReceive(System.TimeSpan,System.AsyncCallback,System.Object)"/> method.</param>
		/// <param name="message">The <see cref="T:System.ServiceModel.Channels.Message"/> received.</param>
		/// <returns>
		/// true if a message is received before the specified interval of time elapses; otherwise false.
		/// </returns>
		public bool EndTryReceive(IAsyncResult result, out Message message)
		{
			message = null;
			return TryDequeue(result, out message);
		}

		/// <summary>
		/// Tries to receive a message within a specified interval of time.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.IAsyncResult"/> returned by a call to one of the <c>System.ServiceModel.Channels.IInputChannel.BeginReceive</c> methods.</param>
		/// <param name="message">The <see cref="T:System.ServiceModel.Channels.Message"/> received.</param>
		/// <returns>
		/// true if a message is received before the <paramref name="timeout"/> has been exceeded; otherwise false.
		/// </returns>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout"/> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public bool TryReceive(TimeSpan timeout, out Message message)
		{
			message = Receive(timeout);
			return true;
		}

		#endregion

		#region WaitForMessage

		/// <summary>
		/// Begins an asynchronous wait-for-a-message-to-arrive operation that has a specified time out and state object associated with it.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> that specifies the interval of time to wait for a message to become available.</param>
		/// <param name="callback">The <see cref="T:System.AsyncCallback"/> delegate that receives the notification of the asynchronous operation completion.</param>
		/// <param name="state">An object, specified by the application, that contains state information associated with the asynchronous operation.</param>
		/// <returns>
		/// The <see cref="T:System.IAsyncResult"/> that references the asynchronous operation to wait for a message to arrive.
		/// </returns>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout"/> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public IAsyncResult BeginWaitForMessage(TimeSpan timeout, AsyncCallback callback, object state)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Completes the specified asynchronous wait-for-a-message operation.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult"/> that identifies the <see cref="M:System.ServiceModel.Channels.IInputChannel.BeginWaitForMessage(System.TimeSpan,System.AsyncCallback,System.Object)"/> operation to finish, and from which to retrieve an end result.</param>
		/// <returns>
		/// true if a message has arrived before the <paramref name="timeout"/> has been exceeded; otherwise false.
		/// </returns>
		public bool EndWaitForMessage(IAsyncResult result)
		{
			throw new NotImplementedException();
		}

		/// <summary>
		/// Returns a value that indicates whether a message has arrived within a specified interval of time.
		/// </summary>
		/// <param name="timeout">The <see cref="T:System.Timespan"/> specifies the maximum interval of time to wait for a message to arrive before timing out.</param>
		/// <returns>
		/// true if a message has arrived before the <paramref name="timeout"/> has been exceeded; otherwise false.
		/// </returns>
		/// <exception cref="T:System.TimeoutException">The specified <paramref name="timeout"/> is exceeded before the operation is completed.</exception>
		/// <exception cref="T:System.ArgumentOutOfRangeException">The timeout specified is less than zero.</exception>
		public bool WaitForMessage(TimeSpan timeout)
		{
			throw new NotImplementedException();
		}

		#endregion
	}
}
