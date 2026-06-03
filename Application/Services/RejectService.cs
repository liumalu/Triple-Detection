using System;
using System.Threading.Tasks;
using TripleDetection.Domain.Repositories;
using TripleDetection.Presentation.Models;

namespace TripleDetection.Application.Services
{
    public class RejectService : IRejectService
    {
        private readonly IIODeviceService _ioService;
        private readonly DeviceControlSettings _settings;
        private readonly LoggingService _logService;
        private int _consecutiveRejectCount = 0;
        private bool _isLineStopped = false;
        private readonly object _lock = new object();

        public int ConsecutiveRejectCount => _consecutiveRejectCount;
        public bool IsLineStopped => _isLineStopped;

        public RejectService(
            IIODeviceService ioService,
            DeviceControlSettings settings,
            LoggingService logService)
        {
            _ioService = ioService;
            _settings = settings;
            _logService = logService;
        }

        public void OnDetectionResultReceived(DetectionResult result)
        {
            if (result.IsOK)
            {
                lock (_lock)
                {
                    _consecutiveRejectCount = 0;
                    if (_isLineStopped)
                    {
                        _isLineStopped = false;
                        _logService.Log("[Reject] 产线恢复运行（收到OK）");
                    }
                }
                return;
            }

            lock (_lock)
            {
                _consecutiveRejectCount++;
                _logService.Log($"[Reject] NG #{_consecutiveRejectCount}");

                // 延迟后触发剔除脉冲（非阻塞）
                _ = Task.Delay(_settings.RejectDelayMs).ContinueWith(_ =>
                {
                    TriggerRejectPulse().Wait();
                });

                // 连续NG超过阈值，触发产线停止
                if (_settings.EnableLineStopOnConsecutiveRejects &&
                    _consecutiveRejectCount >= _settings.ConsecutiveRejectsToStopLine &&
                    !_isLineStopped)
                {
                    _isLineStopped = true;
                    _logService.Log($"[Reject] 连续NG达到 {_consecutiveRejectCount} 次，产线停止");
                    TriggerLineStop().Wait();
                }
            }
        }

        private async Task TriggerRejectPulse()
        {
            try
            {
                await _ioService.WriteCoilAsync(_settings.RejectCoilAddress, true);
                _logService.Log($"[Reject] 继电器吸合，地址={_settings.RejectCoilAddress}");
                await Task.Delay(_settings.RejectDurationMs);
                _logService.Log($"[Reject] 脉冲结束，宽度={_settings.RejectDurationMs}ms");
            }
            catch (Exception ex)
            {
                _logService.Log($"[Reject] 继电器控制异常: {ex.Message}");
            }
        }

        private async Task TriggerLineStop()
        {
            try
            {
                await _ioService.WriteCoilAsync(_settings.LineStopCoilAddress, true);
                _logService.Log($"[Reject] 产线停止继电器吸合，地址={_settings.LineStopCoilAddress}");
            }
            catch (Exception ex)
            {
                _logService.Log($"[Reject] 产线停止控制异常: {ex.Message}");
            }
        }

        public void ResetConsecutiveRejectCount()
        {
            lock (_lock)
            {
                _consecutiveRejectCount = 0;
            }
        }

        public void ResetLineStop()
        {
            lock (_lock)
            {
                if (!_isLineStopped) return;
                _isLineStopped = false;
                _consecutiveRejectCount = 0;
                _logService.Log("[Reject] 产线已手动复位，操作员确认恢复");
            }
        }
    }
}