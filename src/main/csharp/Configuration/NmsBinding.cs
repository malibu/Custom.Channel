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
using System.Configuration;
using System.Globalization;
using System.ServiceModel.Channels;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// The standard binding for NMS.
	/// </summary>
	public class NmsBinding : Binding
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsBinding"/> class.
		/// </summary>
		/// <remarks>
		/// The default transport is set to <see cref="NmsTransportBindingElement" />, while the
		/// default message encoding is set to <see cref="TextMessageEncodingBindingElement" />.
		/// </remarks>
		public NmsBinding()
		{
			_messageElement = new TextMessageEncodingBindingElement();
			_transportElement = new NmsTransportBindingElement();
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsBinding"/> class.
		/// </summary>
		/// <param name="configurationName">Name of the configuration.</param>
		/// <remarks>
		/// The default transport is set to <see cref="NmsTransportBindingElement" />, while the
		/// default message encoding is set to <see cref="TextMessageEncodingBindingElement" />.
		/// </remarks>
		public NmsBinding(string configurationName)
			: this()
		{
			ApplyConfiguration(configurationName);
		}

		#endregion

		#region Public properties

		/// <summary>
		/// This property encapsulates the message encoding used for this custom
		/// channel implementation.
		/// </summary>
		/// <value>The message element.</value>
		public MessageEncodingBindingElement MessageElement
		{
			get { return _messageElement; }
			set { _messageElement = value; }
		}

		/// <summary>
		/// Gets or sets the transport element.
		/// </summary>
		/// <value>The transport element.</value>
		public NmsTransportBindingElement TransportElement
		{
			get { return _transportElement; }
			set { _transportElement = value; }
		}

		/// <summary>
		/// Gets or sets the name of the destination.
		/// </summary>
		/// <value>The name of the destination.</value>
		public string Destination
		{
			get { return _destination; }
			set { _destination = value; }
		}

		/// <summary>
		/// Gets or sets the name of the message topic (used for publish-subscribe operations).
		/// </summary>
		/// <value>The name of the topic.</value>
		public DestinationType DestinationType
		{
			get { return _destinationType; }
			set { _destinationType = value; }
		}

		#endregion

		#region Public methods

		/// <summary>
		/// When overridden in a derived class, creates a collection that contains the binding elements that are part of the current binding.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Collections.Generic.ICollection`1"></see> object of type <see cref="T:System.ServiceModel.Channels.BindingElement"></see> that contains the binding elements from the current binding object in the correct order.
		/// </returns>
		public override BindingElementCollection CreateBindingElements()
		{
			_transportElement.Destination = Destination;
			_transportElement.DestinationType = DestinationType;

			BindingElementCollection elements = new BindingElementCollection();
			elements.Add(_messageElement);
			elements.Add(_transportElement);
			return elements.Clone();
		}

		/// <summary>
		/// When implemented in a derived class, sets the URI scheme that specifies the transport used by the channel and listener factories that are built by the bindings.
		/// </summary>
		/// <value></value>
		/// <returns>The URI scheme that is used by the channels or listeners that are created by the factories built by the current binding.</returns>
		public override string Scheme
		{
			get { return TransportElement.Scheme; }
		}

		#endregion

		#region Private methods

		/// <summary>
		/// Applies the configuration.
		/// </summary>
		/// <param name="configurationName">Name of the configuration.</param>
		private void ApplyConfiguration(string configurationName)
		{
			NmsBindingCollection section = (NmsBindingCollection) ConfigurationManager.GetSection(NmsConstants.NmsBindingSectionName);
			NmsBindingElement element = section.Bindings[configurationName];
			if(element == null)
			{
				throw new ConfigurationErrorsException(String.Format(CultureInfo.CurrentCulture, "There is no binding named {0} at {1}.", configurationName, section.BindingName));
			}
			element.ApplyConfiguration(this);
		}

		#endregion

		#region Private members

		private MessageEncodingBindingElement _messageElement;
		private NmsTransportBindingElement _transportElement;
		private string _destination;
		private DestinationType _destinationType;

		#endregion
	}
}