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
	/// Base class for NMS input channels.
	/// </summary>
	/// <typeparam name="T"></typeparam>
	public abstract class NmsInputQueueChannelBase<T> : ChannelBase where T : class
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsInputQueueChannelBase&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="factory">The factory that was used to create the channel.</param>
		/// <param name="localAddress">The local address of the channel.</param>
		public NmsInputQueueChannelBase(ChannelListenerBase factory, EndpointAddress localAddress)
			: base(factory)
		{
			_localAddress = localAddress;
			_messageQueue = new InputQueue<T>();
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets the local address.
		/// </summary>
		/// <value>The local address.</value>
		public EndpointAddress LocalAddress
		{
			get { return _localAddress; }
		}

		#endregion

		#region Messaging

		/// <summary>
		/// Gets the pending message count.
		/// </summary>
		/// <value>The pending message count.</value>
		public int PendingMessageCount
		{
			get
			{
				return _messageQueue.PendingCount;
			}
		}

		/// <summary>
		/// Dispatches the specified request.
		/// </summary>
		/// <param name="request">The request.</param>
		public void Dispatch(T request)
		{
			ThrowIfDisposedOrNotOpen();
			_messageQueue.EnqueueAndDispatch(request);
		}

		/// <summary>
		/// Begins the dequeue operation.
		/// </summary>
		/// <param name="timeout">The timeout.</param>
		/// <param name="callback">The callback.</param>
		/// <param name="state">The state.</param>
		public IAsyncResult BeginDequeue(TimeSpan timeout, AsyncCallback callback, object state)
		{
			return (State == CommunicationState.Opened)
				? _messageQueue.BeginDequeue(timeout, callback, state)
				: new CompletedAsyncResult(callback, state);
		}

		/// <summary>
		/// Ends the dequeue operation.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		public T EndDequeue(IAsyncResult result)
		{
			ThrowIfDisposedOrNotOpen();
			return _messageQueue.EndDequeue(result);
		}

		/// <summary>
		/// Dequeues the next message.
		/// </summary>
		/// <param name="timeout">The timeout.</param>
		public T Dequeue(TimeSpan timeout)
		{
			ThrowIfDisposedOrNotOpen();
			return _messageQueue.Dequeue(timeout);
		}

		/// <summary>
		/// Tries to dequeue the next message.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <param name="message">The message.</param>
		/// <returns></returns>
		public bool TryDequeue(IAsyncResult result, out T message)
		{
			message = null;
			TypedAsyncResult<T> completedResult = result as TypedAsyncResult<T>;
			if(completedResult != null)
			{
				message = TypedAsyncResult<T>.End(result);
			}
			else if(result.CompletedSynchronously == false)
			{
				InputQueue<T>.AsyncQueueReader completedResult2 = result as InputQueue<T>.AsyncQueueReader;
				InputQueue<T>.AsyncQueueReader.End(result, out message);
			}
			return result.IsCompleted;
		}

		#endregion

		#region Abort

		/// <summary>
		/// Inserts processing on a communication object after it transitions to the closing state due to the invocation of a synchronous abort operation.
		/// </summary>
		protected override void OnAbort()
		{
			_messageQueue.Close();
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
			_messageQueue.Open();
		}

			protected override void OnEndOpen(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
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
			_messageQueue.Close();
		}

		/// <summary>
		/// Completes an asynchronous operation on the close of a communication object.
		/// </summary>
		/// <param name="result">The <see cref="T:System.IAsyncResult"/> that is returned by a call to the <see cref="M:System.ServiceModel.Channels.CommunicationObject.OnEndClose(System.IAsyncResult)"/> method.</param>
		/// <exception cref="T:System.TimeoutException">The interval of time specified by <paramref name="timeout"/> that was allotted for the operation was exceeded before the operation was completed.</exception>
		protected override void OnEndClose(IAsyncResult result)
		{
			CompletedAsyncResult.End(result);
		}

		#endregion

		#region GetProperty

		/// <summary>
		/// Gets the property.
		/// </summary>
		/// <typeparam name="P"></typeparam>
		public override P GetProperty<P>()
		{
			if(typeof(P) == typeof(FaultConverter))
			{
				return FaultConverter.GetDefaultFaultConverter(MessageVersion.Soap12WSAddressing10) as P;
			}
			return base.GetProperty<P>();
		}

		#endregion

		#region Private members

		private EndpointAddress _localAddress;
		private InputQueue<T> _messageQueue;

		#endregion
	}
}
