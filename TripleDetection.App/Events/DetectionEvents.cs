using Prism.Events;
using TripleDetection.Models;

namespace TripleDetection.Events
{
    /// <summary>
    /// Published when a detection result is received from the VisionMaster SDK.
    /// Payload is the parsed DetectionResult.
    /// </summary>
    public class DetectionResultEvent : PubSubEvent<DetectionResult>
    {
    }
}
