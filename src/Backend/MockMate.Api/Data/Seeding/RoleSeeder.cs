using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MockMate.Api.Constants;

namespace MockMate.Api.Data.Seeding;

public static class RoleSeeder
{
    public static void SeedRolesAndAdminAsync(this ModelBuilder builder)
    {
        builder
            .Entity<IdentityRole<int>>()
            .HasData(
                new IdentityRole<int>
                {
                    Id = 1,
                    Name = Roles.Admin,
                    NormalizedName = Roles.Admin.ToUpper(),
                },
                new IdentityRole<int>
                {
                    Id = 2,
                    Name = Roles.User,
                    NormalizedName = Roles.User.ToUpper(),
                }
            );
    }
}
