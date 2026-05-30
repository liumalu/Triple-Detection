using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using System.Diagnostics;
using Prism.Events;
using TripleDetection.Events;

namespace TripleDetection.App.Services.System
{
    public class LoggingService
    {
        private readonly string _logPath;
        private readonly object _lock = new object();
        private readonly IEventAggregator _eventAggregator;

        /// <summary>
        /// Backward-compatible event. Consider subscribing to LogAddedEvent via IEventAggregator instead.
        /// </summary>
        public event EventHandler<LogEntry> OnLogAdded;

        public LoggingService(string logPath) : this(logPath, null)
        {
        }

        public LoggingService(string logPath, IEventAggregator eventAggregator)
        {
            _logPath = logPath;
            _eventAggregator = eventAggregator;
            CleanupOldLogs();
        }

        private void CleanupOldLogs()
        {
            try
            {
                if (!Directory.Exists(_logPath))
                    return;

                var threshold = TimeSpan.FromDays(30);
                var now = DateTime.Now;

                foreach (var file in Directory.GetFiles(_logPath, "*.log"))
                {
                    try
                    {
                        var fileInfo = new FileInfo(file);
                        if ((now - fileInfo.LastWriteTime) > threshold)
                        {
                            File.Delete(file);
                        }
                    }
                    catch
                    {
                        // File may be locked by another process - skip it silently
                    }
                }
            }
            catch
            {
                // Logs directory may not be accessible - skip cleanup silently
            }
        }

        public void Log(string message)
        {
            var entry = new LogEntry();
            entry.Timestamp = DateTime.Now;
            entry.Message = message;

            // Publish via EventAggregator (preferred)
            _eventAggregator?.GetEvent<LogAddedEvent>().Publish(entry);

            // Legacy event (backward compatibility)
            OnLogAdded?.Invoke(this, entry);

            Task.Run(() => SaveLog(entry));
        }

        private void SaveLog(LogEntry entry)
        {
            try
            {
                if (!Directory.Exists(_logPath))
                    Directory.CreateDirectory(_logPath);

                string filename = Path.Combine(_logPath, "app.log");
                string line = $"{entry.Timestamp:yyyy-MM-dd HH:mm:ss:ffff}\t{entry.Message}";

                lock (_lock)
                {
                    File.AppendAllText(filename, line + Environment.NewLine);
                }
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"LoggingService: Failed to save log - {ex.Message}");
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
        public DateTime Timestamp { get; set; }
        public string Message { get; set; }
    }
}
