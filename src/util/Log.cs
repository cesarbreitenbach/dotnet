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
                .File(System.AppDomain.CurrentDomain.FriendlyName + ".log", fileSizeLimitBytes: 8 * 1024 )
                .WriteTo
                .Console(Serilog.Events.LogEventLevel.Debug)
                .CreateLogger();
        }
    }
}