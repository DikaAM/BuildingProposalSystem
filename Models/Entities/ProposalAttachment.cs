namespace BuildingProposalSystem.Models.Entities
{
    public class ProposalAttachment
    {
        public Guid Id { get; set; }
        public Guid ProposalId { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string StoredFileName { get; set; } = string.Empty;
        public string FileExtension { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public string UploadedBy { get; set; } = string.Empty;
        public DateTime UploadedDate { get; set; }

        public BuildingProposal? Proposal { get; set; }
        public ApplicationUser? Uploader { get; set; }
    }
}