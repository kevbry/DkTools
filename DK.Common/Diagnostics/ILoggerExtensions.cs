using System;
using System.Threading.Tasks;

namespace DK.Diagnostics
{
    public static class ILoggerExtensions
    {
        public static void Debug(this ILogger log, string message)
        {
            if (log.Level > LogLevel.Debug) return;

            log.Write(LogLevel.Debug, message);
        }

        public static void Debug(this ILogger log, string format, params object[] args)
        {
            if (log.Level > LogLevel.Debug) return;

            log.Write(LogLevel.Debug, string.Format(format, args));
        }

        public static void Debug(this ILogger log, Exception ex)
        {
            if (log.Level > LogLevel.Debug) return;

            log.Write(LogLevel.Debug, ex.ToString());
        }

        public static void Info(this ILogger log, string message)
        {
            if (log.Level > LogLevel.Info) return;

            log.Write(LogLevel.Info, message);
        }

        public static void Info(this ILogger log, string format, params object[] args)
        {
            if (log.Level > LogLevel.Info) return;

            log.Write(LogLevel.Info, string.Format(format, args));
        }

        public static void Info(this ILogger log, Exception ex)
        {
            if (log.Level > LogLevel.Info) return;

            log.Write(LogLevel.Info, ex.ToString());
        }

        public static void Warning(this ILogger log, string message)
        {
            if (log.Level > LogLevel.Warning) return;

            log.Write(LogLevel.Warning, message);
        }

        public static void Warning(this ILogger log, string format, params object[] args)
        {
            if (log.Level > LogLevel.Warning) return;

            log.Write(LogLevel.Warning, string.Format(format, args));
        }

        public static void Warning(this ILogger log, Exception ex)
        {
            if (log.Level > LogLevel.Warning) return;

            log.Write(LogLevel.Warning, ex.ToString());
        }

        public static void Warning(this ILogger log, Exception ex, string message)
        {
            if (log.Level > LogLevel.Warning) return;

            log.Write(LogLevel.Warning, string.Concat(message, "\r\n", ex));
        }

        public static void Warning(this ILogger log, Exception ex, string format, params object[] args)
        {
            if (log.Level > LogLevel.Warning) return;

            log.Warning(ex, string.Format(format, args));
        }

        public static void Error(this ILogger log, string message)
        {
            log.Write(LogLevel.Error, message);
        }

        public static void Error(this ILogger log, string format, params object[] args)
        {
            log.Write(LogLevel.Error, string.Format(format, args));
        }

        public static void Error(this ILogger log, Exception ex)
        {
            log.Write(LogLevel.Error, ex.ToString());
        }

        public static void Error(this ILogger log, Exception ex, string message)
        {
            log.Write(LogLevel.Error, string.Concat(message, "\r\n", ex));
        }

        public static void Error(this ILogger log, Exception ex, string format, params object[] args)
        {
            log.Error(ex, string.Format(format, args));
        }

        public static async Task DebugAsync(this ILogger log, string message)
        {
            if (log.Level > LogLevel.Debug) return;

            await log.WriteAsync(LogLevel.Debug, message);
        }

        public static async Task DebugAsync(this ILogger log, string format, params object[] args)
        {
            if (log.Level > LogLevel.Debug) return;

            await log.WriteAsync(LogLevel.Debug, string.Format(format, args));
        }

        public static async Task DebugAsync(this ILogger log, Exception ex)
        {
            if (log.Level > LogLevel.Debug) return;

            await log.WriteAsync(LogLevel.Debug, ex.ToString());
        }

        public static async Task InfoAsync(this ILogger log, string message)
        {
            if (log.Level > LogLevel.Info) return;

            await log.WriteAsync(LogLevel.Info, message);
        }

        public static async Task InfoAsync(this ILogger log, string format, params object[] args)
        {
            if (log.Level > LogLevel.Info) return;

            await log.WriteAsync(LogLevel.Info, string.Format(format, args));
        }

        public static async Task InfoAsync(this ILogger log, Exception ex)
        {
            if (log.Level > LogLevel.Info) return;

            await log.WriteAsync(LogLevel.Info, ex.ToString());
        }

        public static async Task WarningAsync(this ILogger log, string message)
        {
            if (log.Level > LogLevel.Warning) return;

            await log.WriteAsync(LogLevel.Warning, message);
        }

        public static async Task WarningAsync(this ILogger log, string format, params object[] args)
        {
            if (log.Level > LogLevel.Warning) return;

            await log.WriteAsync(LogLevel.Warning, string.Format(format, args));
        }

        public static async Task WarningAsync(this ILogger log, Exception ex)
        {
            if (log.Level > LogLevel.Warning) return;

            await log.WriteAsync(LogLevel.Warning, ex.ToString());
        }

        public static async Task WarningAsync(this ILogger log, Exception ex, string message)
        {
            if (log.Level > LogLevel.Warning) return;

            await log.WriteAsync(LogLevel.Warning, string.Concat(message, "\r\n", ex));
        }

        public static async Task WarningAsync(this ILogger log, Exception ex, string format, params object[] args)
        {
            if (log.Level > LogLevel.Warning) return;

            await log.WarningAsync(ex, string.Format(format, args));
        }

        public static async Task ErrorAsync(this ILogger log, string message)
        {
            await log.WriteAsync(LogLevel.Error, message);
        }

        public static async Task ErrorAsync(this ILogger log, string format, params object[] args)
        {
            await log.WriteAsync(LogLevel.Error, string.Format(format, args));
        }

        public static async Task ErrorAsync(this ILogger log, Exception ex)
        {
            await log.WriteAsync(LogLevel.Error, ex.ToString());
        }

        public static async Task ErrorAsync(this ILogger log, Exception ex, string message)
        {
            await log.WriteAsync(LogLevel.Error, string.Concat(message, "\r\n", ex));
        }

        public static async Task ErrorAsync(this ILogger log, Exception ex, string format, params object[] args)
        {
            await log.ErrorAsync(ex, string.Format(format, args));
        }
    }
}
