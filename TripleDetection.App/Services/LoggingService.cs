using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace TripleDetection.Services
{
    public class LoggingService
    {
        private readonly string _logPath;
        private readonly object _lock = new object();

        public event EventHandler<LogEntry> OnLogAdded;

        public LoggingService(string logPath)
        {
            _logPath = logPath;
        }

        public void Log(string message)
        {
            var entry = new LogEntry
            {
                Timestamp = DateTime.Now,
                Message = message
            };

            OnLogAdded?.Invoke(this, entry);

            Task.Run(() => SaveLog(entry));
        }

        private void SaveLog(LogEntry entry)
        {
            try
            {
                if (!Directory.Exists(_logPath))
                    Directory.CreateDirectory(_logPath);

                string filename = Path.Combine(_logPath, DateTime.Now.ToString("yyyy-MM-dd") + ".log");
                string line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss:ffff}\t{entry.Message}";

                lock (_lock)
                {
                    File.AppendAllText(filename, line + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine($"LoggingService: Failed to save log - {ex.Message}");
            }
        }

        public void Clear()
        {
            foreach (var handler in OnLogAdded?.GetInvocationList() ?? Array.Empty<Delegate>())
            {
                OnLogAdded -= (EventHandler<LogEntry>)handler;
            }
        }
    }

    public class LogEntry
    {
        public DateTime Timestamp { get; init; }
        public string Message { get; init; }
    }
}