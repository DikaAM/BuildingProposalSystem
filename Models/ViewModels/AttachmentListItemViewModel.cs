using System;

namespace BuildingProposalSystem.Models.ViewModels
{
    public class AttachmentListItemViewModel
    {
        public Guid Id { get; set; }

        public string OriginalFileName { get; set; } = string.Empty;

        public long FileSize { get; set; }

        public string FileExtension { get; set; } = string.Empty;

        public string ContentType { get; set; } = string.Empty;

        public DateTime UploadedDate { get; set; }

        public string UploadedBy { get; set; } = string.Empty;
    }
}