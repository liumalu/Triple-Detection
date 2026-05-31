using TripleDetection.Domain.Entities;

namespace TripleDetection.Application.Services;

public interface IDetectionRecordService
{
    void Save(DetectionRecord record);
    IPagedResult<DetectionRecord> Query(DetectionRecordQuery query);
    IEnumerable<DetectionRecord> Export(DetectionRecordQuery query);
}