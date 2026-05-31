using System;

namespace TripleDetection.Presentation.Models
{
    public class CommunicationSettings
    {
        public string CameraIp { get; set; } = "192.168.1.100";
        public int CameraPort { get; set; } = 5000;
        public string PlcIp { get; set; } = "192.168.1.200";
        public int PlcPort { get; set; } = 5001;
        public string PlcType { get; set; } = "Mitsubishi";
        public int BaudRate { get; set; } = 115200;
    }
}