﻿using System;
using System.Collections.Generic;
using Serilog.Configuration;
using Serilog.Core;
using Serilog.Events;

namespace Serilog.Sinks.Slack
{
    /// <summary>
    /// Provides extension methods on <see cref="LoggerSinkConfiguration"/>.
    /// </summary>
    public static class SeqLoggerConfigurationExtensions
    {
        /// <summary>
        /// <see cref="LoggerSinkConfiguration"/> extension that provides configuration chaining.
        /// <example>
        ///     new LoggerConfiguration()
        ///         .MinimumLevel.Verbose()
        ///         .WriteTo.Slack("webHookUrl", "channel" ,"username", "icon")
        ///         .CreateLogger();
        /// </example>
        /// </summary>
        /// <param name="loggerSinkConfiguration">Instance of <see cref="LoggerSinkConfiguration"/> object.</param>
        /// <param name="webhookUrl">Slack team post URI.</param>
        /// <param name="logEventProperties"></param>
        /// <param name="tidyStackTraces"></param>
        /// <param name="customChannel">Name of Slack channel to which message should be posted.</param>
        /// <param name="customUsername">User name that will be displayed as a name of the message sender.</param>
        /// <param name="customIcon">Icon that will be used as a sender avatar.</param>
        /// <param name="restrictedToMinimumLevel"><see cref="LogEventLevel"/> value that specifies minimum logging level that will be allowed to be logged.</param>
        /// <returns>Instance of <see cref="LoggerConfiguration"/> object.</returns>
        public static LoggerConfiguration Slack(this LoggerSinkConfiguration loggerSinkConfiguration, string webhookUrl,  string customChannel = null, string customUsername = null, string customIcon = null, IEnumerable<string> logEventProperties = null, bool tidyStackTraces = false, LogEventLevel restrictedToMinimumLevel = LevelAlias.Minimum)
        {
            if (loggerSinkConfiguration == null)
                throw new ArgumentNullException(nameof(loggerSinkConfiguration));

            if (string.IsNullOrWhiteSpace(webhookUrl))
                throw new ArgumentNullException(nameof(webhookUrl));

            ILogEventSink sink = new SlackSink(webhookUrl, customChannel, customUsername, customIcon, logEventProperties, tidyStackTraces);

            return loggerSinkConfiguration.Sink(sink, restrictedToMinimumLevel);
        }
    }
}