using TripleDetection.Domain.Entities;

namespace TripleDetection.Domain.Repositories;

public interface IDetectionRecordRepository : IRepository<DetectionRecord>
{
    IPagedResult<DetectionRecord> Query(DetectionRecordQuery query);
    IEnumerable<DetectionRecord> Export(DetectionRecordQuery query);
}