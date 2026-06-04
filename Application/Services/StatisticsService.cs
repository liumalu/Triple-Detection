// Application/Services/StatisticsService.cs
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data.SQLite;
using TripleDetection.Application.Models;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Application.Services
{

public class StatisticsService : IStatisticsService
{
    private readonly string _connectionString;

    public StatisticsService(IDbConnectionFactory connectionFactory)
    {
        _connectionString = connectionFactory.CreateConnection().ConnectionString;
    }

    // ==================== Operation Log Statistics ====================

    public UserActivityStatistics GetUserActivityStats(int userId, DateTime startDate, DateTime endDate)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    COUNT(*) as TotalOperations,
    SUM(CASE WHEN Action = 'LOGIN' THEN 1 ELSE 0 END) as LoginCount,
    SUM(CASE WHEN Action LIKE 'TASK_%' THEN 1 ELSE 0 END) as TaskOperations,
    SUM(CASE WHEN Action LIKE 'DETECTION_%' THEN 1 ELSE 0 END) as DetectionOperations,
    MAX(CreateAt) as LastActivityAt
FROM AuditLogs
WHERE UserId = @UserId AND CreateAt >= @StartDate AND CreateAt <= @EndDate AND IsDeleted = 0";
                cmd.Parameters.AddWithValue("@UserId", userId);
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var lastActivityStr = reader.GetString(4);
                        var stats = new UserActivityStatistics
                        {
                            UserId = userId,
                            TotalOperations = reader.GetInt32(0),
                            LoginCount = reader.GetInt32(1),
                            TaskOperations = reader.GetInt32(2),
                            DetectionOperations = reader.GetInt32(3),
                            LastActivityAt = string.IsNullOrEmpty(lastActivityStr) ? DateTime.MinValue : DateTime.Parse(lastActivityStr)
                        };
                        return stats;
                    }
                }
                return new UserActivityStatistics { UserId = userId };
            }
        }
    }

    public IEnumerable<ActionDistribution> GetActionDistribution(DateTime startDate, DateTime endDate)
    {
        var result = new List<ActionDistribution>();
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT Action, COUNT(*) as Count
FROM AuditLogs
WHERE CreateAt >= @StartDate AND CreateAt <= @EndDate AND IsDeleted = 0
GROUP BY Action
ORDER BY Count DESC";
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));

                int total = 0;
                using (var reader = cmd.ExecuteReader())
                {
                    var temp = new List<ActionDistribution>();
                    while (reader.Read())
                    {
                        var action = reader.GetString(0);
                        var count = reader.GetInt32(1);
                        total += count;
                        temp.Add(new ActionDistribution { Action = action, Count = count });
                    }
                    foreach (var item in temp)
                        item.Percentage = total > 0 ? Math.Round((double)item.Count / total * 100, 2) : 0;
                    result.AddRange(temp);
                }
            }
        }
        return result;
    }

    public IEnumerable<DailyOperationTrend> GetDailyOperationTrend(DateTime startDate, DateTime endDate)
    {
        var result = new List<DailyOperationTrend>();
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT DATE(CreateAt) as OpDate, Action, COUNT(*) as Count
FROM AuditLogs
WHERE CreateAt >= @StartDate AND CreateAt <= @EndDate AND IsDeleted = 0
GROUP BY DATE(CreateAt), Action
ORDER BY OpDate";
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));

                var dateGroups = new Dictionary<string, DailyOperationTrend>();
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var date = reader.GetString(0);
                        var action = reader.GetString(1);
                        var count = reader.GetInt32(2);

                        if (!dateGroups.ContainsKey(date))
                            dateGroups[date] = new DailyOperationTrend { Date = DateTime.Parse(date), ActionBreakdown = new Dictionary<string, int>() };
                        dateGroups[date].TotalCount += count;
                        dateGroups[date].ActionBreakdown[action] = count;
                    }
                }
                result.AddRange(dateGroups.Values.OrderBy(x => x.Date));
            }
        }
        return result;
    }

    public TaskStatusTransitionStatistics GetTaskStatusTransitions(DateTime startDate, DateTime endDate)
    {
        var result = new TaskStatusTransitionStatistics { TransitionCounts = new Dictionary<string, int>() };
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT FromStatus, ToStatus, COUNT(*) as Count
FROM AuditLogs
WHERE Action IN ('TASK_APPROVE', 'TASK_START', 'TASK_COMPLETE', 'TASK_UPDATE')
    AND CreateAt >= @StartDate AND CreateAt <= @EndDate AND IsDeleted = 0
    AND FromStatus IS NOT NULL AND FromStatus != ''
    AND ToStatus IS NOT NULL AND ToStatus != ''
GROUP BY FromStatus, ToStatus
ORDER BY Count DESC";
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));

                int maxCount = 0;
                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var from = reader.GetString(0);
                        var to = reader.GetString(1);
                        var count = reader.GetInt32(2);
                        var key = $"{from}→{to}";
                        result.TotalTransitions += count;
                        result.TransitionCounts[key] = count;
                        if (count > maxCount)
                        {
                            maxCount = count;
                            result.MostCommonTransition = key;
                        }
                    }
                }
            }
        }
        return result;
    }

    // ==================== Detection Record Statistics ====================

    public DailyDetectionSummary GetDailyDetectionSummary(DateTime date)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    COUNT(*) as Total,
    SUM(CASE WHEN IsOK = 1 THEN 1 ELSE 0 END) as OkCount,
    SUM(CASE WHEN IsOK = 0 THEN 1 ELSE 0 END) as NgCount,
    AVG(ElapsedMs) as AvgElapsed
FROM DetectionRecords
WHERE DATE(DetectionTime) = @Date AND IsDeleted = 0";
                cmd.Parameters.AddWithValue("@Date", date.ToString("yyyy-MM-dd"));

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var total = reader.GetInt32(0);
                        var ok = reader.GetInt32(1);
                        var ng = reader.GetInt32(2);
                        var avgMs = reader.IsDBNull(3) ? 0 : reader.GetDouble(3);
                        return new DailyDetectionSummary
                        {
                            Date = date,
                            TotalDetections = total,
                            OkCount = ok,
                            NgCount = ng,
                            PassRate = total > 0 ? Math.Round((double)ok / total * 100, 2) : 0,
                            AverageElapsedMs = avgMs
                        };
                    }
                }
                return new DailyDetectionSummary { Date = date };
            }
        }
    }

    public PassRateStatistics GetPassRateStatistics(DateTime startDate, DateTime endDate, int? taskId = null)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                var sql = @"
SELECT
    COUNT(*) as Total,
    SUM(CASE WHEN IsOK = 1 THEN 1 ELSE 0 END) as OkCount,
    SUM(CASE WHEN IsOK = 0 THEN 1 ELSE 0 END) as NgCount
FROM DetectionRecords
WHERE DetectionTime >= @StartDate AND DetectionTime <= @EndDate AND IsDeleted = 0";
                if (taskId.HasValue) sql += " AND TaskId = @TaskId";

                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
                if (taskId.HasValue) cmd.Parameters.AddWithValue("@TaskId", taskId.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        var total = reader.GetInt32(0);
                        var ok = reader.GetInt32(1);
                        var ng = reader.GetInt32(2);
                        return new PassRateStatistics
                        {
                            TotalCount = total,
                            OkCount = ok,
                            NgCount = ng,
                            PassRate = total > 0 ? Math.Round((double)ok / total * 100, 2) : 0,
                            MinPassRate = total > 0 ? Math.Round((double)ok / total * 100, 2) : 0,
                            MaxPassRate = total > 0 ? Math.Round((double)ok / total * 100, 2) : 0
                        };
                    }
                }
                return new PassRateStatistics();
            }
        }
    }

    public IEnumerable<DailyPassRateTrend> GetDailyPassRateTrend(DateTime startDate, DateTime endDate, int? taskId = null)
    {
        var result = new List<DailyPassRateTrend>();
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                var sql = @"
SELECT
    DATE(DetectionTime) as DetDate,
    COUNT(*) as Total,
    SUM(CASE WHEN IsOK = 1 THEN 1 ELSE 0 END) as Ok,
    SUM(CASE WHEN IsOK = 0 THEN 1 ELSE 0 END) as Ng
FROM DetectionRecords
WHERE DetectionTime >= @StartDate AND DetectionTime <= @EndDate AND IsDeleted = 0";
                if (taskId.HasValue) sql += " AND TaskId = @TaskId";
                sql += " GROUP BY DATE(DetectionTime) ORDER BY DetDate";

                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
                if (taskId.HasValue) cmd.Parameters.AddWithValue("@TaskId", taskId.Value);

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var date = DateTime.Parse(reader.GetString(0));
                        var total = reader.GetInt32(1);
                        var ok = reader.GetInt32(2);
                        var ng = reader.GetInt32(3);
                        result.Add(new DailyPassRateTrend
                        {
                            Date = date,
                            Total = total,
                            Ok = ok,
                            Ng = ng,
                            PassRate = total > 0 ? Math.Round((double)ok / total * 100, 2) : 0
                        });
                    }
                }
            }
        }
        return result;
    }

    public IEnumerable<ProductDetectionStatistics> GetProductStatistics(DateTime startDate, DateTime endDate)
    {
        var result = new List<ProductDetectionStatistics>();
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                cmd.CommandText = @"
SELECT
    ProductId, ProductName, ProductCode,
    COUNT(*) as Total,
    SUM(CASE WHEN IsOK = 1 THEN 1 ELSE 0 END) as OkCount,
    SUM(CASE WHEN IsOK = 0 THEN 1 ELSE 0 END) as NgCount
FROM DetectionRecords
WHERE DetectionTime >= @StartDate AND DetectionTime <= @EndDate AND IsDeleted = 0
GROUP BY ProductId, ProductName, ProductCode
ORDER BY Total DESC";
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var total = reader.GetInt32(3);
                        var ok = reader.GetInt32(4);
                        result.Add(new ProductDetectionStatistics
                        {
                            ProductId = reader.GetInt32(0),
                            ProductName = reader.GetString(1),
                            ProductCode = reader.GetString(2),
                            TotalDetections = total,
                            OkCount = ok,
                            NgCount = reader.GetInt32(5),
                            PassRate = total > 0 ? Math.Round((double)ok / total * 100, 2) : 0
                        });
                    }
                }
            }
        }
        return result;
    }

    public DetectionTimeStatistics GetDetectionTimeStatistics(DateTime startDate, DateTime endDate, int? taskId = null)
    {
        using (var conn = new SQLiteConnection(_connectionString))
        {
            conn.Open();
            using (var cmd = conn.CreateCommand())
            {
                var sql = @"
SELECT
    AVG(ElapsedMs) as AvgMs,
    MIN(ElapsedMs) as MinMs,
    MAX(ElapsedMs) as MaxMs,
    TaskName
FROM DetectionRecords
WHERE DetectionTime >= @StartDate AND DetectionTime <= @EndDate AND IsDeleted = 0";
                if (taskId.HasValue) sql += " AND TaskId = @TaskId";
                sql += " GROUP BY TaskName";

                cmd.CommandText = sql;
                cmd.Parameters.AddWithValue("@StartDate", startDate.ToString("yyyy-MM-dd HH:mm:ss"));
                cmd.Parameters.AddWithValue("@EndDate", endDate.ToString("yyyy-MM-dd HH:mm:ss"));
                if (taskId.HasValue) cmd.Parameters.AddWithValue("@TaskId", taskId.Value);

                double avgTotal = 0, minTotal = double.MaxValue, maxTotal = 0;
                var byTask = new Dictionary<string, double>();

                using (var reader = cmd.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        var avg = reader.IsDBNull(0) ? 0 : reader.GetDouble(0);
                        var min = reader.IsDBNull(1) ? 0 : reader.GetDouble(1);
                        var max = reader.IsDBNull(2) ? 0 : reader.GetDouble(2);
                        var taskName = reader.IsDBNull(3) ? "Unknown" : reader.GetString(3);

                        avgTotal += avg;
                        if (min < minTotal) minTotal = min;
                        if (max > maxTotal) maxTotal = max;
                        byTask[taskName] = avg;
                    }
                }

                return new DetectionTimeStatistics
                {
                    AverageElapsedMs = avgTotal,
                    MinElapsedMs = minTotal == double.MaxValue ? 0 : minTotal,
                    MaxElapsedMs = maxTotal,
                    ByTaskType = byTask
                };
            }
        }
    }
}
}