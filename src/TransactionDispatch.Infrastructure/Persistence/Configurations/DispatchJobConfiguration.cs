using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using TransactionDispatch.Domain.Models;

namespace TransactionDispatch.Infrastructure.Persistence.Configurations
{
    public class DispatchJobConfiguration : IEntityTypeConfiguration<DispatchJob>
    {
        public void Configure(EntityTypeBuilder<DispatchJob> builder)
        {
            builder.ToTable("DispatchJobs");

            builder.Ignore(j => j.Files);

            builder.HasKey(j => j.JobId);

            builder.Property(j => j.FolderPath)
                   .HasMaxLength(1000)
                   .IsRequired();

            builder.Property(j => j.StartedAt)
                   .IsRequired();

            builder.Property(j => j.CompletedAt)
                   .IsRequired(false);

            builder.Property(j => j.TotalFiles)
                   .IsRequired();

            builder.Property(j => j.Processed)
                   .IsRequired();

            builder.Property(j => j.Successful)
                   .IsRequired();

            builder.Property(j => j.Failed)
                   .IsRequired();

            builder.Property(j => j.Status)
                   .HasConversion<string>() // store enum as string
                   .HasMaxLength(50)
                   .IsRequired();

            builder.Property(j => j.LastError)
                   .HasMaxLength(2000)
                   .IsRequired(false);
        }
    }
}
