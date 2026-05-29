using System;

namespace TripleDetection.Models
{
    public class DetectionResult
    {
        public bool IsOK { get; set; }
        public string CodeInfo { get; set; }
        public int CharCount { get; set; }
        public double Confidence { get; set; }
        public string ImagePath { get; set; }
        public DateTime DetectionTime { get; set; }
        public long ElapsedMs { get; set; }
        public string ErrorMessage { get; set; }
    }
}