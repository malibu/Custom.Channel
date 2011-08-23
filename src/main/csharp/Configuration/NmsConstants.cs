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

using System.ServiceModel.Channels;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Constants used in the NMS WCF provider.
	/// </summary>
	internal class NmsConstants
	{
		private static readonly MessageEncoderFactory factory;

		/// <summary>
		/// Initializes the <see cref="NmsConstants"/> class.
		/// </summary>
		static NmsConstants()
		{
			factory = new TextMessageEncodingBindingElement().CreateMessageEncoderFactory();
		}

		/// <summary>
		/// Gets the default message encoder factory.
		/// </summary>
		/// <value>The default message encoder factory.</value>
		public static MessageEncoderFactory DefaultMessageEncoderFactory
		{
			get { return factory; }
		}

		/// <summary>
		/// The transport scheme to use with NMS.
		/// </summary>
		public const string TransportScheme = "tcp";

		/// <summary>
		/// The name of the NMS binding configuration section.
		/// </summary>
		public const string NmsBindingSectionName = "system.serviceModel/bindings/nmsBinding";

		/// <summary>
		/// The name of the NMS transport configuration section.
		/// </summary>
		public const string NmsTransportSectionName = "nmsTransport";
	}
}
