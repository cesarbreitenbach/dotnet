using Serilog;
using System;
using System.Collections.Generic;
using System.Text;

namespace Utils
{
    public class Log
    {
        public static Serilog.Core.Logger CriarLogger()
        {
            return new LoggerConfiguration()
                .WriteTo
                .File(System.AppDomain.CurrentDomain.FriendlyName + ".log", fileSizeLimitBytes: 50 * 1024,  shared: true)
                .WriteTo
                .Console(Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();
        }
    }
}