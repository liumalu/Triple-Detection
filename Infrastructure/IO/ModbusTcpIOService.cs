using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using Modbus;
using Modbus.Device;
using TripleDetection.Domain.Repositories;

namespace TripleDetection.Infrastructure.IO
{
    public class ModbusTcpIOService : IIODeviceService, IDisposable
    {
        private TcpClient _tcpClient;
        private ModbusIpMaster _master;
        private readonly Application.Services.LoggingService _logService;
        private bool _isConnected = false;
        private readonly object _connLock = new object();

        public bool IsConnected
        {
            get { lock (_connLock) return _isConnected; }
        }

        public ModbusTcpIOService(Application.Services.LoggingService logService)
        {
            _logService = logService;
        }

        public async Task ConnectAsync(string ip, int port, CancellationToken ct = default)
        {
            lock (_connLock)
            {
                if (_isConnected) return;
                _tcpClient?.Dispose();
                _tcpClient = new TcpClient();
            }

            await _tcpClient.ConnectAsync(ip, port);
            _master = ModbusIpMaster.CreateIp(_tcpClient);
            lock (_connLock) { _isConnected = true; }
            _logService.Log($"[ModbusTCP] 已连接 {ip}:{port}");
        }

        public async Task WriteCoilAsync(int coilAddress, bool value, CancellationToken ct = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("ModbusTCP 未连接，请先调用 ConnectAsync");

            const int maxRetries = 3;
            Exception lastEx = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    // NModbus4: WriteSingleCoilAsync(byte slaveAddress, ushort coilAddress, ushort coilValue)
                    // Modbus convention: 0xFF00 = ON, 0x0000 = OFF
                    // Try bool parameter
                    await _master.WriteSingleCoilAsync(1, (ushort)(coilAddress - 1), value);
                    return;
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    lock (_connLock) { _isConnected = false; }

                    if (attempt < maxRetries)
                    {
                        _logService.Log($"[ModbusTCP] WriteCoil 第 {attempt} 次失败，100ms 后重试...");
                        await Task.Delay(100, ct);
                    }
                }
            }

            _logService.Log($"[ModbusTCP] WriteCoil 最终失败（已重试 {maxRetries} 次）: {lastEx?.Message}");
            throw lastEx;
        }

        public async Task<bool> ReadDiscreteInputAsync(int inputAddress, CancellationToken ct = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("ModbusTCP 未连接");

            const int maxRetries = 3;
            Exception lastEx = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    bool[] result = await _master.ReadInputsAsync(1, (ushort)(inputAddress - 1), 1);
                    return result[0];
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    lock (_connLock) { _isConnected = false; }

                    if (attempt < maxRetries)
                    {
                        _logService.Log($"[ModbusTCP] ReadInput 第 {attempt} 次失败，100ms 后重试...");
                        await Task.Delay(100, ct);
                    }
                }
            }

            _logService.Log($"[ModbusTCP] ReadInput 最终失败（已重试 {maxRetries} 次）: {lastEx?.Message}");
            throw lastEx;
        }

        public async Task<bool[]> ReadDiscreteInputsAsync(int startAddress, int count, CancellationToken ct = default)
        {
            if (!IsConnected)
                throw new InvalidOperationException("ModbusTCP 未连接");

            const int maxRetries = 3;
            Exception lastEx = null;

            for (int attempt = 1; attempt <= maxRetries; attempt++)
            {
                try
                {
                    return await _master.ReadInputsAsync(1, (ushort)(startAddress - 1), (ushort)count);
                }
                catch (Exception ex)
                {
                    lastEx = ex;
                    lock (_connLock) { _isConnected = false; }

                    if (attempt < maxRetries)
                    {
                        _logService.Log($"[ModbusTCP] ReadInputs 第 {attempt} 次失败，100ms 后重试...");
                        await Task.Delay(100, ct);
                    }
                }
            }

            _logService.Log($"[ModbusTCP] ReadInputs 最终失败（已重试 {maxRetries} 次）: {lastEx?.Message}");
            throw lastEx;
        }

        public void Disconnect()
        {
            lock (_connLock)
            {
                _isConnected = false;
                _master?.Dispose();
                _tcpClient?.Close();
                _master = null;
                _tcpClient = null;
            }
            _logService.Log("[ModbusTCP] 连接已断开");
        }

        public void Dispose()
        {
            Disconnect();
        }
    }
}