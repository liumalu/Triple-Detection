namespace TripleDetection.Models
{
    public class DetectionResult
    {
        public bool IsOK { get; set; }
        public string CodeInfo { get; set; }
        public int CharCount { get; set; }
        public double Confidence { get; set; }
        public string ImagePath { get; set; }
        public System.DateTime DetectionTime { get; set; }
    }
}