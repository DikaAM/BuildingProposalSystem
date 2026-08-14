using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace BuildingProposalSystem.Models.ViewModels
{
    public class ProposalEditViewModel
    {
        public Guid Id { get; set; }


        [Display(Name = "Nomor Proposal")]
        public string ProposalNumber { get; set; } = string.Empty;


        [Display(Name = "Tanggal Pengajuan")]
        [DataType(DataType.Date)]
        public DateTime? ProposalDate { get; set; }


        [Display(Name = "Nama Gedung")]
        public string? BuildingName { get; set; }


        [Display(Name = "Alamat")]
        public string? Address { get; set; }


        [Display(Name = "Latitude")]
        [Range(-90, 90, ErrorMessage = "Latitude harus berada antara -90 sampai 90.")]
        public decimal? Latitude { get; set; }


        [Display(Name = "Longitude")]
        [Range(-180, 180, ErrorMessage = "Longitude harus berada antara -180 sampai 180.")]
        public decimal? Longitude { get; set; }


        [Display(Name = "Estimasi Biaya")]
        public decimal? EstimatedCost { get; set; }


        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }


        [Display(Name = "Status")]
        public string Status { get; set; } = string.Empty;


        [Display(Name = "Proposal PDF")]
        public IFormFile? ProposalFile { get; set; }


            public List<AttachmentListItemViewModel> Attachments { get; set; }
            = new();


        public string Action { get; set; } = "Draft";

        public bool CanEdit { get; set; }

        public bool CanApprove { get; set; }

        public bool IsRejected { get; set; }

        public string? ApprovalComment { get; set; }

        public string? RejectionReason { get; set; }
    }
}