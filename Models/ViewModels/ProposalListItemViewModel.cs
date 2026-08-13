namespace BuildingProposalSystem.Models.ViewModels
{
    public class ProposalListItemViewModel
    {
        public Guid Id { get; set; }

        public string ProposalNumber { get; set; } = string.Empty;

        public DateTime ProposalDate { get; set; }

        public string BuildingName { get; set; } = string.Empty;

        public decimal EstimatedCost { get; set; }

        public string Status { get; set; } = string.Empty;

        public DateTime CreatedDate { get; set; }
    }
}