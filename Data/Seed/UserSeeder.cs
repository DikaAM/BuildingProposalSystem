using Microsoft.AspNetCore.Identity;
using BuildingProposalSystem.Models.Entities;


namespace BuildingProposalSystem.Data.Seed
{
    public static class UserSeeder
    {
        public static async Task SeedUsersAsync(IServiceProvider serviceProvider)
        {
            var userManager = serviceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var seedUsers = new (string Email, string FullName, string Role)[]
            {
                ("admin@company.com", "Administrator", "Admin"),
                ("staff@company.com", "Staff User", "Staff"),
                ("manager@company.com", "Manager User", "Manager"),
                ("director@company.com", "Director User", "Director"),
            };

            const string defaultPassword = "Password123!";

            foreach (var seed in seedUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(seed.Email);
                if (existingUser != null)
                {
                    continue; // Skip jika user udah ada
                }

                var user = new ApplicationUser
                {
                    UserName = seed.Email,
                    Email = seed.Email,
                    FullName = seed.FullName,
                    IsActive = true,
                    CreatedDate = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                var result = await userManager.CreateAsync(user, defaultPassword);

                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(user, seed.Role);

                }
                else
                {
                    // Handle error jika pembuatan user gagal
                    var errors = string.Join(", ", result.Errors.Select(e => e.Description));
                    Console.WriteLine($"Gagal membuat user {seed.Email}: {errors}");
                }

            }
        }
    }

}
