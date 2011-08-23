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
using System.ServiceModel.Channels;

namespace Apache.NMS.WCF
{
	internal class NmsAsyncResult : AsyncResult
	{
		private ArraySegment<byte> _messageBuffer;
		private readonly NmsOutputChannel _channel;

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsAsyncResult"/> class.
		/// </summary>
		/// <param name="channel">The channel.</param>
		/// <param name="message">The message.</param>
		/// <param name="callback">The callback.</param>
		/// <param name="state">The state.</param>
		public NmsAsyncResult(NmsOutputChannel channel, Message message, AsyncCallback callback, object state)
			: base(callback, state)
		{
			_channel = channel;
			_messageBuffer = _channel.EncodeMessage(message);

			try
			{
				IAsyncResult result = _channel.BeginSend(message, new AsyncCallback(OnSend), this);
				if(!result.CompletedSynchronously)
				{
					return;
				}

				CompleteSend(result, true);
			}
			catch(Exception)
			{
				CleanupBuffer();
				throw;
			}
		}

		private void CleanupBuffer()
		{
			if(_messageBuffer.Array != null)
			{
				_channel.BufferManager.ReturnBuffer(_messageBuffer.Array);
				_messageBuffer = new ArraySegment<byte>();
			}
		}

		internal void CompleteSend(IAsyncResult result, bool synchronous)
		{
			_channel.EndSend(result);
			CleanupBuffer();

			Complete(synchronous);
		}


		internal void OnSend(IAsyncResult result)
		{
			if(result.CompletedSynchronously)
			{
				return;
			}

			try
			{
				CompleteSend(result, false);
			}
			catch(Exception e)
			{
				Complete(false, e);
			}
		}

		public static void End(IAsyncResult result)
		{
			End<NmsAsyncResult>(result);
		}
	}
}