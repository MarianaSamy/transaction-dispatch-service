using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Domain.Models;
using TransactionDispatch.Infrastructure.Persistence;

namespace TransactionDispatch.Infrastructure.Repositories
{
    public class SqlServerJobRepository : IJobRepository
    {
        private readonly DispatchDbContext _context;

        public SqlServerJobRepository(DispatchDbContext context)
        {
            _context = context;
        }

        public async Task AddAsync(DispatchJob job, CancellationToken cancellationToken = default)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            _context.DispatchJobs.Add(job);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<DispatchJob?> GetByIdAsync(Guid jobId, CancellationToken cancellationToken = default)
        {
            return await _context.DispatchJobs
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.JobId == jobId, cancellationToken);
        }

        public async Task UpdateAsync(DispatchJob job, CancellationToken cancellationToken = default)
        {
            if (job == null) throw new ArgumentNullException(nameof(job));
            _context.DispatchJobs.Update(job);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<IReadOnlyList<DispatchJob>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            return await _context.DispatchJobs
                .AsNoTracking()
                .OrderByDescending(x => x.StartedAt)
                .ToListAsync(cancellationToken);
        }
    }
}
