using InternalChat.Domain.Entities;
using InternalChat.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using BCrypt.Net;

namespace InternalChat.Infrastructure.Persistence;

public static class AppDbSeeder
{
    public static async Task SeedAsync(AppDbContext context)
    {
        await context.Database.MigrateAsync();

        if (!await context.Users.AnyAsync(u => u.Role == UserRole.Admin))
        {
            var admin = new User
            {
                Id = Guid.NewGuid(),
                FullName = "System Admin",
                PhoneNumber = "+1234567890",
                PasswordHash = BCrypt.Net.BCrypt.HashPassword("AdminPassword123!"),
                Role = UserRole.Admin,
                Status = UserStatus.Active,
                CreatedAt = DateTime.UtcNow
            };
            
            await context.Users.AddAsync(admin);
            await context.SaveChangesAsync();
        }
    }
}
