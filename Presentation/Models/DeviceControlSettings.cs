namespace TripleDetection.Presentation.Models
{
    public class DeviceControlSettings
    {
        public string LightSourceType { get; set; } = "LED";
        public int CaptureDelayMs { get; set; } = 100;
        public int CaptureFeedbackTimeoutMs { get; set; } = 5000;
        public int RejectDelayMs { get; set; } = 50;
        public int RejectDurationMs { get; set; } = 200;
        public int ConsecutiveRejectsToStopLine { get; set; } = 10;
    }
}