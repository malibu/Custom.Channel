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

namespace Apache.NMS.WCF
{
	/// <summary>
	/// Manages connections to the ActiveMQ messaging service.
	/// </summary>
	public class ConnectionFactoryManager
	{
		#region Constructors

		/// <summary>
		/// Initializes a new instance of the <see cref="ConnectionFactoryManager"/> class.
		/// </summary>
		private ConnectionFactoryManager()
		{
		}

		/// <summary>
		/// Gets the instance.
		/// </summary>
		/// <returns></returns>
		public static ConnectionFactoryManager GetInstance()
		{
			if(_instance == null)
			{
				_instance = new ConnectionFactoryManager();
			}
			return _instance;
		}

		#endregion

		#region Public methods

		/// <summary>
		/// Creates the connection.
		/// </summary>
		/// <param name="connection">The connection.</param>
		/// <returns></returns>
		public IConnection CreateConnection(Uri connection)
		{
			lock(this)
			{
				Tracer.InfoFormat("Connecting to {0}{1}{2}", connection.Scheme, Uri.SchemeDelimiter, connection.Authority);
				primaryFactory = new NMSConnectionFactory(connection);
				return primaryFactory.CreateConnection();
			}
		}

		#endregion

		#region Private members

		private static ConnectionFactoryManager _instance;
		private IConnectionFactory primaryFactory;

		#endregion
	}
}