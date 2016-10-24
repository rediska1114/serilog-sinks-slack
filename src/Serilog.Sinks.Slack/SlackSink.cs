using System;
using System.Collections.Generic;
using System.Dynamic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Serilog.Events;
using Serilog.Sinks.PeriodicBatching;

namespace Serilog.Sinks.Slack
{
    /// <summary>
    /// Implements <see cref="PeriodicBatchingSink"/> and provides means needed for sending Serilog log events to Slack.
    /// </summary>
    public class SlackSink : PeriodicBatchingSink
    {
        private static readonly HttpClient Client = new HttpClient();
        private static readonly Dictionary<LogEventLevel, string> Colors = new Dictionary<LogEventLevel, string>
        {
            {LogEventLevel.Verbose, "#777"},
            {LogEventLevel.Debug, "#777"},
            {LogEventLevel.Information, "#5bc0de"},
            {LogEventLevel.Warning, "#f0ad4e"},
            {LogEventLevel.Error, "#d9534f"},
            {LogEventLevel.Fatal, "#d9534f"}
        };

        private readonly string _webhookUrl;
        private readonly string _customChannel;
        private readonly string _customUserName;
        private readonly string _customIcon;
        private readonly IEnumerable<string> _logEventProperties;
        private readonly bool _tidyStackTraces;
        private readonly JsonSerializer _serializer;

        /// <summary>
        /// Initializes new instance of <see cref="SlackSink"/>.
        /// </summary>
        /// <param name="webhookUrl">Slack team post URI.</param>
        /// <param name="customChannel">Name of Slack channel to which message should be posted.</param>
        /// <param name="customUserName">User name that will be displayed as a name of the message sender.</param>
        /// <param name="customIcon">Icon that will be used as a sender avatar.</param>
        /// <param name="logEventProperties">A collection of custom log event properties to include as attachments</param>
        /// <param name="tidyStackTraces"></param>
        public SlackSink(
            string webhookUrl,
            string customChannel = null,
            string customUserName = null,
            string customIcon = null,
            IEnumerable<string> logEventProperties = null,
            bool tidyStackTraces = false)
            : base(50, TimeSpan.FromSeconds(5))
        {
            _webhookUrl = webhookUrl;
            _customChannel = customChannel;
            _customUserName = customUserName;
            _customIcon = customIcon;
            _logEventProperties = logEventProperties ?? Enumerable.Empty<string>();
            _tidyStackTraces = tidyStackTraces;
            _serializer = new JsonSerializer();
        }

        /// <summary>
        /// Overrides <see cref="PeriodicBatchingSink.EmitBatchAsync"/> method and uses <see cref="HttpClient"/> to post <see cref="LogEvent"/> to Slack.
        /// /// </summary>
        /// <param name="events">Collection of <see cref="LogEvent"/>.</param>
        /// <returns>Awaitable task.</returns>
        protected override async Task EmitBatchAsync(IEnumerable<LogEvent> events)
        {
            foreach (var logEvent in events)
            {
                var message = CreateMessage(logEvent, _customChannel, _customUserName, _customIcon, _logEventProperties, _tidyStackTraces);

                var json = SerializeMessage(message);

                await Client.PostAsync(_webhookUrl, new StringContent(json));
            }
        }

        //we do this to ignore any default JSON Serialization settings
        private string SerializeMessage(dynamic message)
        {
            var sb = new StringBuilder(256);
            var stringWriter = new StringWriter(sb, CultureInfo.InvariantCulture);
            using (var jsonWriter = new JsonTextWriter(stringWriter))
            {
                jsonWriter.Formatting = Newtonsoft.Json.Formatting.Indented;
                _serializer.Serialize(jsonWriter, message);
            }
            return sb.ToString();
        }

        private static dynamic CreateMessage(LogEvent logEvent, string channel, string username, string emoji, IEnumerable<string> logEventProperties, bool tidyStackTraces)
        {
            dynamic message = new ExpandoObject();
            message.text = logEvent.RenderMessage();
            message.channel = string.IsNullOrWhiteSpace(channel) ? string.Empty : channel;
            message.username = string.IsNullOrWhiteSpace(username) ? string.Empty : username;
            message.icon_emoji = string.IsNullOrWhiteSpace(emoji) ? string.Empty : emoji;
            message.attachments = CreateAttachments(logEvent, logEventProperties, tidyStackTraces);

            return message;
        }

        private static List<dynamic> CreateAttachments(LogEvent logEvent, IEnumerable<string> logEventProperties, bool tidyStackTraces)
        {
            var attachments = new List<dynamic>
            {
                new
                {
                    fallback = $"[{logEvent.Level}]{logEvent.RenderMessage()}",
                    color = Colors[logEvent.Level],
                    fields = new List<dynamic>
                    {
                        new {title = "Level", value = logEvent.Level.ToString()},
                        new {title = "Timestamp", value = logEvent.Timestamp.ToString()}
                    }
                }
            };

            //write any custom log event properties we have been asked to include
            var initAttachment = attachments[0];
            foreach (var propertyName in logEventProperties)
            {
                LogEventPropertyValue value;
                if (logEvent.Properties.TryGetValue(propertyName, out value))
                    initAttachment.fields.Add(new { title = propertyName, value = value.ToString() });
            }

            if (logEvent.Exception == null)
                return attachments;

            var finalStackTrace = logEvent.Exception.StackTrace;
            if (tidyStackTraces)
            {
                var stackTrace = logEvent.Exception.StackTrace.Split('\n');
                var filteredStackTrace = new List<string>();
                var lastWasAsync = false;

                foreach (var line in stackTrace)
                {
                    if (line.TrimStart().StartsWith("at System.Runtime.CompilerServices.")
                        )
                    {
                        lastWasAsync = true;
                        continue;
                    }

                    if (lastWasAsync)
                    {
                        filteredStackTrace.Add(" -- (async)\r");
                    }

                    filteredStackTrace.Add(line);

                    lastWasAsync = false;
                }

                finalStackTrace = string.Join("\n", filteredStackTrace.ToArray());
            }

            attachments.Add(new
            {
                title = "Exception",
                fallback = $"Exception: {logEvent.Exception.Message} \n {finalStackTrace}",
                color = Colors[LogEventLevel.Fatal],
                fields = new List<dynamic>
                {
                    new {title = "Message", value = logEvent.Exception.Message},
                    new {title = "Type", value = "`" + logEvent.Exception.GetType().Name + "`"},
                    new {title = "Stack Trace", value = "```" + finalStackTrace + "```", @short = false}
                },
                mrkdwn_in = new List<string> { "fields" }
            });

            return attachments;
        }
    }
}