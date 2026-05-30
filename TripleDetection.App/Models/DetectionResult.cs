using System;

namespace TripleDetection.Models
{
    public class DetectionResult
    {
        public bool IsOK { get; set; }
        public string BatchNumber { get; set; }
        public string ProductionDate { get; set; }
        public string ExpirationDate { get; set; }
        public string ImagePath { get; set; }
        public DateTime DetectionTime { get; set; }
        public long ElapsedMs { get; set; }
        public string ErrorMessage { get; set; }
    }
}
