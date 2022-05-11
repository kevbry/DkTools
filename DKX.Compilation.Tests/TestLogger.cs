using DK.Diagnostics;
using NUnit.Framework;
using System.Threading.Tasks;

namespace DKX.Compilation.Tests
{
    class TestLogger : ILogger
    {
        private LogLevel _level;

        public LogLevel Level { get => _level; set => _level = value; }

        public void Write(LogLevel level, string message)
        {
            TestContext.Out.WriteLine($"({level}) {message}");
        }

        public void Write(LogLevel level, string format, params object[] args)
        {
            TestContext.Out.WriteLine($"({level}) {string.Format(format, args)}");
        }

        public async Task WriteAsync(LogLevel level, string message)
        {
            await TestContext.Out.WriteLineAsync($"({level}) {message}");
        }

        public async Task WriteAsync(LogLevel level, string format, params object[] args)
        {
            await TestContext.Out.WriteLineAsync($"({level}) {string.Format(format, args)}");
        }
    }
}
