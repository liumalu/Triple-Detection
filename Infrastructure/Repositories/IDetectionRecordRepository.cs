using System.Collections.Generic;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Entities.Queries;

namespace TripleDetection.Domain.Repositories;

public interface IDetectionRecordRepository : IRepository<DetectionRecord>
{
    IPagedResult<DetectionRecord> Query(DetectionRecordQuery query);
    IEnumerable<DetectionRecord> Export(DetectionRecordQuery query);
}