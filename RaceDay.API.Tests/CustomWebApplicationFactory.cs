using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.VisualStudio.TestPlatform.TestHost;
using RaceDay.API.Data;
namespace RaceDay.API.Tests;
public class CustomWebApplicationFactory
    : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(
        IWebHostBuilder builder)
    {
        builder.UseEnvironment("Development");
        builder.ConfigureServices(services =>
        {
            // Remove the API's original database registration.
            services.RemoveAll<RaceDayDbContext>();
            services.RemoveAll<DbContextOptions<RaceDayDbContext>>();
            // Use an isolated in-memory database for testing.
            services.AddDbContext<RaceDayDbContext>(options =>
            {
                options.UseInMemoryDatabase(
                    $"RaceDayTests_{Guid.NewGuid()}");
            });
        });
    }
}