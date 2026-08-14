using BuildingProposalSystem.Models.Enums;

namespace BuildingProposalSystem.Models.Entities
{
    public class ApprovalHistory
    {
        public Guid Id { get; set; }

        public Guid ProposalId { get; set; }

        public ApprovalLevel ApprovalLevel { get; set; }

        public string Action { get; set; } = string.Empty;

        public string? Comment { get; set; }

        public string ApprovedBy { get; set; } = string.Empty;

        public DateTime ActionDate { get; set; }

        public BuildingProposal? Proposal { get; set; }

        public ApplicationUser? Approver { get; set; }
    }
}