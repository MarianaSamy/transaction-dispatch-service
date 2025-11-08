using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TransactionDispatch.Domain.Models;

namespace TransactionDispatch.Application.Interfaces
{
    public interface IJobRepository
    {
        Task AddAsync(DispatchJob job, CancellationToken cancellationToken = default);
        Task<DispatchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default);
        Task UpdateAsync(DispatchJob job, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<DispatchJob>> GetAllAsync(CancellationToken cancellationToken = default);
    }
}
