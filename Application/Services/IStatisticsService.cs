using System;
using System.Collections.Generic;
using TripleDetection.Application.Models;

namespace TripleDetection.Application.Services
{
    public interface IStatisticsService
    {
        // ==================== Operation Log Statistics ====================

        UserActivityStatistics GetUserActivityStats(int userId, DateTime startDate, DateTime endDate);

        IEnumerable<ActionDistribution> GetActionDistribution(DateTime startDate, DateTime endDate);

        IEnumerable<DailyOperationTrend> GetDailyOperationTrend(DateTime startDate, DateTime endDate);

        TaskStatusTransitionStatistics GetTaskStatusTransitions(DateTime startDate, DateTime endDate);

        // ==================== Detection Record Statistics ====================

        DailyDetectionSummary GetDailyDetectionSummary(DateTime date);

        PassRateStatistics GetPassRateStatistics(DateTime startDate, DateTime endDate, int? taskId = null);

        IEnumerable<DailyPassRateTrend> GetDailyPassRateTrend(DateTime startDate, DateTime endDate, int? taskId = null);

        IEnumerable<ProductDetectionStatistics> GetProductStatistics(DateTime startDate, DateTime endDate);

        DetectionTimeStatistics GetDetectionTimeStatistics(DateTime startDate, DateTime endDate, int? taskId = null);
    }
}