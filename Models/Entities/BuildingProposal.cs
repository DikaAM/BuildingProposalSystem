using BuildingProposalSystem.Models.Enums;

namespace BuildingProposalSystem.Models.Entities
{
    public class BuildingProposal
    {
        public Guid Id { get; set; }
        public string ProposalNumber { get; set; } = string.Empty;
        public DateTime ProposalDate { get; set; }
        public string BuildingName { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public decimal Latitude { get; set; }
        public decimal Longitude { get; set; }
        public decimal EstimatedCost { get; set; }
        public string? Description { get; set; }
        public ProposalStatus Status { get; set; }
        public ApprovalLevel CurrentApproverRole { get; set; }

        public string CreatedBy { get; set; } = string.Empty;
        public DateTime CreatedDate { get; set; }
        public string? UpdatedBy { get; set; }
        public DateTime? UpdatedDate { get; set; }

        public ApplicationUser? Creator { get; set; }
        public ApplicationUser? Updater { get; set; }
        public ICollection<ProposalAttachment> Attachments { get; set; } = new List<ProposalAttachment>();
    }
}