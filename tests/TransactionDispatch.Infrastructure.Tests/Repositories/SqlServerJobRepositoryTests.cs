using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TransactionDispatch.Domain.Models;
using TransactionDispatch.Infrastructure.Persistence;
using TransactionDispatch.Infrastructure.Repositories;
using Xunit;

namespace TransactionDispatch.Infrastructure.Tests.Repositories
{
    public class SqlServerJobRepositoryTests
    {
        private static SqlServerJobRepository CreateRepository(out DispatchDbContext ctx)
        {
            var opts = new DbContextOptionsBuilder<DispatchDbContext>()
                .UseInMemoryDatabase(Guid.NewGuid().ToString())
                .Options;

            ctx = new DispatchDbContext(opts);
            return new SqlServerJobRepository(ctx);
        }

        [Fact]
        public async Task AddAsync_And_GetByIdAsync_Works()
        {
            var repo = CreateRepository(out var ctx);
            var job = new DispatchJob("C:/data", totalFiles: 2);

            await repo.AddAsync(job);

            var found = await repo.GetByIdAsync(job.JobId);

            Assert.NotNull(found);
            Assert.Equal(job.JobId, found!.JobId);
            Assert.Equal("C:/data", found.FolderPath);
        }

        [Fact]
        public async Task UpdateAsync_PersistsChanges()
        {
            var repo = CreateRepository(out var ctx);
            var job = new DispatchJob("C:/data");
            await repo.AddAsync(job);

            job.Processed = 3;
            await repo.UpdateAsync(job);

            var updated = await repo.GetByIdAsync(job.JobId);
            Assert.Equal(3, updated!.Processed);
        }

        [Fact]
        public async Task GetAllAsync_ReturnsDescendingByStartedAt()
        {
            var repo = CreateRepository(out var ctx);
            var oldJob = new DispatchJob("C:/old") { };
            var newJob = new DispatchJob("C:/new") { };
            await repo.AddAsync(oldJob);
            await Task.Delay(5); // ensure different timestamps
            await repo.AddAsync(newJob);

            var all = await repo.GetAllAsync();

            Assert.Equal(2, all.Count);
            Assert.True(all.First().StartedAt >= all.Last().StartedAt);
        }

        [Fact]
        public async Task AddAsync_NullJob_Throws()
        {
            var repo = CreateRepository(out var ctx);
            await Assert.ThrowsAsync<ArgumentNullException>(() => repo.AddAsync(null!));
        }
    }
}
