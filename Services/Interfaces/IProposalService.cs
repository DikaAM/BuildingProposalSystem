using BuildingProposalSystem.Models.ViewModels;

namespace BuildingProposalSystem.Services.Interfaces
{
    public interface IProposalService
    {
        Task<Guid> CreateDraftAsync(ProposalCreateViewModel model, string userId);
    }
}