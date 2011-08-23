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
using System.IO;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.ServiceModel.Description;
using System.Web.Services.Description;
using System.Xml;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Key class to specify the custom transport class and its schema.
	/// </summary>
	/// <remarks>
	/// <para>
	/// Its key role in the WCF is to be the 'factory of factories'.  It determines what shape
	/// the channel will be. In this case by returning channel factory (for the client) that returns an
	/// <see cref="IOutputChannel" /> or <see cref="IOutputSessionChannel" />, and a channel listener 
	/// (for the server) that returns an <see cref="IInputChannel" /> or <see cref="IInputSessionChannel" />,
	/// this class determines that this implementation is a datagram 'shape'.
	/// </para>
	/// <para>
	/// The request/reply channel shape is not supported by WCF.
	/// </para>
	/// </remarks>
	public class NmsTransportBindingElement : TransportBindingElement, IWsdlExportExtension, IPolicyExportExtension
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsTransportBindingElement"/> class.
		/// </summary>
		public NmsTransportBindingElement()
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsTransportBindingElement"/> class.
		/// </summary>
		/// <param name="element">The element.</param>
		protected NmsTransportBindingElement(NmsTransportBindingElement element)
		{
			ManualAddressing = element.ManualAddressing;
			Destination = element.Destination;
			DestinationType = element.DestinationType;
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets or sets the name of the message destination.
		/// </summary>
		/// <value>The name of the message destination.</value>
		public string Destination
		{
			get { return _destination; }
			set { _destination = value; }
		}

		/// <summary>
		/// Gets or sets the type of the message destination.
		/// </summary>
		/// <value>The type of the message destination (may be either <c>queue</c> or <c>topic</c>, and temporary 
		/// or permanent versions of either).</value>
		public DestinationType DestinationType
		{
			get { return _destinationType; }
			set { _destinationType = value; }
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Determines whether this instance can build a channel factory in the specified context.
		/// Only implementations of <see cref="IOutputChannel" /> and <see cref="IOutputSessionChannel" /> are supported.
		/// </summary>
		/// <typeparam name="TChannel">The type of the channel.</typeparam>
		/// <param name="context">The context.</param>
		/// <returns>
		/// 	<c>true</c> if the requested channel factory can be built; otherwise, <c>false</c>.
		/// </returns>
		public override bool CanBuildChannelFactory<TChannel>(BindingContext context)
		{
			return (typeof(TChannel) == typeof(IOutputChannel) || typeof(TChannel) == typeof(IOutputSessionChannel));
		}

		/// <summary>
		/// Determines whether this instance can build a channel listener in the specified context.
		/// Only implementations of <see cref="IInputChannel" /> and <see cref="IInputSessionChannel" /> are supported.
		/// </summary>
		/// <typeparam name="TChannel">The type of the channel.</typeparam>
		/// <param name="context">The context.</param>
		/// <returns>
		/// 	<c>true</c> if the requested channel listener can be built; otherwise, <c>false</c>.
		/// </returns>
		/// <exception cref="ArgumentException">the requested channel does not implement <see cref="IReplyChannel" />.</exception>
		public override bool CanBuildChannelListener<TChannel>(BindingContext context)
		{
			return (typeof(TChannel) == typeof(IInputChannel) || typeof(TChannel) == typeof(IInputSessionChannel));
		}

		/// <summary>
		/// Builds the channel factory.
		/// </summary>
		/// <typeparam name="TChannel">The type of the channel.</typeparam>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		/// <exception cref="ArgumentException">the requested channel does not implement <see cref="IReplyChannel" />.</exception>
		public override IChannelFactory<TChannel> BuildChannelFactory<TChannel>(BindingContext context)
		{
			if(context == null)
			{
				throw new ArgumentNullException("context");
			}
			if(!CanBuildChannelFactory<TChannel>(context))
			{
				throw new ArgumentException(String.Format("Unsupported channel type: {0}.", typeof(TChannel).Name));
			}
			return (IChannelFactory<TChannel>) new NmsChannelFactory<TChannel>(this, context);
		}

		/// <summary>
		/// Builds the channel listener.
		/// </summary>
		/// <typeparam name="TChannel">The type of the channel.</typeparam>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public override IChannelListener<TChannel> BuildChannelListener<TChannel>(BindingContext context)
		{
			if(context == null)
			{
				throw new ArgumentNullException("context");
			}

			if(!CanBuildChannelListener<TChannel>(context))
			{
				throw new ArgumentException(String.Format("Unsupported channel type: {0}.", typeof(TChannel).Name));
			}

			if (typeof(TChannel) == typeof(IInputSessionChannel))
			{
				return (IChannelListener<TChannel>)new NmsInputSessionChannelListener(this, context);
			}
			return (IChannelListener<TChannel>) new NmsInputChannelListener(this, context);
		}

		/// <summary>
		/// Gets the URI scheme for the transport.
		/// </summary>
		/// <value></value>
		/// <returns>Returns the URI scheme for the transport, which varies depending on what derived class implements this method.</returns>
		public override string Scheme
		{
			get { return NmsConstants.TransportScheme; }
		}

		/// <summary>
		/// When overridden in a derived class, returns a copy of the binding element object.
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.ServiceModel.Channels.BindingElement"></see> object that is a deep clone of the original.
		/// </returns>
		public override BindingElement Clone()
		{
			return new NmsTransportBindingElement(this);
		}

		/// <summary>
		/// Gets the property.
		/// </summary>
		/// <typeparam name="T"></typeparam>
		/// <param name="context">The context.</param>
		/// <returns></returns>
		public override T GetProperty<T>(BindingContext context)
		{
			if(context == null)
			{
				throw new ArgumentNullException("context");
			}

			return context.GetInnerProperty<T>();
		}

		#endregion

		#region Implementation of IWsdlExportExtension

		/// <summary>
		/// Writes custom Web Services Description Language (WSDL) elements into the generated WSDL for a contract.
		/// </summary>
		/// <param name="exporter">The <see cref="T:System.ServiceModel.Description.WsdlExporter" /> that exports the contract information.</param>
		/// <param name="context">Provides mappings from exported WSDL elements to the contract description.</param>
		public void ExportContract(WsdlExporter exporter, WsdlContractConversionContext context)
		{
		}

		/// <summary>
		/// Writes custom Web Services Description Language (WSDL) elements into the generated WSDL for an endpoint.
		/// </summary>
		/// <param name="exporter">The <see cref="T:System.ServiceModel.Description.WsdlExporter" /> that exports the endpoint information.</param>
		/// <param name="context">Provides mappings from exported WSDL elements to the endpoint description.</param>
		public void ExportEndpoint(WsdlExporter exporter, WsdlEndpointConversionContext context)
		{
			BindingElementCollection bindingElements = context.Endpoint.Binding.CreateBindingElements();
			MessageEncodingBindingElement encodingBindingElement = bindingElements.Find<MessageEncodingBindingElement>() ?? new TextMessageEncodingBindingElement();

			if(context.WsdlPort != null)
			{
				AddAddressToWsdlPort(context.WsdlPort, context.Endpoint.Address, encodingBindingElement.MessageVersion.Addressing);
			}
		}

		#endregion

		#region Implementation of IPolicyExportExtension

		/// <summary>
		/// Implement to include for exporting a custom policy assertion about bindings.
		/// </summary>
		/// <param name="exporter">The <see cref="T:System.ServiceModel.Description.MetadataExporter" /> that you can use to modify the exporting process.</param>
		/// <param name="context">The <see cref="T:System.ServiceModel.Description.PolicyConversionContext" /> that you can use to insert your custom policy assertion.</param>
		public void ExportPolicy(MetadataExporter exporter, PolicyConversionContext context)
		{
			if(exporter == null)
			{
				throw new ArgumentNullException("exporter");
			}

			if(context == null)
			{
				throw new ArgumentNullException("context");
			}

			bool createdNew = false;
			MessageEncodingBindingElement encodingBindingElement = context.BindingElements.Find<MessageEncodingBindingElement>();
			if(encodingBindingElement == null)
			{
				createdNew = true;
				encodingBindingElement = new TextMessageEncodingBindingElement();
			}

			if(createdNew && encodingBindingElement is IPolicyExportExtension)
			{
				((IPolicyExportExtension) encodingBindingElement).ExportPolicy(exporter, context);
			}

			AddWSAddressingAssertion(context, encodingBindingElement.MessageVersion.Addressing);
		}

		/// <summary>
		/// Adds the address to WSDL port.
		/// </summary>
		/// <param name="wsdlPort">The WSDL port.</param>
		/// <param name="endpointAddress">The endpoint address.</param>
		/// <param name="addressing">The addressing.</param>
		private static void AddAddressToWsdlPort(Port wsdlPort, EndpointAddress endpointAddress, AddressingVersion addressing)
		{
			if(addressing == AddressingVersion.None)
			{
				return;
			}

			MemoryStream memoryStream = new MemoryStream();
			XmlWriter xmlWriter = XmlWriter.Create(memoryStream);
			xmlWriter.WriteStartElement("temp");

			if(addressing == AddressingVersion.WSAddressing10)
			{
				xmlWriter.WriteAttributeString("xmlns", "wsa10", null, AddressingVersions.WSAddressing10NameSpace);
			}
			else if(addressing == AddressingVersion.WSAddressingAugust2004)
			{
				xmlWriter.WriteAttributeString("xmlns", "wsa", null, AddressingVersions.WSAddressingAugust2004NameSpace);
			}
			else
			{
				throw new InvalidOperationException("This addressing version is not supported:\n" + addressing);
			}

			endpointAddress.WriteTo(addressing, xmlWriter);
			xmlWriter.WriteEndElement();

			xmlWriter.Flush();
			memoryStream.Seek(0, SeekOrigin.Begin);

			XmlReader xmlReader = XmlReader.Create(memoryStream);
			xmlReader.MoveToContent();

			XmlElement endpointReference = (XmlElement) XmlDoc.ReadNode(xmlReader).ChildNodes[0];

			wsdlPort.Extensions.Add(endpointReference);
		}

		/// <summary>
		/// Adds the WS addressing assertion.
		/// </summary>
		/// <param name="context">The context.</param>
		/// <param name="addressing">The addressing.</param>
		static void AddWSAddressingAssertion(PolicyConversionContext context, AddressingVersion addressing)
		{
			XmlElement addressingAssertion;

			if(addressing == AddressingVersion.WSAddressing10)
			{
				addressingAssertion = XmlDoc.CreateElement("wsaw", "UsingAddressing", "http://www.w3.org/2006/05/addressing/wsdl");
			}
			else if(addressing == AddressingVersion.WSAddressingAugust2004)
			{
				addressingAssertion = XmlDoc.CreateElement("wsap", "UsingAddressing", AddressingVersions.WSAddressingAugust2004NameSpace + "/policy");
			}
			else if(addressing == AddressingVersion.None)
			{
				// do nothing
				addressingAssertion = null;
			}
			else
			{
				throw new InvalidOperationException("This addressing version is not supported:\n" + addressing);
			}

			if(addressingAssertion != null)
			{
				context.GetBindingAssertions().Add(addressingAssertion);
			}
		}

		/// <summary>
		/// Gets the XML doc.
		/// </summary>
		static XmlDocument XmlDoc
		{
			get
			{
				if(_xmlDocument == null)
				{
					NameTable nameTable = new NameTable();
					nameTable.Add("Policy");
					nameTable.Add("All");
					nameTable.Add("ExactlyOne");
					nameTable.Add("PolicyURIs");
					nameTable.Add("Id");
					nameTable.Add("UsingAddressing");
					nameTable.Add("UsingAddressing");
					_xmlDocument = new XmlDocument(nameTable);
				}
				return _xmlDocument;
			}
		}

		private static XmlDocument _xmlDocument;

		#endregion

		#region Private members

		private string _destination;
		private DestinationType _destinationType;

		#endregion
	}
}
