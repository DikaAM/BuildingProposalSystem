using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildingProposalSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;

        public AccountController(SignInManager<ApplicationUser> signInManager, UserManager<ApplicationUser> userManager)
        {
            _signInManager = signInManager;
            _userManager = userManager;
        }

        [HttpGet]
        [Route("Account/Masuk")]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [Route("Account/Masuk")]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            //cari user by email, jika tiddak ditemukan, cari by name
            var user = await _userManager.FindByEmailAsync(model.UsernameOrEmail) ?? await _userManager.FindByNameAsync(model.UsernameOrEmail);

            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "Username atau Email tidak ditemukan.");
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
