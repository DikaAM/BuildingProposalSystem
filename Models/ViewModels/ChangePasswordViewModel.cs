using System.ComponentModel.DataAnnotations;

namespace BuildingProposalSystem.Models.ViewModels
{
    public class ChangePasswordViewModel
    {
        [Required(ErrorMessage = "Password lama wajib diisi.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Lama")]
        public string OldPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi.")]
        [DataType(DataType.Password)]
        [Display(Name = "Password Baru")]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi password tidak cocok.")]
        [Display(Name = "Konfirmasi Password Baru")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}