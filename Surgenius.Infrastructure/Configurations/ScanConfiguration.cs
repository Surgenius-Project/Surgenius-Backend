using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Surgenius.Domain.Models;

namespace Surgenius.Infrastructure.Configurations
{
    public class ScanConfiguration : IEntityTypeConfiguration<Scan>
    {
        public void Configure(EntityTypeBuilder<Scan> builder)
        {
            builder.HasKey(s => s.Id);

            builder.Property(s => s.ScanPath)
                .IsRequired();

            builder.Property(s => s.ScanType)
                .IsRequired();

            builder.Property(s => s.UploadDate)
                .IsRequired();

            builder.HasOne(s => s.Case)
                .WithMany(c => c.Scans)
                .HasForeignKey(s => s.CaseId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.ThreeDModel)
                .WithOne(t => t.Scan)
                .HasForeignKey<ThreeDModel>(t => t.ScanId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(s => s.AnalysisResult)
                .WithOne(a => a.Scan)
                .HasForeignKey<AnalysisResult>(a => a.ScanId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}

