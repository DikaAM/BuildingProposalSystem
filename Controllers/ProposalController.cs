using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BuildingProposalSystem.Controllers
{
    [Authorize]
    public class ProposalController : Controller
    {
        private readonly IProposalService _proposalService;
        private readonly ILogger<ProposalController> _logger;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProposalController(
            IProposalService proposalService,
            ILogger<ProposalController> logger,
            UserManager<ApplicationUser> userManager)
        {
            _proposalService = proposalService;
            _logger = logger;
            _userManager = userManager;
        }


        private string GetCurrentUserId()
        {
            return User.FindFirstValue(ClaimTypes.NameIdentifier)
                ?? throw new InvalidOperationException(
                    "User ID tidak ditemukan.");
        }


        // =========================================================
        // INDEX
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var proposals =
                await _proposalService.GetAllProposalsAsync();

            return View(proposals);
        }


        // =========================================================
        // CREATE
        // =========================================================

        [HttpGet]
        public IActionResult Create()
        {
            return View(new ProposalCreateViewModel
            {
                ProposalDate = DateTime.Today
            });
        }


        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(
            ProposalCreateViewModel model)
        {
            var userId = GetCurrentUserId();

            /*
             * Draft tidak membutuhkan validasi lengkap.
             */

            if (model.Action == "Draft")
            {
                ModelState.Clear();

                try
                {
                    var proposalId =
                        await _proposalService.CreateDraftAsync(
                            model,
                            userId);

                    TempData["SuccessMessage"] =
                        "Proposal berhasil disimpan sebagai draft.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id = proposalId });
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Gagal menyimpan draft proposal.");

                    ModelState.AddModelError(
                        string.Empty,
                        "Terjadi kesalahan saat menyimpan draft.");

                    return View(model);
                }
            }


            /*
             * Submit membutuhkan validasi lengkap.
             */

            if (model.Action == "Submit")
            {
                ValidateSubmitCreateModel(model);

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                try
                {
                    var proposalId =
                        await _proposalService.CreateDraftAsync(
                            model,
                            userId);

                    /*
                     * Proposal baru masih berstatus Draft
                     * karena CreateDraftAsync memang membuat Draft.
                     *
                     * Submit final akan kita proses melalui
                     * endpoint submit.
                     */

                    var editModel =
                        await _proposalService
                            .GetProposalForEditAsync(
                                proposalId,
                                userId);

                    if (editModel == null)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "Proposal gagal diproses.");

                        return View(model);
                    }

                    editModel.Action = "Submit";

                    var submitted =
                        await _proposalService.SubmitDraftAsync(
                            editModel,
                            userId);

                    if (!submitted)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "Proposal gagal disubmit.");

                        return View(model);
                    }

                    TempData["SuccessMessage"] =
                        "Proposal berhasil disubmit dan menunggu approval Manager.";

                    return RedirectToAction(
                        nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Gagal submit proposal.");

                    ModelState.AddModelError(
                        string.Empty,
                        "Terjadi kesalahan saat submit proposal.");

                    return View(model);
                }
            }


            ModelState.AddModelError(
                string.Empty,
                "Action proposal tidak valid.");

            return View(model);
        }


        // =========================================================
        // EDIT
        // =========================================================

        [HttpGet]
        public async Task<IActionResult> Edit(Guid id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var model = await _proposalService.GetProposalForEditAsync(
                id,
                userId);

            if (model == null)
            {
                TempData["ErrorMessage"] =
                    "Proposal tidak ditemukan atau Anda tidak memiliki akses untuk mengedit proposal ini.";

                return RedirectToAction(nameof(Index));
            }

            return View(model);
        }


        // =========================================================
        // EDIT POST
        // =========================================================

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(
            ProposalEditViewModel model)
        {
            var userId = User.FindFirstValue(
                ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            // =====================================================
            // SAVE DRAFT
            // =====================================================

            if (model.Action == "Draft")
            {
                // Draft boleh menyimpan data yang belum lengkap.
                ModelState.Clear();

                try
                {
                    var updated =
                        await _proposalService
                            .UpdateDraftAsync(
                                model,
                                userId);

                    if (!updated)
                    {
                        TempData["ErrorMessage"] =
                            "Proposal tidak ditemukan atau sudah tidak dapat diedit.";

                        return RedirectToAction(
                            nameof(Index));
                    }

                    TempData["SuccessMessage"] =
                        "Perubahan proposal berhasil disimpan.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id = model.Id });
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Gagal menyimpan perubahan proposal {ProposalId}.",
                        model.Id);

                    ModelState.AddModelError(
                        string.Empty,
                        "Terjadi kesalahan saat menyimpan perubahan.");

                    return View(model);
                }
            }


            // =====================================================
            // SUBMIT
            // =====================================================

            if (model.Action == "Submit")
            {
                ValidateSubmitEditModel(model);

                if (!ModelState.IsValid)
                {
                    return View(model);
                }

                try
                {
                    var submitted =
                        await _proposalService
                            .SubmitDraftAsync(
                                model,
                                userId);

                    if (!submitted)
                    {
                        ModelState.AddModelError(
                            string.Empty,
                            "Proposal gagal disubmit. Pastikan proposal masih dapat disubmit.");

                        return View(model);
                    }

                    TempData["SuccessMessage"] =
                        "Proposal berhasil disubmit dan menunggu approval Manager.";

                    return RedirectToAction(
                        nameof(Index));
                }
                catch (Exception ex)
                {
                    _logger.LogError(
                        ex,
                        "Gagal submit proposal {ProposalId}.",
                        model.Id);

                    ModelState.AddModelError(
                        string.Empty,
                        "Terjadi kesalahan saat submit proposal.");

                    return View(model);
                }
            }


            // =====================================================
            // INVALID ACTION
            // =====================================================

            ModelState.AddModelError(
                string.Empty,
                "Action proposal tidak valid.");

            return View(model);
        }



        // =========================================================
        // VALIDATION CREATE
        // =========================================================

        private void ValidateSubmitCreateModel(
            ProposalCreateViewModel model)
        {
            if (!model.ProposalDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.ProposalDate),
                    "Tanggal pengajuan wajib diisi.");
            }

            if (string.IsNullOrWhiteSpace(
                    model.BuildingName))
            {
                ModelState.AddModelError(
                    nameof(model.BuildingName),
                    "Nama gedung wajib diisi.");
            }

            if (string.IsNullOrWhiteSpace(
                    model.Address))
            {
                ModelState.AddModelError(
                    nameof(model.Address),
                    "Alamat wajib diisi.");
            }

            if (!model.Latitude.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.Latitude),
                    "Latitude wajib diisi.");
            }

            if (!model.Longitude.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.Longitude),
                    "Longitude wajib diisi.");
            }

            if (!model.EstimatedCost.HasValue ||
                model.EstimatedCost <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.EstimatedCost),
                    "Estimasi biaya wajib lebih besar dari nol.");
            }

            if (string.IsNullOrWhiteSpace(
                    model.Description))
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Deskripsi wajib diisi.");
            }

            if (model.ProposalFile == null ||
                model.ProposalFile.Length == 0)
            {
                ModelState.AddModelError(
                    nameof(model.ProposalFile),
                    "Proposal PDF wajib diupload.");
            }
        }


        // =========================================================
        // VALIDATION EDIT
        // =========================================================

        private void ValidateSubmitEditModel(
            ProposalEditViewModel model)
        {
            if (!model.ProposalDate.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.ProposalDate),
                    "Tanggal pengajuan wajib diisi.");
            }

            if (string.IsNullOrWhiteSpace(
                    model.BuildingName))
            {
                ModelState.AddModelError(
                    nameof(model.BuildingName),
                    "Nama gedung wajib diisi.");
            }

            if (string.IsNullOrWhiteSpace(
                    model.Address))
            {
                ModelState.AddModelError(
                    nameof(model.Address),
                    "Alamat wajib diisi.");
            }

            if (!model.Latitude.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.Latitude),
                    "Latitude wajib diisi.");
            }

            if (!model.Longitude.HasValue)
            {
                ModelState.AddModelError(
                    nameof(model.Longitude),
                    "Longitude wajib diisi.");
            }

            if (!model.EstimatedCost.HasValue ||
                model.EstimatedCost <= 0)
            {
                ModelState.AddModelError(
                    nameof(model.EstimatedCost),
                    "Estimasi biaya wajib lebih besar dari nol.");
            }

            if (string.IsNullOrWhiteSpace(
                    model.Description))
            {
                ModelState.AddModelError(
                    nameof(model.Description),
                    "Deskripsi wajib diisi.");
            }

            /*
             * ProposalFile tidak wajib saat Edit
             * jika PDF sebelumnya sudah tersedia.
             *
             * Service akan mengecek existing attachment.
             */
        }

        private string GetCurrentUserRole()
        {
            if (User.IsInRole("Admin"))
            {
                return "Admin";
            }

            if (User.IsInRole("Manager"))
            {
                return "Manager";
            }

            if (User.IsInRole("Direktur"))
            {
                return "Direktur";
            }

            return string.Empty;
        }

        [HttpGet]
        public async Task<IActionResult> Approval(Guid id)
        {
            var userId = GetCurrentUserId();

            var userRole = GetCurrentUserRole();

            if (string.IsNullOrWhiteSpace(userRole))
            {
                return Forbid();
            }


            var model =
                await _proposalService
                    .GetProposalForApprovalAsync(
                        id,
                        userId,
                        userRole);

            if (model == null)
            {
                TempData["ErrorMessage"] =
                    "Proposal tidak tersedia untuk approval Anda.";

                return RedirectToAction(nameof(Index));
            }


            return View(
                "Edit",
                model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approval(
                Guid id,
                string approvalAction,
                string? approvalComment)
        {
            var userId = GetCurrentUserId();

            var userRole = GetCurrentUserRole();

            if (string.IsNullOrWhiteSpace(userRole))
            {
                return Forbid();
            }


            // =====================================================
            // VALIDASI ACTION
            // =====================================================

            if (approvalAction != "Approve" &&
                approvalAction != "Reject")
            {
                TempData["ErrorMessage"] =
                    "Action approval tidak valid.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }


            // =====================================================
            // APPROVE
            // =====================================================

            if (approvalAction == "Approve")
            {
                var approved =
                    await _proposalService.ApproveAsync(
                        id,
                        userId,
                        userRole,
                        approvalComment);

                if (!approved)
                {
                    TempData["ErrorMessage"] =
                        "Proposal gagal diapprove. Pastikan proposal masih menunggu approval dan Anda memiliki hak approval.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id });
                }


                TempData["SuccessMessage"] =
                    "Proposal berhasil diapprove.";

                return RedirectToAction(
                    nameof(Index));
            }


            // =====================================================
            // REJECT
            // =====================================================

            if (approvalAction == "Reject")
            {
                if (string.IsNullOrWhiteSpace(
                        approvalComment))
                {
                    TempData["ErrorMessage"] =
                        "Alasan reject wajib diisi.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id });
                }


                var rejected =
                    await _proposalService.RejectAsync(
                        id,
                        userId,
                        userRole,
                        approvalComment);

                if (!rejected)
                {
                    TempData["ErrorMessage"] =
                        "Proposal gagal direject. Pastikan proposal masih menunggu approval dan Anda memiliki hak approval.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id });
                }


                TempData["SuccessMessage"] =
                    "Proposal berhasil direject dan dikembalikan kepada Staff untuk diperbaiki.";

                return RedirectToAction(
                    nameof(Index));
            }


            return RedirectToAction(
                nameof(Edit),
                new { id });
        }

        //POST APPROVE

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Approve(
            Guid id,
            string? comment)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Challenge();
            }


            var roles =
                await _userManager.GetRolesAsync(user);

            var userRole =
                roles.FirstOrDefault();

            if (string.IsNullOrEmpty(userRole))
            {
                TempData["ErrorMessage"] =
                    "Role user tidak ditemukan.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }


            try
            {
                var approved =
                    await _proposalService
                        .ApproveAsync(
                            id,
                            userId,
                            userRole,
                            comment);

                if (!approved)
                {
                    TempData["ErrorMessage"] =
                        "Proposal tidak dapat di-approve. " +
                        "Pastikan Anda memiliki hak approval " +
                        "pada level yang sesuai.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id });
                }


                TempData["SuccessMessage"] =
                    "Proposal berhasil di-approve.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Gagal approve proposal {ProposalId}.",
                    id);

                TempData["ErrorMessage"] =
                    "Terjadi kesalahan saat melakukan approval.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }
        }
        //POST REJECT

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reject(
            Guid id,
            string? comment)
        {
            var userId =
                User.FindFirstValue(
                    ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }


            var user =
                await _userManager.FindByIdAsync(userId);

            if (user == null)
            {
                return Challenge();
            }


            var roles =
                await _userManager.GetRolesAsync(user);

            var userRole =
                roles.FirstOrDefault();

            if (string.IsNullOrEmpty(userRole))
            {
                TempData["ErrorMessage"] =
                    "Role user tidak ditemukan.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }


            if (string.IsNullOrWhiteSpace(comment))
            {
                TempData["ErrorMessage"] =
                    "Alasan reject wajib diisi.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }


            try
            {
                var rejected =
                    await _proposalService
                        .RejectAsync(
                            id,
                            userId,
                            userRole,
                            comment);

                if (!rejected)
                {
                    TempData["ErrorMessage"] =
                        "Proposal tidak dapat di-reject. " +
                        "Pastikan Anda memiliki hak approval " +
                        "pada level yang sesuai.";

                    return RedirectToAction(
                        nameof(Edit),
                        new { id });
                }


                TempData["SuccessMessage"] =
                    "Proposal berhasil di-reject " +
                    "dan dikembalikan kepada Staff.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(
                    ex,
                    "Gagal reject proposal {ProposalId}.",
                    id);

                TempData["ErrorMessage"] =
                    "Terjadi kesalahan saat melakukan reject.";

                return RedirectToAction(
                    nameof(Edit),
                    new { id });
            }
        }
    }
}