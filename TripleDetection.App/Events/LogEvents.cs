using Prism.Events;
using TripleDetection.App.Services.System;

namespace TripleDetection.Events
{
    /// <summary>
    /// Published when a log entry is added. Payload is the LogEntry.
    /// </summary>
    public class LogAddedEvent : PubSubEvent<LogEntry>
    {
    }
}
