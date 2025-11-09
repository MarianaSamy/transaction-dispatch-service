using Microsoft.EntityFrameworkCore;
using TransactionDispatch.Domain.Models;
using TransactionDispatch.Infrastructure.Persistence.Configurations;

namespace TransactionDispatch.Infrastructure.Persistence
{
    public class DispatchDbContext : DbContext
    {
        public DispatchDbContext(DbContextOptions<DispatchDbContext> options)
            : base(options)
        {
        }

        public DbSet<DispatchJob> DispatchJobs => Set<DispatchJob>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply configuration for the DispatchJob domain entity
            modelBuilder.ApplyConfiguration(new DispatchJobConfiguration());
        }
    }
}
