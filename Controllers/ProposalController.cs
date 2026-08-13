using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace BuildingProposalSystem.Controllers
{
    //[Authorize(Roles = "Staff,Admin")]
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
        public async Task<IActionResult> Index()
        {
            var proposals = await _proposalService.GetAllProposalsAsync();

            return View(proposals);
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

            var proposal = await _proposalService.CreateDraftAsync(model, userId);
           
            if (proposal == Guid.Empty)
            {
                ModelState.AddModelError(string.Empty, "Gagal membuat draft proposal.");
                return View(model);
            }

            return RedirectToAction("Index", "Proposal");
        }
    }
}