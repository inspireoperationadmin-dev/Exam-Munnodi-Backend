using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace ScholarFlow.Infrastructure.Persistence;

/// <summary>
/// Used by EF Core design-time tools (dotnet ef migrations) only.
/// Not used at runtime — runtime uses DI.
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        var cwd = Directory.GetCurrentDirectory();
        var candidates = new[]
        {
            Path.Combine(cwd, "ScholarFlow.WebAPI"),
            Path.Combine(cwd, "../ScholarFlow.WebAPI"),
            cwd
        };
        var basePath = candidates.FirstOrDefault(p => File.Exists(Path.Combine(p, "appsettings.json")))
                       ?? cwd;

        var config = new ConfigurationBuilder()
            .SetBasePath(basePath)
            .AddJsonFile("appsettings.json", optional: false)
            .AddJsonFile("appsettings.Development.json", optional: true)
            .Build();

        var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        optionsBuilder.UseSqlServer(
            config.GetConnectionString("DefaultConnection"),
            b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));

        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
