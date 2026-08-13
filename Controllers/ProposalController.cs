using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildingProposalSystem.Controllers
{
    [Authorize(Roles = "Staff")] 
    public class ProposalController : Controller
    {
        private readonly IProposalService _proposalService;
        private readonly UserManager<Models.Entities.ApplicationUser> _userManager;

        public ProposalController(
            IProposalService proposalService,
            UserManager<Models.Entities.ApplicationUser> userManager)
        {
            _proposalService = proposalService;
            _userManager = userManager;
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProposalCreateViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = _userManager.GetUserId(User);
            if (userId == null)
            {
                return RedirectToAction("Login", "Account");
            }

            var proposalId = await _proposalService.CreateDraftAsync(model, userId);

            TempData["SuccessMessage"] = "Draft proposal berhasil disimpan.";
            //return RedirectToAction("Create"); 
            return RedirectToAction("Edit", new { id = proposalId }); 
        }
    }
}