using System;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.Services
{
    public interface IRejectService
    {
        void OnDetectionResultReceived(DetectionResult result);
        void ResetConsecutiveRejectCount();
        void ResetLineStop();
        int ConsecutiveRejectCount { get; }
        bool IsLineStopped { get; }
    }
}