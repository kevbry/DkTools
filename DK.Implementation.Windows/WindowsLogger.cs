using DK.Diagnostics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace DK.Implementation.Windows
{
    public class WindowsLogger : ILogger
    {
        private static StreamWriter _writer;
        private static StringBuilder _sb = new StringBuilder();
        private static SemaphoreSlim _lock = new SemaphoreSlim(1, 1);
        private static string _logDir;
        private static string _logFileNameFormat;

#if DEBUG
        private LogLevel _level = LogLevel.Debug;
#else
        private LogLevel _level = LogLevel.Info;
#endif

        private const int PurgeDays = 7;

        public WindowsLogger(string logDir, string logFileNameFormat)
        {
            try
            {
                _logDir = logDir ?? throw new ArgumentNullException(nameof(logDir));
                _logFileNameFormat = logFileNameFormat ?? throw new ArgumentNullException(nameof(logFileNameFormat));

                _lock.Wait();
                try
                {
                    CheckStream();
                }
                finally
                {
                    _lock.Release();
                }

                PurgeOldLogFiles();
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex.ToString());
            }
        }

        public LogLevel Level
        {
            get
            {
                return _level;
            }
            set
            {
                if (_level != value)
                {
                    _level = value;
                    this.Info("Log level changed to {0}.", _level);
                }
            }
        }

        public bool CheckStream()
        {
            if (_writer == null)
            {
                if (string.IsNullOrEmpty(_logDir) || string.IsNullOrEmpty(_logFileNameFormat)) return false;

                var logFileName = Path.Combine(_logDir, string.Format(_logFileNameFormat, DateTime.Now));
                _writer = new StreamWriter(logFileName);

                Write(LogLevel.Info, "Created log file: {0}", logFileName);
            }

            return true;
        }

        public void Close()
        {
            try
            {
                _lock.Wait();
                try
                {
                    if (_writer != null)
                    {
                        Write(LogLevel.Info, "Closing log file.");
                        _writer.Close();
                        _writer = null;
                    }
                }
                finally
                {
                    _lock.Release();
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(ex);
            }
        }

        private void PurgeOldLogFiles()
        {
            try
            {
                if (string.IsNullOrEmpty(_logDir) || string.IsNullOrEmpty(_logFileNameFormat)) return;

                var purgeDate = DateTime.Now.Subtract(TimeSpan.FromDays(PurgeDays));
                var removeList = new List<string>();

                foreach (var logFileName in Directory.GetFiles(_logDir))
                {
                    try
                    {
                        var fileInfo = new FileInfo(logFileName);
                        if (fileInfo.LastWriteTime < purgeDate) removeList.Add(logFileName);
                    }
                    catch (Exception ex)
                    {
                        this.Error(ex, "Error when examining log file for purge: {0}", logFileName);
                    }
                }

                foreach (var logFileName in removeList)
                {
                    try
                    {
                        var attribs = File.GetAttributes(logFileName);
                        if ((attribs & FileAttributes.ReadOnly) != 0) File.SetAttributes(logFileName, attribs & ~FileAttributes.ReadOnly);

                        File.Delete(logFileName);
                    }
                    catch (Exception ex)
                    {
                        this.Error(ex, "Error when deleting old log file: {0}", logFileName);
                    }
                }
            }
            catch (Exception ex)
            {
                this.Error(ex, "Error when purging old log files.");
            }
        }

        public async Task WriteAsync(LogLevel level, string message)
        {
            if (level < _level) return;

            await _lock.WaitAsync();
            try
            {
                _sb.Clear();
                _sb.Append("[");
                _sb.Append(DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff"));
                _sb.Append("] (");
                _sb.Append(level.ToString());
                _sb.Append(") {");
                _sb.Append(System.Threading.Thread.CurrentThread.ManagedThreadId);
                _sb.Append("} ");
                _sb.Append(message);
                var msg = _sb.ToString();
#if DEBUG
                System.Diagnostics.Debug.WriteLine(msg);
#endif
                if (!CheckStream()) return;
                if (_writer != null) await _writer.WriteLineAsync(msg);
            }
            catch (Exception ex)
            {
#if DEBUG
                System.Diagnostics.Debug.WriteLine(ex.ToString());
#endif
                try
                {
                    _writer?.Close();
                    _writer = null;
                }
                catch (Exception)
                {
                    _writer = null;
                }
            }
            finally
            {
                _lock.Release();
            }
        }

        public void Write(LogLevel level, string message)
        {
            if (level < _level) return;

            WriteAsync(level, message).GetAwaiter().GetResult();
        }

        public void Write(LogLevel level, string format, params object[] args)
        {
            if (level < _level) return;

            Write(level, string.Format(format, args));
        }

        public async Task WriteAsync(LogLevel level, string format, params object[] args)
        {
            if (level < _level) return;

            await WriteAsync(level, string.Format(format, args));
        }
    }
}
