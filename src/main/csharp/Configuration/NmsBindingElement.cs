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
using System.ServiceModel.Configuration;

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Configuration element for the <see cref="NmsBinding" />, allowing the overriding of default values.
	/// </summary>
	public class NmsBindingElement : StandardBindingElement
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsBindingElement"/> class.
		/// </summary>
		public NmsBindingElement()
			: this(null)
		{
		}

		/// <summary>
		/// Initializes a new instance of the <see cref="NmsBindingElement"/> class.
		/// </summary>
		/// <param name="configurationName">Name of the configuration.</param>
		public NmsBindingElement(string configurationName)
			: base(configurationName)
		{
		}

		#endregion

		#region Public properties

		/// <summary>
		/// Gets or sets the name of the message destination.
		/// </summary>
		/// <value>The name of the message destination.</value>
		[ConfigurationProperty(NmsConfigurationStrings.Destination, IsRequired = true)]
		public string Destination
		{
			get { return (string) base[NmsConfigurationStrings.Destination]; }
			set { base[NmsConfigurationStrings.Destination] = value; }
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

		#region Protected methods

		/// <summary>
		/// Gets a <see cref="T:System.Configuration.ConfigurationPropertyCollection"/> instance that contains a collection of <see cref="T:System.Configuration.ConfigurationProperty"/> objects that can be attributes or <see cref="T:System.Configuration.ConfigurationElement"/> objects of this configuration element.
		/// </summary>
		/// <value></value>
		/// <returns>A <see cref="T:System.Configuration.ConfigurationPropertyCollection"/> instance that contains a collection of <see cref="T:System.Configuration.ConfigurationProperty"/> objects that can be attributes or <see cref="T:System.Configuration.ConfigurationElement"/> objects of this configuration element.</returns>
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

		/// <summary>
		/// Initializes this binding configuration element with the content of the specified binding.
		/// </summary>
		/// <param name="binding">A binding.</param>
		/// <exception cref="T:System.ArgumentNullException">
		/// 	<paramref name="binding"/> is null.</exception>
		/// <exception cref="T:System.ArgumentException">The type of this binding element is different from the type specified by <paramref name="binding"/>.</exception>
		protected override void InitializeFrom(Binding binding)
		{
			base.InitializeFrom(binding);
			NmsBinding nmsBinding = (NmsBinding) binding;
			Destination = nmsBinding.Destination;
			DestinationType = nmsBinding.DestinationType;
		}

		/// <summary>
		/// Called when the content of a specified binding element is applied to this binding configuration element.
		/// </summary>
		/// <param name="binding">A binding.</param>
		protected override void OnApplyConfiguration(Binding binding)
		{
			if(binding == null)
			{
				throw new ArgumentNullException("binding");
			}

			if(binding.GetType() != typeof(NmsBinding))
			{
				throw new ArgumentException(String.Format(CultureInfo.CurrentCulture, "Invalid type for binding. Expected tpye: {0}. Type passed in: {1}.", typeof(NmsBinding).AssemblyQualifiedName, binding.GetType().AssemblyQualifiedName));
			}

			NmsBinding nmsBinding = (NmsBinding) binding;
			nmsBinding.Destination = Destination;
			nmsBinding.DestinationType = DestinationType;
		}

		/// <summary>
		/// When overridden in a derived class, gets the <see cref="T:System.Type" /> object that represents the custom binding element. 
		/// </summary>
		/// <returns>
		/// A <see cref="T:System.Type" /> object that represents the custom binding type.
		/// </returns>
		protected override Type BindingElementType
		{
			get { return typeof(NmsBinding); }
		}

		#endregion
	}
}
