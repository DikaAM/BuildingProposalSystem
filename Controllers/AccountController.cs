using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildingProposalSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRecaptchaService _recaptchaService;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IRecaptchaService recaptchaService, ILogger<AccountController> logger) 
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _recaptchaService = recaptchaService;
            _logger = logger;
        }

        [HttpGet]
        [Route("Account/Masuk")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("Account/Masuk")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //Validasi captcha 


           
            var recaptchaToken = Request.Form["g-recaptcha-response"];

            if (string.IsNullOrEmpty(recaptchaToken))
            {
                ModelState.AddModelError(string.Empty, "Silakan centang reCAPTCHA terlebih dahulu.");
                return View(model);
            }

            var isRecaptchaValid = await _recaptchaService.VerifyAsync(recaptchaToken!);



            if (!isRecaptchaValid)
            {
                ModelState.AddModelError(string.Empty, "Verifikasi reCAPTCHA gagal. Silakan coba lagi.");
                return View(model);
            }

            //cari user by email, jika tiddak ditemukan, cari by name
            var user = await _userManager.FindByEmailAsync(model.UsernameOrEmail) ?? await _userManager.FindByNameAsync(model.UsernameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Username/Email atau Password salah.");
                return View(model);
            }
            var result = await _signInManager.PasswordSignInAsync(
               user.UserName!,
               model.Password,
               model.RememberMe,
               lockoutOnFailure: true);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {

                ModelState.AddModelError(string.Empty, "Akun terkunci karena terlalu banyak percobaan gagal. Coba lagi nanti.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Username/Email atau Password salah.");
            return View(model);
        }
    }
}
