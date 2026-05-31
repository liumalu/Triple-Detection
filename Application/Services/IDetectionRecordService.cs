using System.Collections.Generic;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;

namespace TripleDetection.Application.Services;

public interface IDetectionRecordService
{
    void Save(DetectionRecord record);
    IPagedResult<DetectionRecord> Query(DetectionRecordQuery query);
    IEnumerable<DetectionRecord> Export(DetectionRecordQuery query);
}