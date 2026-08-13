using System.ComponentModel.DataAnnotations;

namespace BuildingProposalSystem.Models.ViewModels
{
    public class ProposalCreateViewModel
    {
        [Required(ErrorMessage = "Tanggal wajib diisi.")]
        [Display(Name = "Tanggal Pengajuan")]
        [DataType(DataType.Date)]
        public DateTime ProposalDate { get; set; } = DateTime.Today;

        [Required(ErrorMessage = "Nama gedung wajib diisi.")]
        [Display(Name = "Nama Gedung")]
        public string BuildingName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Alamat wajib diisi.")]
        public string Address { get; set; } = string.Empty;

        [Required(ErrorMessage = "Estimasi biaya wajib diisi.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Biaya harus lebih besar dari nol.")] // BR-005
        [Display(Name = "Estimasi Biaya")]
        public decimal EstimatedCost { get; set; }

        [Display(Name = "Deskripsi")]
        public string? Description { get; set; }

    }
}