using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

namespace BuildingProposalSystem.Services.Implementations
{
    public class UserManagementService : IUserManagementService
    {
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<UserManagementService> _logger;

        public UserManagementService(
            UserManager<ApplicationUser> userManager,
            ILogger<UserManagementService> logger)
        {
            _userManager = userManager;
            _logger = logger;
        }

        public async Task<List<UserListItemViewModel>> GetAllUsersAsync()
        {
            var users = await _userManager
                .Users
                .OrderBy (x => x.FullName)
                .ThenBy(x => x.Email)
                .ToListAsync();
            var result = new List<UserListItemViewModel>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                result.Add(new UserListItemViewModel
                {
                    Id = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    UserName = user.UserName ?? string.Empty,
                    IsActive = user.IsActive,
                    Roles = roles
                });
            }

            return result;
        }

        public async Task<IdentityResult> CreateUserAsync(UserCreateViewModel model)
        {
            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
            {
                await _userManager.AddToRoleAsync(user, model.Role);
                _logger.LogInformation("Admin membuat user baru: {Email}, Role: {Role}", user.Email, model.Role);
            }

            return result;
        }

        public async Task<UserEditViewModel?> GetUserForEditAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return null;
            }

            var roles = await _userManager.GetRolesAsync(user);

            return new UserEditViewModel
            {
                Id = user.Id,
                FullName = user.FullName,
                Email = user.Email ?? string.Empty,
                Role = roles.FirstOrDefault() ?? string.Empty,
                IsActive = user.IsActive
            };
        }

        public async Task<IdentityResult> UpdateUserAsync(UserEditViewModel model)
        {
            var user = await _userManager.FindByIdAsync(model.Id);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User tidak ditemukan." });
            }

            user.FullName = model.FullName;
            user.UpdatedDate = DateTime.UtcNow;

            await _userManager.SetEmailAsync(user, model.Email);

            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                return updateResult;
            }

            // Ganti Role: hapus semua role lama, assign role baru.
            var currentRoles = await _userManager.GetRolesAsync(user);
            await _userManager.RemoveFromRolesAsync(user, currentRoles);
            await _userManager.AddToRoleAsync(user, model.Role);

            _logger.LogInformation("Admin mengubah data user: {Email}", user.Email);

            return IdentityResult.Success;
        }

        public async Task<IdentityResult> ToggleActiveStatusAsync(string userId)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User tidak ditemukan." });
            }

            user.IsActive = !user.IsActive;
            user.UpdatedDate = DateTime.UtcNow;

            var result = await _userManager.UpdateAsync(user);

            _logger.LogInformation("Admin mengubah status user {Email} menjadi IsActive={IsActive}", user.Email, user.IsActive);

            return result;
        }

        public async Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword)
        {
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null)
            {
                return IdentityResult.Failed(new IdentityError { Description = "User tidak ditemukan." });
            }

            var removeResult = await _userManager.RemovePasswordAsync(user);
            if (!removeResult.Succeeded)
            {
                return removeResult;
            }

            var addResult = await _userManager.AddPasswordAsync(user, newPassword);

            if (addResult.Succeeded)
            {
                _logger.LogInformation("Admin mereset password untuk user: {Email}", user.Email);
            }

            return addResult;
        }
    }
}