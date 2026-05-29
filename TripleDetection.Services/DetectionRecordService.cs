using System;
using System.Collections.Generic;
using TripleDetection.Data.Entities;
using TripleDetection.Data.Repositories;

namespace TripleDetection.Services
{
    public interface IDetectionRecordService
    {
        void Save(DetectionRecord record);
        IPagedResult<DetectionRecord> Query(DetectionRecordQuery query);
        IEnumerable<DetectionRecord> Export(DetectionRecordQuery query);
    }

    public class DetectionRecordService : IDetectionRecordService
    {
        private readonly IDetectionRecordRepository _repository;

        public DetectionRecordService(IDetectionRecordRepository repository)
        {
            _repository = repository;
        }

        public void Save(DetectionRecord record)
        {
            _repository.Add(record);
        }

        public IPagedResult<DetectionRecord> Query(DetectionRecordQuery query)
        {
            return _repository.Query(query);
        }

        public IEnumerable<DetectionRecord> Export(DetectionRecordQuery query)
        {
            return _repository.Export(query);
        }
    }
}