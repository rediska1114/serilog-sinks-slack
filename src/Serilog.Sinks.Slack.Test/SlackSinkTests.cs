﻿using System;
using System.Threading;
using Xunit;

namespace Serilog.Sinks.Slack.Test
{
    public class SlackSinkTests
    {
        [Fact]
        public void SendMessageTests()
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Verbose()
                .WriteTo.Slack("")
                .CreateLogger();

            Log.Logger.Verbose("1 Verbose");
            Log.Logger.Debug("2 Debug");
            Log.Logger.Error("3 Error");

            try
            {
                throw new Exception("some logged exception!");
            }
            catch (Exception ex)
            {
                Log.Logger.Fatal(ex, "4 Fatal");
            }

            Log.Logger.Information("5 Information");
            Log.Logger.Warning("6 Warning");
            Log.Logger.Debug("7 Formatting {myProp}", new { myProp = "test" });
            
            Thread.Sleep(5000);
        }
    }
}
