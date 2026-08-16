using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using PMS.Models.Interfaces;

namespace PMS.Repository.Data;

/// <summary>
/// Design-time DbContext factory for EF Core migrations.
/// Solves the problem of multiple constructors by providing a factory method
/// that creates the DbContext with the design-time constructor (without tenant provider).
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use the connection string from environment or hardcoded for migrations
        var connectionString = Environment.GetEnvironmentVariable("DB_CONNECTION_STRING")
            ?? "Host=ep-fragrant-queen-axzmnpkx-pooler.c-4.us-east-2.aws.neon.tech;Port=5432;Database=neondb;Username=neondb_owner;Password=npg_JEAB8rp7nHXw;SSL Mode=Require;Channel Binding=Require";

        optionsBuilder.UseNpgsql(connectionString);

        // Return using the design-time constructor (no tenant provider)
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
