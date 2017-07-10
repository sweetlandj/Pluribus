﻿using System;
using System.Configuration;
using Platibus.Diagnostics;
using Platibus.SQL;
using Platibus.SQL.Commands;

namespace Platibus.Config.Extensibility
{
    /// <summary>
    /// Factory class for initializing SQL command builders
    /// </summary>
    public class CommandBuildersFactory
    {
        private readonly ConnectionStringSettings _connectionStringSettings;
        private readonly IDiagnosticService _diagnosticService;

        /// <summary>
        /// Initializes a new <see cref="CommandBuildersFactory"/>
        /// </summary>
        /// <param name="connectionStringSettings">The connection string settings for the
        /// connections that will be used to create and execute the commands</param>
        /// <param name="diagnosticService">(Optional) The service through which diagnostic events
        /// are reported and processed</param>
        public CommandBuildersFactory(ConnectionStringSettings connectionStringSettings, IDiagnosticService diagnosticService = null)
        {
            if (connectionStringSettings == null) throw new ArgumentNullException("connectionStringSettings");
            _connectionStringSettings = connectionStringSettings;
            _diagnosticService = diagnosticService ?? DiagnosticService.DefaultInstance;
        }

        /// <summary>
        /// Initializes <see cref="IMessageJournalingCommandBuilders"/>
        /// </summary>
        /// <returns>Returns initialized <see cref="IMessageJournalingCommandBuilders"/></returns>
        public IMessageJournalingCommandBuilders InitMessageJournalingCommandBuilders()
        {
            var providerName = _connectionStringSettings.ProviderName;
            IMessageJournalingCommandBuildersProvider provider;
            if (string.IsNullOrWhiteSpace(providerName))
            {
                _diagnosticService.Emit(new SQLEventBuilder(this, DiagnosticEventType.ConfigurationDefault)
                {
                    Detail = "No provider name specified; using default provider (MSSQL)",
                    ConnectionName = _connectionStringSettings.Name,
                    
                }.Build());
                provider = new MSSQLCommandBuildersProvider();
            }
            else
            {
                provider = ProviderHelper.GetProvider<IMessageJournalingCommandBuildersProvider>(providerName);
            }
            return provider.GetMessageJournalingCommandBuilders(_connectionStringSettings);
        }

        /// <summary>
        /// Initializes <see cref="IMessageQueueingCommandBuilders"/>
        /// </summary>
        /// <returns>Returns initialized <see cref="IMessageQueueingCommandBuilders"/></returns>
        public IMessageQueueingCommandBuilders InitMessageQueueingCommandBuilders()
        {
            var providerName = _connectionStringSettings.ProviderName;
            IMessageQueueingCommandBuildersProvider provider;
            if (string.IsNullOrWhiteSpace(providerName))
            {
                _diagnosticService.Emit(new SQLEventBuilder(this, DiagnosticEventType.ConfigurationDefault)
                {
                    Detail = "No provider name specified; using default provider (MSSQL)",
                    ConnectionName = _connectionStringSettings.Name,

                }.Build());
                provider = new MSSQLCommandBuildersProvider();
            }
            else
            {
                provider = ProviderHelper.GetProvider<IMessageQueueingCommandBuildersProvider>(providerName);
            }
            return provider.GetMessageQueueingCommandBuilders(_connectionStringSettings);
        }

        /// <summary>
        /// Initializes <see cref="ISubscriptionTrackingCommandBuilders"/>
        /// </summary>
        /// <returns>Returns initialized <see cref="ISubscriptionTrackingCommandBuilders"/></returns>
        public ISubscriptionTrackingCommandBuilders InitSubscriptionTrackingCommandBuilders()
        {
            var providerName = _connectionStringSettings.ProviderName;
            ISubscriptionTrackingCommandBuildersProvider provider;
            if (string.IsNullOrWhiteSpace(providerName))
            {
                _diagnosticService.Emit(new SQLEventBuilder(this, DiagnosticEventType.ConfigurationDefault)
                {
                    Detail = "No provider name specified; using default provider (MSSQL)",
                    ConnectionName = _connectionStringSettings.Name,

                }.Build());
                provider = new MSSQLCommandBuildersProvider();
            }
            else
            {
                provider = ProviderHelper.GetProvider<ISubscriptionTrackingCommandBuildersProvider>(providerName);
            }
            return provider.GetSubscriptionTrackingCommandBuilders(_connectionStringSettings);
        }
    }
}