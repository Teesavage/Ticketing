
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;


namespace Ticketing.Infrastructure
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // Look for configuration files in the API project directory
            var apiProjectPath = Path.Combine(Directory.GetCurrentDirectory(), "..", "Ticketing.Api");
            
            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: true)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();

            // Get connection string from the API project configuration
            var connectionString = configuration.GetConnectionString("DefaultConnection");
            
            if (string.IsNullOrEmpty(connectionString))
            {
                throw new InvalidOperationException(
                    "No connection string found. Please ensure appsettings.json exists in Ticketing.Api with a DefaultConnection.");
            }

            optionsBuilder.UseSqlServer(connectionString);

            // Return context for design-time operations (migrations, etc.)
            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
} 