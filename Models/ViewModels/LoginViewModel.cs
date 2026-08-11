using System.ComponentModel.DataAnnotations;


namespace BuildingProposalSystem.Models.ViewModels
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Username atau Email wajib diisi.")]
        [Display(Name = "Username atau Email")]
        public string UsernameOrEmail { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password wajib diisi.")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "Ingat saya")]
        public bool RememberMe { get; set; }

    }
}
