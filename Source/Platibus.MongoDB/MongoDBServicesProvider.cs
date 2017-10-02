﻿// The MIT License (MIT)
// 
// Copyright (c) 2017 Jesse Sweetland
// 
// Permission is hereby granted, free of charge, to any person obtaining a copy
// of this software and associated documentation files (the "Software"), to deal
// in the Software without restriction, including without limitation the rights
// to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
// copies of the Software, and to permit persons to whom the Software is
// furnished to do so, subject to the following conditions:
//
// The above copyright notice and this permission notice shall be included in
// all copies or substantial portions of the Software.
// 
// THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
// IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
// FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
// AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
// LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
// OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
// THE SOFTWARE.

using Platibus.Config;
using Platibus.Config.Extensibility;
using Platibus.Journaling;
using Platibus.Multicast;
using System.Configuration;
using System.Threading.Tasks;

namespace Platibus.MongoDB
{
    /// <summary>
    /// A provider for MongoDB-based message queueing and subscription tracking services
    /// </summary>
    [Provider("MongoDB")]
    public class MongoDBServicesProvider : IMessageQueueingServiceProvider, ISubscriptionTrackingServiceProvider, IMessageJournalProvider
    {
        /// <inheritdoc />
        public async Task<IMessageQueueingService> CreateMessageQueueingService(QueueingElement configuration)
        {
            var connectionName = configuration.GetString("connectionName");
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'connectionName' is required for MongoDB message queueing service");
            }

            QueueCollectionNameFactory collectionNameFactory = null;
            var databaseName = configuration.GetString("database");
            var collectionName = configuration.GetString("collection");
            var collectionPerQueue = configuration.GetBool("collectionPerQueue");
            if (!string.IsNullOrWhiteSpace(collectionName))
            {
                collectionNameFactory = _ => collectionName;
            }
            else if (collectionPerQueue)
            {
                var collectionPrefix = (configuration.GetString("collectionPrefix") ?? "").Trim();
                collectionNameFactory = queueName => (collectionPrefix + queueName).Trim();
            }
          
            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName];
            if (connectionStringSettings == null)
            {
                throw new ConfigurationErrorsException("Connection string settings \"" + connectionName + "\" not found");
            }

            var securityTokenServiceFactory = new SecurityTokenServiceFactory();
            var securitTokenConfig = configuration.SecurityTokens;
            var securityTokenService = await securityTokenServiceFactory.InitSecurityTokenService(securitTokenConfig);

            var messageQueueingService = new MongoDBMessageQueueingService(connectionStringSettings, 
                securityTokenService, databaseName, collectionNameFactory);

            return messageQueueingService;
        }

        /// <inheritdoc />
        public Task<ISubscriptionTrackingService> CreateSubscriptionTrackingService(SubscriptionTrackingElement configuration)
        {
            var connectionName = configuration.GetString("connectionName");
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'connectionName' is required for MongoDB subscription tracking service");
            }

            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName];
            if (connectionStringSettings == null)
            {
                throw new ConfigurationErrorsException("Connection string settings \"" + connectionName + "\" not found");
            }

            var databaseName = configuration.GetString("database");
            var collectionName = configuration.GetString("collection");
            var subscriptionTrackingService = new MongoDBSubscriptionTrackingService(connectionStringSettings, databaseName, collectionName);

            var multicast = configuration.Multicast;
            if (multicast == null || !multicast.Enabled)
            {
                return Task.FromResult<ISubscriptionTrackingService>(subscriptionTrackingService);
            }

            var multicastTrackingService = new MulticastSubscriptionTrackingService(
                subscriptionTrackingService, multicast.Address, multicast.Port);

            return Task.FromResult<ISubscriptionTrackingService>(multicastTrackingService);
        }

        /// <inheritdoc />
        public Task<IMessageJournal> CreateMessageJournal(JournalingElement configuration)
        {
            var connectionName = configuration.GetString("connectionName");
            if (string.IsNullOrWhiteSpace(connectionName))
            {
                throw new ConfigurationErrorsException(
                    "Attribute 'connectionName' is required for MongoDB message journal");
            }

            var connectionStringSettings = ConfigurationManager.ConnectionStrings[connectionName];
            if (connectionStringSettings == null)
            {
                throw new ConfigurationErrorsException("Connection string settings \"" + connectionName + "\" not found");
            }

            var databaseName = configuration.GetString("database");
            var collectionName = configuration.GetString("collection");
            var sqlMessageJournalingService = new MongoDBMessageJournal(connectionStringSettings, databaseName, collectionName);
            return Task.FromResult<IMessageJournal>(sqlMessageJournalingService);
        }
    }
}
