using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;


namespace BuildingProposalSystem.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly IRecaptchaService _recaptchaService;
        private readonly ILogger<AccountController> _logger;
        private readonly IMemoryCache _memoryCache; 


        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            IRecaptchaService recaptchaService, ILogger<AccountController> logger,
            IMemoryCache memoryCache) 

        {
            _signInManager = signInManager;
            _userManager = userManager;
            _recaptchaService = recaptchaService;
            _logger = logger;
            _memoryCache = memoryCache;
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
                TempData.Keep("2FASetupUserId");
                return View(model);
            }

            await _userManager.SetTwoFactorEnabledAsync(user, true);

            _memoryCache.Remove($"2fa_setup_{model.Token}");

            await _signInManager.SignInAsync(user, isPersistent: false);

            return RedirectToAction("Index", "Home");
        }

        // Halaman verify 2FA untuk user yang sudah PERNAH setup 2FA

        [HttpGet]
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
    }
}
