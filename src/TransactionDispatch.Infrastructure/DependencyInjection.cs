using System;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TransactionDispatch.Application.Interfaces;
using TransactionDispatch.Infrastructure.Persistence;
using TransactionDispatch.Infrastructure.Repositories;

namespace TransactionDispatch.Infrastructure
{
    public static class DependencyInjection
    {
        public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            var connectionString = configuration.GetConnectionString("DispatchDatabase")
                                   ?? throw new InvalidOperationException("Connection string 'DispatchDatabase' not found.");

            services.AddDbContext<DispatchDbContext>(options =>
                options.UseSqlServer(connectionString, sqlOptions => sqlOptions.EnableRetryOnFailure()));

            // Repository
            services.AddScoped<IJobRepository, SqlServerJobRepository>();

            return services;
        }
    }
}
