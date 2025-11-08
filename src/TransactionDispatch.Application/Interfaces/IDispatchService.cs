using System;
using System.Threading;
using System.Threading.Tasks;
using TransactionDispatch.Application.DTOs;

namespace TransactionDispatch.Application.Ports
{
    public interface IDispatchService
    {
        Task<Guid> StartDispatchAsync(DispatchJobRequestDto request, CancellationToken cancellationToken = default);
        Task<JobStatusDto?> GetStatusAsync(Guid jobId, CancellationToken cancellationToken = default);
    }
}
