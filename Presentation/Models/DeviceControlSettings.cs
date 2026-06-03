namespace TripleDetection.Presentation.Models
{
    public class DeviceControlSettings
    {
        // === 既有字段 ===
        public string LightSourceType { get; set; } = "LED";
        public int CaptureDelayMs { get; set; } = 100;
        public int CaptureFeedbackTimeoutMs { get; set; } = 5000;
        public int RejectDelayMs { get; set; } = 50;
        public int RejectDurationMs { get; set; } = 200;
        public int ConsecutiveRejectsToStopLine { get; set; } = 10;

        // === Modbus TCP 配置 ===
        public string ModbusTcpIp { get; set; } = "192.168.1.100";
        public int ModbusTcpPort { get; set; } = 502;
        public int RejectCoilAddress { get; set; } = 1;       // 剔除继电器
        public int LineStopCoilAddress { get; set; } = 2;     // 产线停止继电器
        public int ConnectionTimeoutMs { get; set; } = 3000;
        public bool EnableLineStopOnConsecutiveRejects { get; set; } = false;
        public bool RequireIOConnectionToStartTask { get; set; } = false;
    }
}