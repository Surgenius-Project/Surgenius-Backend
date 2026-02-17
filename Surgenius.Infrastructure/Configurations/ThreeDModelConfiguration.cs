using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Surgenius.Domain.Models;

namespace Surgenius.Infrastructure.Configurations
{
    public class ThreeDModelConfiguration : IEntityTypeConfiguration<ThreeDModel>
    {
        public void Configure(EntityTypeBuilder<ThreeDModel> builder)
        {
            builder.HasKey(t => t.Id);
        }
    }
}

