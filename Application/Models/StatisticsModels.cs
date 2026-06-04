using System;
using System.Collections.Generic;

namespace TripleDetection.Application.Models
{
    // ==================== Operation Log Statistics ====================

    public class UserActivityStatistics
    {
        public int UserId { get; set; }
        public string UserName { get; set; }
        public int TotalOperations { get; set; }
        public int LoginCount { get; set; }
        public int TaskOperations { get; set; }
        public int DetectionOperations { get; set; }
        public DateTime LastActivityAt { get; set; }
    }

    public class ActionDistribution
    {
        public string Action { get; set; }
        public int Count { get; set; }
        public double Percentage { get; set; }
    }

    public class DailyOperationTrend
    {
        public DateTime Date { get; set; }
        public int TotalCount { get; set; }
        public Dictionary<string, int> ActionBreakdown { get; set; }
    }

    public class TaskStatusTransitionStatistics
    {
        public int TotalTransitions { get; set; }
        public Dictionary<string, int> TransitionCounts { get; set; }
        public string MostCommonTransition { get; set; }
    }

    // ==================== Detection Record Statistics ====================

    public class DailyDetectionSummary
    {
        public DateTime Date { get; set; }
        public int TotalDetections { get; set; }
        public int OkCount { get; set; }
        public int NgCount { get; set; }
        public double PassRate { get; set; }
        public double AverageElapsedMs { get; set; }
    }

    public class PassRateStatistics
    {
        public int TotalCount { get; set; }
        public int OkCount { get; set; }
        public int NgCount { get; set; }
        public double PassRate { get; set; }
        public double MinPassRate { get; set; }
        public double MaxPassRate { get; set; }
    }

    public class DailyPassRateTrend
    {
        public DateTime Date { get; set; }
        public int Total { get; set; }
        public int Ok { get; set; }
        public int Ng { get; set; }
        public double PassRate { get; set; }
    }

    public class ProductDetectionStatistics
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public string ProductCode { get; set; }
        public int TotalDetections { get; set; }
        public int OkCount { get; set; }
        public int NgCount { get; set; }
        public double PassRate { get; set; }
    }

    public class DetectionTimeStatistics
    {
        public double AverageElapsedMs { get; set; }
        public double MinElapsedMs { get; set; }
        public double MaxElapsedMs { get; set; }
        public Dictionary<string, double> ByTaskType { get; set; }
    }
}