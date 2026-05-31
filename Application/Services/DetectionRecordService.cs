using System.Collections.Generic;
using TripleDetection.Domain.Entities;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Application.Services;

public class DetectionRecordService : IDetectionRecordService
{
    private readonly IDetectionRecordRepository _repository;

    public DetectionRecordService(IDetectionRecordRepository repository)
    {
        _repository = repository;
    }

    public void Save(DetectionRecord record) => _repository.Add(record);
    public IPagedResult<DetectionRecord> Query(DetectionRecordQuery query) => _repository.Query(query);
    public IEnumerable<DetectionRecord> Export(DetectionRecordQuery query) => _repository.Export(query);
}