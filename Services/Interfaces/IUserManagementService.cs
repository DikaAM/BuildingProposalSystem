using BuildingProposalSystem.Models.ViewModels;
using Microsoft.AspNetCore.Identity;

namespace BuildingProposalSystem.Services.Interfaces
{
    public interface IUserManagementService
    {
        Task<List<UserListItemViewModel>> GetAllUsersAsync();
        Task<IdentityResult> CreateUserAsync(UserCreateViewModel model);
        Task<UserEditViewModel?> GetUserForEditAsync(string userId);
        Task<IdentityResult> UpdateUserAsync(UserEditViewModel model);
        Task<IdentityResult> ToggleActiveStatusAsync(string userId);
        Task<IdentityResult> ResetPasswordAsync(string userId, string newPassword);
    }
}