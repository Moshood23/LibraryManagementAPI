using LibraryManagementAPI.Data;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace LibraryManagementAPI.Tests;

/// <summary>Runs the real app with an in-memory database instead of SQL Server.</summary>
public class LibraryApiFactory : WebApplicationFactory<Program>
{
    public const string AdminEmail = "admin@test.local";
    public const string AdminPassword = "Admin@123";

    private readonly string _dbName = $"library-tests-{Guid.NewGuid()}";

    /// <summary>The app's clock. Tests can move it forward to make loans overdue.</summary>
    public TestClock Clock { get; } = new();

    /// <summary>Kept low so the limit is quick to hit in tests.</summary>
    public const int MaxActiveLoansPerUser = 3;

    public const string AllowedOrigin = "https://app.example.test";

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("JWT:Secret", "test-secret-key-that-is-at-least-32-bytes-long!");
        builder.UseSetting("SeedAdmin:Email", AdminEmail);
        builder.UseSetting("SeedAdmin:Password", AdminPassword);
        builder.UseSetting("Loans:LoanPeriodDays", "14");
        builder.UseSetting("Loans:MaxActiveLoansPerUser", MaxActiveLoansPerUser.ToString());
        // Tests log in constantly from the same (empty) IP, so lift the limit; RateLimitingTests lowers it again.
        builder.UseSetting("RateLimiting:AuthPermitLimit", "100000");
        builder.UseSetting("Cors:AllowedOrigins:0", AllowedOrigin);

        builder.ConfigureServices(services =>
        {
            var toRemove = services
                .Where(d => d.ServiceType == typeof(DbContextOptions<LibraryContext>)
                         || d.ServiceType == typeof(DbContextOptions)
                         || (d.ServiceType.IsGenericType &&
                             d.ServiceType.GetGenericTypeDefinition().Name.StartsWith("IDbContextOptionsConfiguration")))
                .ToList();
            foreach (var d in toRemove)
                services.Remove(d);

            services.AddDbContext<LibraryContext>(o => o.UseInMemoryDatabase(_dbName));
            services.AddSingleton<TimeProvider>(Clock);
        });
    }
}

/// <summary>A clock that starts at the real time and only moves when a test advances it.</summary>
public class TestClock : TimeProvider
{
    private DateTimeOffset _now = DateTimeOffset.UtcNow;

    public override DateTimeOffset GetUtcNow() => _now;

    public void Advance(TimeSpan by) => _now = _now.Add(by);
}
