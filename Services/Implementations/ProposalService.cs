using BuildingProposalSystem.Data;
using BuildingProposalSystem.Models.Entities;
using BuildingProposalSystem.Models.Enums;
using BuildingProposalSystem.Models.ViewModels;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;

namespace BuildingProposalSystem.Services.Implementations
{
    public class ProposalService : IProposalService
    {
        private readonly ApplicationDbContext _context;
        private readonly IProposalNumberService _proposalNumberService;
        private readonly ILogger<ProposalService> _logger;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<ApplicationUser> _userManager;

        public ProposalService(
            ApplicationDbContext context,
            IProposalNumberService proposalNumberService,
            ILogger<ProposalService> logger,
            IWebHostEnvironment environment,
            UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _proposalNumberService = proposalNumberService;
            _logger = logger;
            _environment = environment;
            _userManager = userManager;

        }


        public async Task<Guid> CreateDraftAsync(
            ProposalCreateViewModel model,
            string userId)
        {
            var proposalNumber =
                await _proposalNumberService.GenerateNumberAsync();

            var proposal = new BuildingProposal
            {
                Id = Guid.NewGuid(),

                ProposalNumber = proposalNumber,

                ProposalDate =
                    model.ProposalDate ?? DateTime.UtcNow,

                BuildingName =
                    model.BuildingName ?? string.Empty,

                Address =
                    model.Address ?? string.Empty,

                EstimatedCost =
                    model.EstimatedCost ?? 0,

                Description =
                    model.Description,

                Latitude =
                    model.Latitude ?? 0,

                Longitude =
                    model.Longitude ?? 0,

                Status = ProposalStatus.Draft,

                CurrentApproverRole =
                    ApprovalLevel.None,

                CreatedBy = userId,

                CreatedDate = DateTime.UtcNow
            };


            _context.BuildingProposals.Add(proposal);

            await _context.SaveChangesAsync();


            if (model.ProposalFile != null &&
                model.ProposalFile.Length > 0)
            {
                await SaveAttachmentAsync(
                    proposal,
                    model.ProposalFile,
                    userId);
            }


            _logger.LogInformation(
                "Draft proposal {ProposalNumber} dibuat oleh {UserId}",
                proposalNumber,
                userId);


            return proposal.Id;
        }


        public async Task<List<ProposalListItemViewModel>>
            GetAllProposalsAsync()
        {
            var proposals =
                await _context.BuildingProposals
                    .OrderByDescending(x => x.CreatedDate)
                    .Select(x => new ProposalListItemViewModel
                    {
                        Id = x.Id,
                        ProposalNumber = x.ProposalNumber,
                        ProposalDate = x.ProposalDate,
                        BuildingName = x.BuildingName,
                        EstimatedCost = x.EstimatedCost,
                        Status = x.Status.ToString(),
                        CreatedDate = x.CreatedDate
                    })
                    .ToListAsync();

            return proposals;
        }


        public async Task<ProposalEditViewModel?> GetProposalForEditAsync(
            Guid id,
            string userId)
        {
            var proposal = await _context.BuildingProposals
                .Include(x => x.Attachments)
                .Include(x => x.ApprovalHistories)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (proposal == null)
            {
                return null;
            }


            // =====================================================
            // GET USER
            // =====================================================

            var user = await _userManager.FindByIdAsync(userId);


            if (user == null)
            {
                return null;
            }


            // =====================================================
            // USER ROLE
            // =====================================================

            var userRoles = await _userManager.GetRolesAsync(user);

            var userRole = userRoles.FirstOrDefault() ?? string.Empty;

            // =====================================================
            // EDIT PERMISSION
            // =====================================================

            var isOwner = proposal.CreatedBy == userId;

            var canEdit =
                isOwner &&
                (
                    proposal.Status == ProposalStatus.Draft ||
                    proposal.Status == ProposalStatus.Rejected
                );


            // =====================================================
            // APPROVAL PERMISSION
            // =====================================================

            var canApprove =
                CanUserApprove(
                    proposal.CurrentApproverRole,
                    userRole);


            // =====================================================
            // LATEST REJECTION
            // =====================================================

            var latestRejection =
                proposal.ApprovalHistories
                    .Where(x => x.Action == "Reject")
                    .OrderByDescending(x => x.ActionDate)
                    .FirstOrDefault();


            // =====================================================
            // VIEW MODEL
            // =====================================================

            return new ProposalEditViewModel
            {
                Id =
                    proposal.Id,

                ProposalNumber =
                    proposal.ProposalNumber,

                ProposalDate =
                    proposal.ProposalDate,

                BuildingName =
                    proposal.BuildingName,

                Address =
                    proposal.Address,

                Latitude =
                    proposal.Latitude,

                Longitude =
                    proposal.Longitude,

                EstimatedCost =
                    proposal.EstimatedCost,

                Description =
                    proposal.Description,

                Status =
                    proposal.Status.ToString(),

                CanEdit =
                    canEdit,

                CanApprove =
                    canApprove,

                IsRejected =
                    proposal.Status ==
                    ProposalStatus.Rejected,

                RejectionReason =
                    latestRejection?.Comment,

                Attachments =
                    proposal.Attachments
                        .OrderByDescending(
                            x => x.UploadedDate)
                        .Select(x =>
                            new AttachmentListItemViewModel
                            {
                                Id =
                                    x.Id,

                                OriginalFileName =
                                    x.OriginalFileName,

                                FileSize =
                                    x.FileSize,

                                UploadedDate =
                                    x.UploadedDate
                            })
                        .ToList()
            };
        }


        public async Task<bool>
            UpdateDraftAsync(
                ProposalEditViewModel model,
                string userId)
        {
            var proposal =
                await _context.BuildingProposals
                    .Include(x => x.Attachments)
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.Id &&
                        x.CreatedBy == userId);

            if (proposal == null)
            {
                return false;
            }


            if (proposal.Status != ProposalStatus.Draft &&
                proposal.Status != ProposalStatus.Rejected)
                {
                return false;
            }


            /*
             * Update proposal data.
             *
             * Karena entity saat ini masih non-nullable,
             * nilai kosong akan menggunakan default value.
             */

            proposal.ProposalDate =
                model.ProposalDate ?? proposal.ProposalDate;

            proposal.BuildingName =
                model.BuildingName ?? string.Empty;

            proposal.Address =
                model.Address ?? string.Empty;

            proposal.Latitude =
                model.Latitude ?? 0;

            proposal.Longitude =
                model.Longitude ?? 0;

            proposal.EstimatedCost =
                model.EstimatedCost ?? 0;

            proposal.Description =
                model.Description;


            proposal.UpdatedBy = userId;

            proposal.UpdatedDate = DateTime.UtcNow;


            /*
             * Jika user memilih PDF baru,
             * ganti attachment PDF lama.
             */

            if (model.ProposalFile != null &&
                model.ProposalFile.Length > 0)
            {
                await ReplaceAttachmentAsync(
                    proposal,
                    model.ProposalFile,
                    userId);
            }


            await _context.SaveChangesAsync();


            _logger.LogInformation(
                "Draft proposal {ProposalNumber} diubah oleh {UserId}",
                proposal.ProposalNumber,
                userId);


            return true;
        }


        public async Task<bool>
            SubmitDraftAsync(
                ProposalEditViewModel model,
                string userId)
        {
            var proposal =
                await _context.BuildingProposals
                    .Include(x => x.Attachments)
                    .FirstOrDefaultAsync(x =>
                        x.Id == model.Id &&
                        x.CreatedBy == userId);

            if (proposal == null)
            {
                return false;
            }


            if (proposal.Status != ProposalStatus.Draft &&
                proposal.Status != ProposalStatus.Rejected)
            {
                return false;
            }


            /*
             * Validasi data wajib untuk Submit.
             */

            if (!model.ProposalDate.HasValue)
            {
                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    model.BuildingName))
            {
                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    model.Address))
            {
                return false;
            }


            if (!model.Latitude.HasValue)
            {
                return false;
            }


            if (!model.Longitude.HasValue)
            {
                return false;
            }


            if (!model.EstimatedCost.HasValue ||
                model.EstimatedCost <= 0)
            {
                return false;
            }


            if (string.IsNullOrWhiteSpace(
                    model.Description))
            {
                return false;
            }


            /*
             * PDF wajib ada.
             *
             * Jika user upload file baru, file tersebut
             * akan menggantikan file lama.
             */

            var hasExistingPdf =
                proposal.Attachments.Any(x =>
                    x.FileExtension.ToLower() == ".pdf");

            var hasNewPdf =
                model.ProposalFile != null &&
                model.ProposalFile.Length > 0;

            if (!hasExistingPdf && !hasNewPdf)
            {
                return false;
            }


            /*
             * Update data proposal.
             */

            proposal.ProposalDate =
                model.ProposalDate.Value;

            proposal.BuildingName =
                model.BuildingName.Trim();

            proposal.Address =
                model.Address.Trim();

            proposal.Latitude =
                model.Latitude.Value;

            proposal.Longitude =
                model.Longitude.Value;

            proposal.EstimatedCost =
                model.EstimatedCost.Value;

            proposal.Description =
                model.Description;


            /*
             * Jika upload PDF baru,
             * replace PDF lama.
             */

            if (hasNewPdf)
            {
                await ReplaceAttachmentAsync(
                    proposal,
                    model.ProposalFile!,
                    userId);
            }


            /*
             * Masuk workflow Manager.
             */

            proposal.Status =
                ProposalStatus.WaitingManagerApproval;

            proposal.CurrentApproverRole =
                ApprovalLevel.Manager;

            proposal.UpdatedBy =
                userId;

            proposal.UpdatedDate =
                DateTime.UtcNow;


            await _context.SaveChangesAsync();


            _logger.LogInformation(
                "Proposal {ProposalNumber} disubmit oleh {UserId} dan menunggu approval Manager",
                proposal.ProposalNumber,
                userId);


            return true;
        }


        private async Task SaveAttachmentAsync(
            BuildingProposal proposal,
            IFormFile file,
            string userId)
        {
            ValidatePdf(file);


            var uploadDirectory =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "proposals");


            Directory.CreateDirectory(
                uploadDirectory);


            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            var storedFileName =
                $"{Guid.NewGuid():N}{extension}";

            var filePath =
                Path.Combine(
                    uploadDirectory,
                    storedFileName);


            await using (
                var stream =
                    new FileStream(
                        filePath,
                        FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }


            var attachment =
                new ProposalAttachment
                {
                    Id = Guid.NewGuid(),

                    ProposalId =
                        proposal.Id,

                    OriginalFileName =
                        Path.GetFileName(
                            file.FileName),

                    StoredFileName =
                        storedFileName,

                    FileExtension =
                        extension,

                    ContentType =
                        file.ContentType,

                    FileSize =
                        file.Length,

                    UploadedBy =
                        userId,

                    UploadedDate =
                        DateTime.UtcNow
                };


            _context.ProposalAttachments
                .Add(attachment);
        }


        private async Task ReplaceAttachmentAsync(
            BuildingProposal proposal,
            IFormFile file,
            string userId)
        {
            ValidatePdf(file);


            var oldAttachments =
                proposal.Attachments
                    .ToList();


            foreach (var oldAttachment
                in oldAttachments)
            {
                DeletePhysicalFile(
                    oldAttachment.StoredFileName);

                _context.ProposalAttachments
                    .Remove(oldAttachment);
            }


            await SaveAttachmentAsync(
                proposal,
                file,
                userId);
        }


        private void DeletePhysicalFile(
            string storedFileName)
        {
            if (string.IsNullOrWhiteSpace(
                    storedFileName))
            {
                return;
            }


            var filePath =
                Path.Combine(
                    _environment.WebRootPath,
                    "uploads",
                    "proposals",
                    storedFileName);


            if (File.Exists(filePath))
            {
                File.Delete(filePath);
            }
        }


        private static void ValidatePdf(
            IFormFile file)
        {
            var extension =
                Path.GetExtension(file.FileName)
                    .ToLowerInvariant();

            if (extension != ".pdf")
            {
                throw new InvalidOperationException(
                    "File proposal harus berformat PDF.");
            }


            if (!string.Equals(
                    file.ContentType,
                    "application/pdf",
                    StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException(
                    "File proposal harus berupa PDF.");
            }
        }

        public async Task<bool> ApproveAsync(
            Guid id,
            string userId,
            string userRole,
            string? comment)
        {
            var proposal =
                await _context.BuildingProposals
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (proposal == null)
            {
                return false;
            }


            if (!CanUserApprove(
                    proposal.CurrentApproverRole,
                    userRole))
            {
                return false;
            }


            var approvalLevel =
                proposal.CurrentApproverRole;


            /*
             * =====================================================
             * SIMPAN APPROVAL HISTORY
             * =====================================================
             */

            var history = new ApprovalHistory
            {
                Id = Guid.NewGuid(),

                ProposalId =
                    proposal.Id,

                ApprovalLevel =
                    approvalLevel,

                Action =
                    "Approve",

                Comment =
                    comment,

                ApprovedBy =
                    userId,

                ActionDate =
                    DateTime.UtcNow
            };

            _context.ApprovalHistories.Add(history);


            /*
             * =====================================================
             * APPROVAL WORKFLOW
             * =====================================================
             */

            const decimal directorApprovalLimit = 10_000_000m;


            if (approvalLevel == ApprovalLevel.Manager)
            {
                if (proposal.EstimatedCost < directorApprovalLimit)
                {
                    proposal.Status = ProposalStatus.Submitted;

                    proposal.CurrentApproverRole = ApprovalLevel.None;
                }
                else
                {                   
                    proposal.Status = ProposalStatus.WaitingDirectorApproval;

                    proposal.CurrentApproverRole = ApprovalLevel.Director;
                }
            }
            else if (approvalLevel == ApprovalLevel.Director)
            {

                proposal.Status = ProposalStatus.Submitted;

                proposal.CurrentApproverRole =
                    ApprovalLevel.None;
            }
            else
            {
                return false;
            }


            proposal.UpdatedBy = userId;

            proposal.UpdatedDate = DateTime.UtcNow;


            await _context.SaveChangesAsync();


            _logger.LogInformation(
                "Proposal {ProposalNumber} diapprove oleh {UserId} sebagai {Role}. Status akhir: {Status}",
                proposal.ProposalNumber,
                userId,
                userRole,
                proposal.Status);


            return true;
        }
        //public async Task<bool> ApproveAsync(
        //    Guid id,
        //    string userId,
        //    string userRole,
        //    string? comment)
        //{
        //    var proposal =
        //        await _context.BuildingProposals
        //            .FirstOrDefaultAsync(x => x.Id == id);

        //    if (proposal == null)
        //    {
        //        return false;
        //    }


        //    // =====================================================
        //    // CHECK APPROVAL PERMISSION
        //    // =====================================================

        //    if (!CanUserApprove(
        //            proposal.CurrentApproverRole,
        //            userRole))
        //    {
        //        return false;
        //    }


        //    var approvalLevel =
        //        proposal.CurrentApproverRole;


        //    // =====================================================
        //    // PROCESS APPROVAL
        //    // =====================================================

        //    if (approvalLevel == ApprovalLevel.Manager)
        //    {
        //        proposal.Status =
        //            ProposalStatus.WaitingDirectorApproval;

        //        proposal.CurrentApproverRole =
        //            ApprovalLevel.Director;
        //    }
        //    else if (approvalLevel == ApprovalLevel.Director)
        //    {
        //        proposal.Status =
        //            ProposalStatus.Submitted;

        //        proposal.CurrentApproverRole =
        //            ApprovalLevel.None;
        //    }
        //    else
        //    {
        //        return false;
        //    }


        //    // =====================================================
        //    // APPROVAL HISTORY
        //    // =====================================================

        //    var history = new ApprovalHistory
        //    {
        //        Id = Guid.NewGuid(),

        //        ProposalId =
        //            proposal.Id,

        //        ApprovalLevel =
        //            approvalLevel,

        //        Action =
        //            "Approve",

        //        Comment =
        //            comment,

        //        ApprovedBy =
        //            userId,

        //        ActionDate =
        //            DateTime.UtcNow
        //    };

        //    _context.ApprovalHistories.Add(history);


        //    // =====================================================
        //    // AUDIT
        //    // =====================================================

        //    proposal.UpdatedBy =
        //        userId;

        //    proposal.UpdatedDate =
        //        DateTime.UtcNow;


        //    await _context.SaveChangesAsync();


        //    _logger.LogInformation(
        //        "Proposal {ProposalNumber} diapprove oleh {UserId} sebagai {Role}",
        //        proposal.ProposalNumber,
        //        userId,
        //        userRole);


        //    return true;
        //}

        public async Task<bool> RejectAsync(
            Guid id,
            string userId,
            string userRole,
            string? comment)
        {
            if (string.IsNullOrWhiteSpace(comment))
            {
                return false;
            }


            var proposal =
                await _context.BuildingProposals
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (proposal == null)
            {
                return false;
            }


            // =====================================================
            // CHECK APPROVAL PERMISSION
            // =====================================================

            if (!CanUserApprove(
                    proposal.CurrentApproverRole,
                    userRole))
            {
                return false;
            }


            var approvalLevel =
                proposal.CurrentApproverRole;


            // =====================================================
            // VALIDATE APPROVAL LEVEL
            // =====================================================

            if (approvalLevel != ApprovalLevel.Manager &&
                approvalLevel != ApprovalLevel.Director)
            {
                return false;
            }


            // =====================================================
            // APPROVAL HISTORY
            // =====================================================

            var history = new ApprovalHistory
            {
                Id = Guid.NewGuid(),

                ProposalId =
                    proposal.Id,

                ApprovalLevel =
                    approvalLevel,

                Action =
                    "Reject",

                Comment =
                    comment.Trim(),

                ApprovedBy =
                    userId,

                ActionDate =
                    DateTime.UtcNow
            };

            _context.ApprovalHistories.Add(history);


            // =====================================================
            // RETURN TO STAFF
            // =====================================================

            proposal.Status =
                ProposalStatus.Rejected;

            proposal.CurrentApproverRole =
                ApprovalLevel.None;


            // =====================================================
            // AUDIT
            // =====================================================

            proposal.UpdatedBy =
                userId;

            proposal.UpdatedDate =
                DateTime.UtcNow;


            await _context.SaveChangesAsync();


            _logger.LogInformation(
                "Proposal {ProposalNumber} ditolak oleh {UserId} sebagai {Role}",
                proposal.ProposalNumber,
                userId,
                userRole);


            return true;
        }
        private static bool CanUserApprove(
        ApprovalLevel approvalLevel,
        string userRole)
        {
            if (userRole.Equals(
                    "Admin",
                    StringComparison.OrdinalIgnoreCase))
            {
                return approvalLevel == ApprovalLevel.Manager ||
                       approvalLevel == ApprovalLevel.Director;
            }


            if (approvalLevel == ApprovalLevel.Manager)
            {
                return userRole.Equals(
                    "Manager",
                    StringComparison.OrdinalIgnoreCase);
            }


            if (approvalLevel == ApprovalLevel.Director)
            {
                return userRole.Equals(
                    "Direktur",
                    StringComparison.OrdinalIgnoreCase);
            }


            return false;
        }

        public async Task<ProposalEditViewModel?>
        GetProposalForApprovalAsync(
        Guid id,
        string userId,
        string userRole)
        {
            var proposal =
                await _context.BuildingProposals
                    .Include(x => x.Attachments)
                    .Include(x => x.ApprovalHistories)
                    .ThenInclude(x => x.Approver)
                    .FirstOrDefaultAsync(x => x.Id == id);

            if (proposal == null)
            {
                return null;
            }


            if (!CanUserApprove(
                    proposal.CurrentApproverRole,
                    userRole))
            {
                return null;
            }


            var latestRejection =
                proposal.ApprovalHistories
                    .Where(x => x.Action == "Reject")
                    .OrderByDescending(x => x.ActionDate)
                    .FirstOrDefault();


            return new ProposalEditViewModel
            {
                Id = proposal.Id,

                ProposalNumber =
                    proposal.ProposalNumber,

                ProposalDate =
                    proposal.ProposalDate,

                BuildingName =
                    proposal.BuildingName,

                Address =
                    proposal.Address,

                Latitude =
                    proposal.Latitude,

                Longitude =
                    proposal.Longitude,

                EstimatedCost =
                    proposal.EstimatedCost,

                Description =
                    proposal.Description,

                Status =
                    proposal.Status.ToString(),

                CanEdit = false,

                CanApprove = true,

                IsRejected =
                    proposal.Status == ProposalStatus.Rejected,

                RejectionReason =
                    latestRejection?.Comment,

                Attachments =
                    proposal.Attachments
                        .OrderByDescending(x => x.UploadedDate)
                        .Select(x =>
                            new AttachmentListItemViewModel
                            {
                                Id = x.Id,

                                OriginalFileName =
                                    x.OriginalFileName,

                                FileSize =
                                    x.FileSize,

                                UploadedDate =
                                    x.UploadedDate
                            })
                        .ToList()
            };
        }

        public async Task<ProposalAttachment?> GetAttachmentAsync(Guid id)
        {
            return await _context.ProposalAttachments
                .FirstOrDefaultAsync(x => x.Id == id);
        }
    }
}