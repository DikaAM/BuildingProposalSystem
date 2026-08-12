using System.ComponentModel.DataAnnotations;


namespace BuildingProposalSystem.Models.ViewModels
{
    public class ForgotPasswordViewModel
    {
        [Required(ErrorMessage = "Email wajib diisi.")]
        [EmailAddress(ErrorMessage = "Format email tidak valid.")]
        public string Email { get; set; } = string.Empty;
    }
}
