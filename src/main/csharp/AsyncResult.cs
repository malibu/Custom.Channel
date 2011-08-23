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
using System.Diagnostics;
using System.Threading;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// A generic base class for IAsyncResult implementations
	/// that wraps a ManualResetEvent.
	/// </summary>
	internal abstract class AsyncResult : IAsyncResult
	{
		private readonly AsyncCallback _callback;
		private readonly object _state;
		private readonly object _thisLock;
		private bool _completedSynchronously;
		private bool _endCalled;
		private Exception _exception;
		private bool _isCompleted;
		private ManualResetEvent _manualResetEvent;

		/// <summary>
		/// Initializes a new instance of the <see cref="AsyncResult"/> class.
		/// </summary>
		/// <param name="callback">The callback.</param>
		/// <param name="state">The state.</param>
		protected AsyncResult(AsyncCallback callback, object state)
		{
			_callback = callback;
			_state = state;
			_thisLock = new object();
		}

		/// <summary>
		/// Gets a user-defined object that qualifies or contains information about an asynchronous operation.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// A user-defined object that qualifies or contains information about an asynchronous operation.
		/// </returns>
		public object AsyncState
		{
			get { return _state; }
		}

		/// <summary>
		/// Gets a <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// A <see cref="T:System.Threading.WaitHandle"/> that is used to wait for an asynchronous operation to complete.
		/// </returns>
		public WaitHandle AsyncWaitHandle
		{
			get
			{
				if(_manualResetEvent != null)
				{
					return _manualResetEvent;
				}

				lock(ThisLock)
				{
					if(_manualResetEvent == null)
					{
						_manualResetEvent = new ManualResetEvent(_isCompleted);
					}
				}

				return _manualResetEvent;
			}
		}

		/// <summary>
		/// Gets a value that indicates whether the asynchronous operation completed synchronously.
		/// </summary>
		/// <value></value>
		/// <returns>true if the asynchronous operation completed synchronously; otherwise, false.
		/// </returns>
		public bool CompletedSynchronously
		{
			get { return _completedSynchronously; }
		}

		/// <summary>
		/// Gets a value that indicates whether the asynchronous operation has completed.
		/// </summary>
		/// <value></value>
		/// <returns>true if the operation is complete; otherwise, false.
		/// </returns>
		public bool IsCompleted
		{
			get { return _isCompleted; }
		}

		/// <summary>
		/// Gets an object lock.
		/// </summary>
		/// <value>The object lock.</value>
		private object ThisLock
		{
			get { return _thisLock; }
		}

		// Call this version of complete when your asynchronous operation is complete.  This will update the state
		// of the operation and notify the callback.
		/// <summary>
		/// Completes the specified completed synchronously.
		/// </summary>
		/// <param name="completedSynchronously">if set to <see langword="true"/> [completed synchronously].</param>
		protected void Complete(bool completedSynchronously)
		{
			if(_isCompleted)
			{
				throw new InvalidOperationException("Cannot call Complete twice");
			}

			_completedSynchronously = completedSynchronously;

			if(completedSynchronously)
			{
				// If we completedSynchronously, then there's no chance that the manualResetEvent was created so
				// we don't need to worry about a race
				Debug.Assert(_manualResetEvent == null, "No ManualResetEvent should be created for a synchronous AsyncResult.");
				_isCompleted = true;
			}
			else
			{
				lock(ThisLock)
				{
					_isCompleted = true;
					if(_manualResetEvent != null)
					{
						_manualResetEvent.Set();
					}
				}
			}

			// If the callback throws, there is an error in the callback implementation
			if(_callback != null)
			{
				_callback(this);
			}
		}

		/// <summary>
		/// Call this version of complete if you raise an exception during processing.  In addition to notifying
		/// the callback, it will capture the exception and store it to be thrown during AsyncResult.End.
		/// </summary>
		/// <param name="completedSynchronously">if set to <see langword="true"/> [completed synchronously].</param>
		/// <param name="exception">The exception.</param>
		protected void Complete(bool completedSynchronously, Exception exception)
		{
			_exception = exception;
			Complete(completedSynchronously);
		}

		/// <summary>
		/// End should be called when the End function for the asynchronous operation is complete. It
		/// ensures the asynchronous operation is complete, and does some common validation.
		/// </summary>
		/// <typeparam name="TAsyncResult">The type of the async result.</typeparam>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		protected static TAsyncResult End<TAsyncResult>(IAsyncResult result) where TAsyncResult : AsyncResult
		{
			if(result == null)
			{
				throw new ArgumentNullException("result");
			}

			TAsyncResult asyncResult = result as TAsyncResult;

			if(asyncResult == null)
			{
				throw new ArgumentException("Invalid async messageBody.", "result");
			}

			if(asyncResult._endCalled)
			{
				throw new InvalidOperationException("Async object already ended.");
			}

			asyncResult._endCalled = true;

			if(!asyncResult._isCompleted)
			{
				asyncResult.AsyncWaitHandle.WaitOne();
			}

			if(asyncResult._manualResetEvent != null)
			{
				asyncResult._manualResetEvent.Close();
			}

			if(asyncResult._exception != null)
			{
				throw asyncResult._exception;
			}

			return asyncResult;
		}
	}

	/// <summary>
	/// An AsyncResult that completes as soon as it is instantiated.
	/// </summary>
	internal class CompletedAsyncResult : AsyncResult
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="CompletedAsyncResult"/> class.
		/// </summary>
		/// <param name="callback">The callback.</param>
		/// <param name="state">The state.</param>
		public CompletedAsyncResult(AsyncCallback callback, object state)
			: base(callback, state)
		{
			Complete(true);
		}

		/// <summary>
		/// Ends the specified result.
		/// </summary>
		/// <param name="result">The result.</param>
		public static void End(IAsyncResult result)
		{
			End<CompletedAsyncResult>(result);
		}
	}

	//A strongly typed AsyncResult
	internal abstract class TypedAsyncResult<T> : AsyncResult
	{
		private T _data;

		/// <summary>
		/// Initializes a new instance of the <see cref="TypedAsyncResult&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="callback">The callback.</param>
		/// <param name="state">The state.</param>
		protected TypedAsyncResult(AsyncCallback callback, object state)
			: base(callback, state)
		{
		}

		/// <summary>
		/// Gets the data.
		/// </summary>
		/// <value>The data.</value>
		public T Data
		{
			get { return _data; }
		}

		/// <summary>
		/// Completes the specified data.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="completedSynchronously">if set to <see langword="true"/> [completed synchronously].</param>
		protected void Complete(T data, bool completedSynchronously)
		{
			_data = data;
			Complete(completedSynchronously);
		}

		/// <summary>
		/// Ends the specified result.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		public static T End(IAsyncResult result)
		{
			TypedAsyncResult<T> typedResult = End<TypedAsyncResult<T>>(result);
			return typedResult.Data;
		}
	}

	//A strongly typed AsyncResult that completes as soon as it is instantiated.
	internal class TypedCompletedAsyncResult<T> : TypedAsyncResult<T>
	{
		/// <summary>
		/// Initializes a new instance of the <see cref="TypedCompletedAsyncResult&lt;T&gt;"/> class.
		/// </summary>
		/// <param name="data">The data.</param>
		/// <param name="callback">The callback.</param>
		/// <param name="state">The state.</param>
		public TypedCompletedAsyncResult(T data, AsyncCallback callback, object state)
			: base(callback, state)
		{
			Complete(data, true);
		}

		/// <summary>
		/// Finishes the async request.
		/// </summary>
		/// <param name="result">The result.</param>
		/// <returns></returns>
		public new static T End(IAsyncResult result)
		{
			TypedCompletedAsyncResult<T> completedResult = result as TypedCompletedAsyncResult<T>;
			if(completedResult == null)
			{
				throw new ArgumentException("Invalid async messageBody.", "messageBody");
			}

			return TypedAsyncResult<T>.End(completedResult);
		}
	}
}