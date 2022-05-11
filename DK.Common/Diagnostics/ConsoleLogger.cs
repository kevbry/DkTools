using System;
using System.Threading;
using System.Threading.Tasks;

namespace DK.Diagnostics
{
    public class ConsoleLogger : ILogger
    {
#if DEBUG
        private LogLevel _level = LogLevel.Debug;
#else
        private LogLevel _level = LogLevel.Info;
#endif

        private SemaphoreSlim _lock = new SemaphoreSlim(1, 1);

        public LogLevel Level { get => _level; set => _level = value; }

        public void Write(LogLevel level, string message)
        {
            if (level >= _level) WriteInner(level, message);
        }

        public void Write(LogLevel level, string format, params object[] args)
        {
            if (level >= _level) WriteInner(level, string.Format(format, args));
        }

        private void WriteInner(LogLevel level, string message)
        {
            var origColor = Console.ForegroundColor;

            _lock.Wait();
            try
            {
                switch (level)
                {
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.DarkCyan;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }

                Console.WriteLine(message);

#if DEBUG
                System.Diagnostics.Debug.WriteLine(message);
#endif
            }
#if DEBUG
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in loggedin: {ex}");
            }
#else
            catch (Exception) { }
#endif
            finally
            {
                Console.ForegroundColor = origColor;
                _lock.Release();
            }
        }

        public async Task WriteAsync(LogLevel level, string message)
        {
            if (level >= _level) await WriteInnerAsync(level, message);
        }

        public async Task WriteAsync(LogLevel level, string format, params object[] args)
        {
            if (level >= _level) await WriteInnerAsync(level, string.Format(format, args));
        }

        private async Task WriteInnerAsync(LogLevel level, string message)
        {
            var origColor = Console.ForegroundColor;

            await _lock.WaitAsync();
            try
            {
                switch (level)
                {
                    case LogLevel.Error:
                        Console.ForegroundColor = ConsoleColor.Red;
                        break;
                    case LogLevel.Warning:
                        Console.ForegroundColor = ConsoleColor.Yellow;
                        break;
                    case LogLevel.Debug:
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        break;
                    default:
                        Console.ForegroundColor = ConsoleColor.Gray;
                        break;
                }

                Console.WriteLine(message);

#if DEBUG
                System.Diagnostics.Debug.WriteLine(message);
#endif
            }
#if DEBUG
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"Exception in loggedin: {ex}");
            }
#else
            catch (Exception) { }
#endif
            finally
            {
                Console.ForegroundColor = origColor;
                _lock.Release();
            }
        }
    }
}
