using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.Extensions.Caching.Memory;
using System.Net;

namespace BuildingProposalSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRecaptchaService _recaptchaService;
        private readonly ILogger<AccountController> _logger;
        private readonly IMemoryCache _memoryCache;
        private readonly IEmailService _emailService;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IRecaptchaService recaptchaService, ILogger<AccountController> logger,
            IMemoryCache memoryCache,
            IEmailService emailService)

        {
            _signInManager = signInManager;
            _userManager = userManager;
            _recaptchaService = recaptchaService;
            _logger = logger;
            _memoryCache = memoryCache;
            _emailService = emailService;

        }

        [HttpGet]
        [Route("Account/Masuk")]
        [AllowAnonymous]
        public IActionResult Login()
        {
            return View();
        }

        [HttpPost]
        [Route("Account/Masuk")]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
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

            //cek user apakah isActive is true
            if (!user.IsActive)
            {
                ModelState.AddModelError(
                    string.Empty,
                    "Akun Anda sudah dinonaktifkan. Silakan hubungi Administrator.");

                return View(model);
            }

            //cek password user dulu untuk pengecekan 2FA nya true,kalo true lanjut ke login result, kalo false, redirect ke halaman setup 2FA
            var passwordCheck = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);

            if(!passwordCheck.Succeeded)
            {
                if(passwordCheck.IsLockedOut)
                {
                    ModelState.AddModelError(string.Empty, "Akun Anda terkunci sementara karena terlalu banyak percobaan login yang gagal. Silakan coba lagi setelah 2 menit.");
                    return View(model);
                }
                
                ModelState.AddModelError(string.Empty, "Username/Email atau Password salah.");
                return View(model);
            }

            //cek 2FA, apabila masih false, maka redirect ke halaman 2FA
            if(!user.TwoFactorEnabled)
            {
                var setupToken = Guid.NewGuid().ToString("N");
                _memoryCache.Set($"2fa_setup_{setupToken}", user.Id, TimeSpan.FromMinutes(5));

                return RedirectToAction("SetupTwoFactor", new { token = setupToken });
                //return RedirectToAction("SetupTwoFactor", new { userId = user.Id }); 
            }

            //apabila sudah setup, lanjut
            var result = await _signInManager.PasswordSignInAsync(
               user.UserName!,
               model.Password,
               model.RememberMe,
               lockoutOnFailure: true);

            if(result.RequiresTwoFactor)
            {
                return RedirectToAction("VerifyTwoFactor", new { rememberMe = model.RememberMe });
            }

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

        //Halaman setup 2FA untuk user yang BELUM pernah setup
        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SetupTwoFactor(string token)
        {
            if (string.IsNullOrEmpty(token) || !_memoryCache.TryGetValue($"2fa_setup_{token}", out string? userId))
            {
                return RedirectToAction("Login");
            }

            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            if (string.IsNullOrEmpty(unformattedKey))
            {
                await _userManager.ResetAuthenticatorKeyAsync(user);
                unformattedKey = await _userManager.GetAuthenticatorKeyAsync(user);
            }

            var email = await _userManager.GetEmailAsync(user);
            var authenticatorUri = $"otpauth://totp/BuildingProposalSystem:{email}" +   
                                    $"?secret={unformattedKey}&issuer=BuildingProposalSystem&digits=6";

            var model = new TwoFactorSetupViewModel
            {
                Token = token,
                SharedKey = unformattedKey!,
                AuthenticatorUri = authenticatorUri
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> SetupTwoFactor(TwoFactorSetupViewModel model)
        {

            if (string.IsNullOrEmpty(model.Token) || !_memoryCache.TryGetValue($"2fa_setup_{model.Token}", out string? userId))
            {
                return RedirectToAction("Login");
            }
            var user = await _userManager.FindByIdAsync(userId!);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var isCodeValid = await _userManager.VerifyTwoFactorTokenAsync(
                user, TokenOptions.DefaultAuthenticatorProvider, model.Code);

            if (!isCodeValid)
            {
                ModelState.AddModelError(string.Empty, "Kode tidak valid. Pastikan waktu di HP kamu sinkron.");
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);

            _memoryCache.Remove($"2fa_setup_{model.Token}");

            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        // Halaman verify 2FA untuk user yang sudah PERNAH setup 2FA

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyTwoFactor(bool rememberMe = false)
        {
            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            return View(new TwoFactorVerifyViewModel { RememberMe = rememberMe });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> VerifyTwoFactor(TwoFactorVerifyViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _signInManager.GetTwoFactorAuthenticationUserAsync();
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _signInManager.TwoFactorAuthenticatorSignInAsync(
                model.Code, model.RememberMe, rememberClient: false);

            if (result.Succeeded)
            {
                return RedirectToAction("Index", "Home");
            }

            if (result.IsLockedOut)
            {
                ModelState.AddModelError(string.Empty, "Akun terkunci karena terlalu banyak percobaan gagal. Coba lagi nanti.");
                return View(model);
            }

            ModelState.AddModelError(string.Empty, "Kode 2FA salah.");
            return View(model);
        }

        // LOGOUT
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            _logger.LogInformation("User berhasil logout.");
            return RedirectToAction("Login");
        }


        // LUPA PASSWORD

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ForgotPassword()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

           
            if (user != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                //var encodedToken = WebUtility.UrlEncode(token);

                var resetLink = Url.Action("ResetPassword", "Account",
                    new { email = user.Email, token = token },
                    protocol: Request.Scheme);

                var emailBody = $"""
            <p>Halo {user.FullName},</p>
            <p>Klik link berikut untuk reset password kamu:</p>
            <p><a href="{resetLink}">Reset Password</a></p>
            <p>Link ini berlaku 1 hari. Kalau kamu tidak meminta reset password, abaikan email ini.</p>
            """;

                try
                {
                    await _emailService.SendEmailAsync(user.Email!, "Reset Password - BuildingProposalSystem", emailBody);
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Gagal mengirim email reset password untuk {Email}", user.Email);
                   
                }
            }

            ViewBag.Message = "Jika email terdaftar, link reset password telah dikirim.";
            return View();
        }


        // RESET PASSWORD

        [HttpGet]
        [AllowAnonymous]
        public IActionResult ResetPassword(string email, string token)
        {
            if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(token))
            {
                return RedirectToAction("Login");
            }

            var model = new ResetPasswordViewModel
            {
                Email = email,
                Token = token 
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        [AllowAnonymous]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                ViewBag.Message = "Password berhasil direset.";
                return View("ResetPasswordConfirmation");
            }

            var result = await _userManager.ResetPasswordAsync(user, model.Token, model.NewPassword);

            if (result.Succeeded)
            {
                _logger.LogInformation("Password berhasil direset untuk {Email}", user.Email);
                return View("ResetPasswordConfirmation");
            }

            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            return View(model);
        }


        //CHANGE PASSWORD
        [HttpGet]
        [Authorize]
        public IActionResult ChangePassword()
        {
            return View();
        }

        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null)
            {
                return RedirectToAction("Login");
            }

            var result = await _userManager.ChangePasswordAsync(user, model.OldPassword, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            await _signInManager.RefreshSignInAsync(user);

            _logger.LogInformation("User {Email} berhasil mengubah password.", user.Email);

            ViewBag.Message = "Password berhasil diubah.";
            return View();
        }


        //REGITER USER 

        [Authorize(Roles = "Admin")]
        [HttpGet]
        public IActionResult Register()
        {
            ViewBag.Roles = new SelectList(new[] { "Staff", "Manager", "Direktur" });
            return View();
        }

        [Authorize(Roles = "Admin")]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(new[] { "Staff", "Manager", "Direktur" });
                return View(model);
            }

            var existingEmail = await _userManager.FindByEmailAsync(model.Email);
            if (existingEmail != null)
            {
                ModelState.AddModelError(nameof(model.Email), "Email sudah terdaftar.");
                ViewBag.Roles = new SelectList(new[] { "Staff", "Manager", "Direktur" });
                return View(model);
            }

            var existingUsername = await _userManager.FindByNameAsync(model.Username);
            if (existingUsername != null)
            {
                ModelState.AddModelError(nameof(model.Username), "Username sudah digunakan.");
                ViewBag.Roles = new SelectList(new[] { "Staff", "Manager", "Direktur" });
                return View(model);
            }

            var user = new ApplicationUser
            {
                UserName = model.Username,
                Email = model.Email,
                FullName = model.FullName,
                IsActive = true,
                CreatedDate = DateTime.UtcNow,
                EmailConfirmed = true 
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.Roles = new SelectList(new[] { "Staff", "Manager", "Direktur" });
                return View(model);
            }

            await _userManager.AddToRoleAsync(user, model.Role);

            _logger.LogInformation("User baru terdaftar: {Email}, Role: {Role}", user.Email, model.Role);

            return RedirectToAction("Users", "Admin");
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
