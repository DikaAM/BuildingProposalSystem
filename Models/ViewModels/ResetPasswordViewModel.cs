using System.ComponentModel.DataAnnotations;

namespace BuildingProposalSystem.Models.ViewModels
{
    public class ResetPasswordViewModel
    {
        [Required]
        public string Email { get; set; } = string.Empty;

        [Required]
        public string Token { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password baru wajib diisi.")]
        [DataType(DataType.Password)]
        public string NewPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "Konfirmasi password wajib diisi.")]
        [DataType(DataType.Password)]
        [Compare(nameof(NewPassword), ErrorMessage = "Konfirmasi password tidak cocok.")]
        public string ConfirmPassword { get; set; } = string.Empty;
    }
}
