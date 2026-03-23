using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Surgenius.Domain.Models;

namespace Surgenius.Infrastructure.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        
        builder.HasOne(u => u.Doctor)
               .WithMany(u => u.Students)
               .HasForeignKey(u => u.DoctorId)
               .OnDelete(DeleteBehavior.Restrict);

        
        builder.HasMany(u => u.Cases)
               .WithOne(c => c.User)
               .HasForeignKey(c => c.UserId);

       
    }
}
