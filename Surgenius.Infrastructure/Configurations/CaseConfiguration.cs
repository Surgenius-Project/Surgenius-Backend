using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Surgenius.Domain.Models;

namespace Surgenius.Infrastructure.Configurations
{
    public class CaseConfiguration : IEntityTypeConfiguration<Case>
    {
        public void Configure(EntityTypeBuilder<Case> builder)
        {
            builder.HasKey(c => c.Id);

            builder.Property(c => c.CaseType)
                .IsRequired();

            builder.Property(c => c.CreationDate)
                .IsRequired();

            builder.HasOne(c => c.User)
                .WithMany(u => u.Cases)
                .HasForeignKey(c => c.UserID)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}

