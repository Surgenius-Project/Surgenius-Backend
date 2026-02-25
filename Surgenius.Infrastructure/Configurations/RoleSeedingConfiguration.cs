using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.AspNetCore.Identity;

namespace Surgenius.Infrastructure.Configurations;

public class RoleSeedingConfiguration : IEntityTypeConfiguration<IdentityRole<Guid>>
{
    public void Configure(EntityTypeBuilder<IdentityRole<Guid>> builder)
    {
        builder.HasData(
            new IdentityRole<Guid> { Id = new Guid("468F64C8-5CC1-4C5A-870F-A9FDBFC98305"), Name = "Admin", NormalizedName = "ADMIN" },
            new IdentityRole<Guid> { Id = new Guid("2CBAA562-EA8B-4DC1-BD61-A0E0B9156553"), Name = "Doctor", NormalizedName = "DOCTOR" },
            new IdentityRole<Guid> { Id = new Guid("1392A877-66F5-4E7F-9596-E8B83AB843B3"), Name = "Student", NormalizedName = "STUDENT" }
        );
    }
}
