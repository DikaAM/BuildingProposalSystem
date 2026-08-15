using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.ViewModels;

namespace BuildingProposalSystem.Services.Interfaces
{
    public interface IProposalService
    {
        Task<Guid> CreateDraftAsync(
            ProposalCreateViewModel model,
            string userId);

        Task<List<ProposalListItemViewModel>> GetAllProposalsAsync();

        Task<ProposalEditViewModel?> GetProposalForEditAsync(
            Guid id,
            string userId);

        Task<bool> UpdateDraftAsync(
            ProposalEditViewModel model,
            string userId);

        Task<bool> SubmitDraftAsync(
            ProposalEditViewModel model,
            string userId);

        Task<ProposalEditViewModel?> GetProposalForApprovalAsync(
            Guid id,
            string userId,
            string userRole);

        Task<bool> ApproveAsync(
            Guid id,
            string userId,
            string userRole,
            string? comment);

        Task<bool> RejectAsync(
            Guid id,
            string userId,
            string userRole,
            string? comment);

        Task<ProposalAttachment?> GetAttachmentAsync(Guid id);
    }
}