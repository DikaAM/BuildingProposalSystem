using BuildingProposalSystem.Data;
using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.Enums;
using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;

namespace BuildingProposalSystem.Services.Implementations
{
    public class ProposalService : IProposalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IProposalNumberService _proposalNumberService;
        private readonly ILogger<ProposalService> _logger;

        public ProposalService(
            ApplicationDbContext context,
            IProposalNumberService proposalNumberService,
            ILogger<ProposalService> logger)
        {
            _context = context;
            _proposalNumberService = proposalNumberService;
            _logger = logger;
        }

        public async Task<Guid> CreateDraftAsync(ProposalCreateViewModel model, string userId)
        {
            var proposalNumber = await _proposalNumberService.GenerateNumberAsync();

            var proposal = new BuildingProposal
            {
                Id = Guid.NewGuid(),
                ProposalNumber = proposalNumber,
                ProposalDate = model.ProposalDate,
                BuildingName = model.BuildingName,
                Address = model.Address,
                EstimatedCost = model.EstimatedCost,
                Description = model.Description,
                Latitude = 0, // belum diisi - dilengkapi di task Map
                Longitude = 0,
                Status = ProposalStatus.Draft,
                CurrentApproverRole = ApprovalLevel.None,
                CreatedBy = userId,
                CreatedDate = DateTime.UtcNow
            };

            _context.BuildingProposals.Add(proposal);
            await _context.SaveChangesAsync();

            _logger.LogInformation("Draft proposal {ProposalNumber} dibuat oleh {UserId}", proposalNumber, userId);

            return proposal.Id;
        }
    }
}