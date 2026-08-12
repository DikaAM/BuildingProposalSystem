using System.ComponentModel.DataAnnotations;

namespace BuildingProposalSystem.Models.ViewModels
{
    public class TwoFactorSetupViewModel
    {
        public string SharedKey { get; set; } = string.Empty;
        public string AuthenticatorUri { get; set; } = string.Empty;
        public string Token { get; set; } = string.Empty; 

        [Required(ErrorMessage = "Kode wajib diisi.")]
        [Display(Name = "Kode dari Authenticator App")]
        public string Code { get; set; } = string.Empty;
    }
}
