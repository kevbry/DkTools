using System;

namespace DK.Diagnostics
{
    public class ConsoleLogger : ILogger
    {
#if DEBUG
        private LogLevel _level = LogLevel.Debug;
#else
        private LogLevel _level = LogLevel.Info;
#endif

        public LogLevel Level { get => _level; set => _level = value; }

        public void Write(LogLevel level, string message) => Console.WriteLine($"({level}) {message}");

        public void Write(LogLevel level, string format, params object[] args) => Console.WriteLine($"({level}) {string.Format(format, args)}");
    }
}
