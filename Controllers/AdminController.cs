using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace BuildingProposalSystem.Controllers
{
    [Authorize(Roles = "Admin")]
    [Route("Admin/Users")]
    public class AdminController : Controller
    {
        private readonly IUserManagementService _userManagementService;
        private static readonly string[] Roles = { "Admin", "Staff", "Manager", "Direktur" };

        public AdminController(IUserManagementService userManagementService)
        {
            _userManagementService = userManagementService;
        }

        [HttpGet]
        [Route("")]
        public async Task<IActionResult> Index()
        {
            var users = await _userManagementService.GetAllUsersAsync();
            return View(users);
        }

        [HttpGet]
        [Route("Create")]
        public IActionResult Create()
        {
            ViewBag.Roles = new SelectList(Roles);
            return View();
        }

        [HttpPost]
        [Route("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(UserCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(Roles);
                return View(model);
            }

            var result = await _userManagementService.CreateUserAsync(model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.Roles = new SelectList(Roles);
                return View(model);
            }

            TempData["SuccessMessage"] = "User berhasil dibuat.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var model = await _userManagementService.GetUserForEditAsync(id);
            if (model == null)
            {
                return NotFound();
            }

            ViewBag.Roles = new SelectList(Roles);
            return View(model);
        }

        [HttpPost]
        [Route("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, UserEditViewModel model)
        {
            if (id != model.Id)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Roles = new SelectList(Roles);
                return View(model);
            }

            var result = await _userManagementService.UpdateUserAsync(model);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                ViewBag.Roles = new SelectList(Roles);
                return View(model);
            }

            TempData["SuccessMessage"] = "Data user berhasil diperbarui.";
            return RedirectToAction("Index");
        }

        [HttpPost]
        [Route("ToggleActive/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            await _userManagementService.ToggleActiveStatusAsync(id);
            TempData["SuccessMessage"] = "Status user berhasil diubah.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        [Route("ResetPassword/{id}")]
        public async Task<IActionResult> ResetPassword(string id)
        {
            var user = await _userManagementService.GetUserForEditAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new AdminResetPasswordViewModel
            {
                UserId = user.Id,
                Email = user.Email
            };

            return View(model);
        }

        [HttpPost]
        [Route("ResetPassword/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(string id, AdminResetPasswordViewModel model)
        {
            if (id != model.UserId)
            {
                return BadRequest();
            }

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userManagementService.ResetPasswordAsync(model.UserId, model.NewPassword);

            if (!result.Succeeded)
            {
                foreach (var error in result.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            TempData["SuccessMessage"] = "Password user berhasil direset.";
            return RedirectToAction("Index");
        }
    }
}