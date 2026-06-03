using System.Threading.Tasks;
using System.Threading;

namespace TripleDetection.Domain.Repositories
{
    public interface IIODeviceService
    {
        Task WriteCoilAsync(int coilAddress, bool value, CancellationToken ct = default);
        Task<bool> ReadDiscreteInputAsync(int inputAddress, CancellationToken ct = default);
        Task<bool[]> ReadDiscreteInputsAsync(int startAddress, int count, CancellationToken ct = default);
        bool IsConnected { get; }
        Task ConnectAsync(string ip, int port, CancellationToken ct = default);
        void Disconnect();
    }
}
