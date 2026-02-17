using Microsoft.EntityFrameworkCore;
using Surgenius.Domain.Models;

namespace Surgenius.Infrastructure.Data.Context
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Case> Cases { get; set; }
        public DbSet<Scan> Scans { get; set; }
        public DbSet<AnalysisResult> AnalysisResults { get; set; }
        public DbSet<ThreeDModel> ThreeDModels { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);
        }
    }
}
