// IJobProcessor.cs (Application.Interfaces)
using System;
using System.Threading;
using System.Threading.Tasks;

namespace TransactionDispatch.Application.Interfaces
{
    public interface IJobProcessor
    {
        Task ProcessJobAsync(Guid jobId, CancellationToken cancellationToken = default);
    }
}
