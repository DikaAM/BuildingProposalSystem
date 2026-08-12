using System.ComponentModel.DataAnnotations;

namespace BuildingProposalSystem.Models.ViewModels
{
    public class TwoFactorVerifyViewModel
    {
        [Required(ErrorMessage = "Kode wajib diisi.")]
        [Display(Name = "Kode dari Authenticator App")]
        public string Code { get; set; } = string.Empty;

        public bool RememberMe { get; set; }
    }
}
