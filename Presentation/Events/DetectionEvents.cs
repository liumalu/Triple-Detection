using Prism.Events;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Presentation.Events
{
    /// <summary>
    /// Published when a detection result is received from the VisionMaster SDK.
    /// Payload is the parsed DetectionResult.
    /// </summary>
    public class DetectionResultEvent : PubSubEvent<DetectionResult>
    {
    }
}