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
using System.ServiceModel.Channels;
using System.ServiceModel.Configuration;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Configuration section for NMS.
	/// </summary>
	public class NmsTransportElement : BindingElementExtensionElement
	{
		#region Public properties

		/// <summary>
		/// Gets or sets the name of the message destination.
		/// </summary>
		/// <value>The name of the message destination.</value>
		[ConfigurationProperty(NmsConfigurationStrings.Destination, IsRequired = true)]
		public string Destination
		{
			get { return (string) base[NmsConfigurationStrings.Destination]; }
			set
			{
				base[NmsConfigurationStrings.Destination] = value;
			}
		}

		/// <summary>
		/// Gets or sets the type of the message destination.
		/// </summary>
		/// <value>The type of the message destination (may be either <c>queue</c> or <c>topic</c>, and temporary 
		/// or permanent versions of either).</value>
		[ConfigurationProperty(NmsConfigurationStrings.DestinationType, DefaultValue = NmsConfigurationDefaults.Destination)]
		public DestinationType DestinationType
		{
			get { return (DestinationType) Enum.Parse(typeof(DestinationType), base[NmsConfigurationStrings.DestinationType].ToString(), true); }
			set { base[NmsConfigurationStrings.DestinationType] = value; }
		}

		#endregion

		#region Configuration overrides

		/// <summary>
		/// When overridden in a derived class, gets the <see cref="T:System.Type" /> object that represents the custom binding element. 
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Type" /> object that represents the custom binding type.
		/// </returns>
		public override Type BindingElementType
		{
			get { return typeof(NmsTransportBindingElement); }
		}

		/// <summary>
		/// When overridden in a derived class, returns a custom binding element object. 
		/// </summary>
		/// <returns>
		/// A custom <see cref="T:System.ServiceModel.Channels.BindingElement" /> object.
		/// </returns>
		protected override BindingElement CreateBindingElement()
		{
			NmsTransportBindingElement bindingElement = new NmsTransportBindingElement();
			this.ApplyConfiguration(bindingElement);
			return bindingElement;
		}

		#endregion

		/// <summary>
		/// 
		/// </summary>
		/// <param name="bindingElement"></param>
		public override void ApplyConfiguration(BindingElement bindingElement)
		{
			base.ApplyConfiguration(bindingElement);

			NmsTransportBindingElement nmsBindingElement = (NmsTransportBindingElement) bindingElement;
			nmsBindingElement.Destination = Destination;
			nmsBindingElement.DestinationType = DestinationType;
		}

		/// <summary>
		/// Copies the content of the specified configuration element to this configuration element.
		/// </summary>
		/// <param name="from">The configuration element to be copied.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// 	<paramref name="from"/> is null.</exception>
		/// <exception cref="T:System.Configuration.ConfigurationErrorsException">The configuration file is read-only.</exception>
		public override void CopyFrom(ServiceModelExtensionElement from)
		{
			base.CopyFrom(from);

			NmsTransportElement source = (NmsTransportElement) from;
			Destination = source.Destination;
			DestinationType = source.DestinationType;
		}

		/// <summary>
		/// Initializes this binding configuration section with the content of the specified binding element.
		/// </summary>
		/// <param name="bindingElement">A binding element.</param>
		protected override void InitializeFrom(BindingElement bindingElement)
		{
			base.InitializeFrom(bindingElement);

			NmsTransportBindingElement nmsBindingElement = (NmsTransportBindingElement) bindingElement;
			Destination = nmsBindingElement.Destination;
			DestinationType = nmsBindingElement.DestinationType;
		}

		/// <summary>
		/// Gets the collection of properties.
		/// </summary>
		/// <value></value>
		/// <returns>
		/// The <see cref="T:System.Configuration.ConfigurationPropertyCollection"/> of properties for the element.
		/// </returns>
		protected override ConfigurationPropertyCollection Properties
		{
			get
			{
				ConfigurationPropertyCollection properties = base.Properties;
				properties.Add(new ConfigurationProperty(NmsConfigurationStrings.Destination, typeof(string), null, ConfigurationPropertyOptions.IsRequired));
				properties.Add(new ConfigurationProperty(NmsConfigurationStrings.DestinationType, typeof(DestinationType), NmsConfigurationDefaults.Destination));
				return properties;
			}
		}
	}
}
