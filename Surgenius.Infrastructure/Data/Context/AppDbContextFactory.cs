using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace Surgenius.Infrastructure.Data.Context
{
    /// <summary>
    /// Used only by EF Core CLI tools (migrations, database updates) at design time.
    /// Locates the API project's appsettings.json by searching up the directory tree,
    /// so it works regardless of which folder the EF CLI is invoked from.
    /// </summary>
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            var apiProjectPath = FindApiProjectDirectory();

            var configuration = new ConfigurationBuilder()
                .SetBasePath(apiProjectPath)
                .AddJsonFile("appsettings.json", optional: false)
                .AddJsonFile("appsettings.Development.json", optional: true)
                .Build();

            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(configuration.GetConnectionString("DefaultConnection"));

            return new AppDbContext(optionsBuilder.Options);
        }

        /// <summary>
        /// Walks up from the current working directory until it finds a sibling folder
        /// named "Surgenius.Api" that contains an appsettings.json file.
        /// </summary>
        private static string FindApiProjectDirectory()
        {
            const string apiProjectName = "Surgenius.Api";
            const string settingsFile  = "appsettings.json";

            // Start at cwd and walk up
            var dir = new DirectoryInfo(Directory.GetCurrentDirectory());

            while (dir != null)
            {
                // Check siblings (or self) for the API project folder
                var candidate = Path.Combine(dir.FullName, apiProjectName, settingsFile);
                if (File.Exists(candidate))
                    return Path.Combine(dir.FullName, apiProjectName);

                dir = dir.Parent;
            }

            throw new DirectoryNotFoundException(
                $"Could not find '{apiProjectName}' folder with '{settingsFile}' " +
                $"while searching upward from '{Directory.GetCurrentDirectory()}'. " +
                "Make sure to run EF CLI commands from within the solution directory.");
        }
    }
}
