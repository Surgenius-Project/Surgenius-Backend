using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Surgenius.Domain.Models;

namespace Surgenius.Infrastructure.Configurations
{
    public class AnalysisResultConfiguration : IEntityTypeConfiguration<AnalysisResult>
    {
        public void Configure(EntityTypeBuilder<AnalysisResult> builder)
        {
            builder.HasKey(a => a.Id);

            builder.Property(a => a.Confidence)
                .HasPrecision(18, 2);
        }
    }
}

