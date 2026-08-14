using BuildingProposalSystem.Models.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace BuildingProposalSystem.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ApplicationUser>(entity =>
            {
                entity.Property(e => e.FullName).HasMaxLength(150);
            });
            modelBuilder.Entity<BuildingProposal>(entity =>
            {
                entity.Property(e => e.ProposalNumber).HasMaxLength(30);
                entity.Property(e => e.BuildingName).HasMaxLength(200);
                entity.Property(e => e.Latitude).HasColumnType("decimal(10,7)");
                entity.Property(e => e.Longitude).HasColumnType("decimal(10,7)");
                entity.Property(e => e.EstimatedCost).HasColumnType("decimal(18,2)");
                entity.HasIndex(e => e.ProposalNumber).IsUnique();
                entity.HasIndex(e => e.Status);
                entity.Property(e => e.CreatedBy).HasMaxLength(450);
                entity.Property(e => e.UpdatedBy).HasMaxLength(450);
                entity.HasIndex(e => e.CurrentApproverRole);
                entity.Property(e => e.Address).HasMaxLength(500);

                entity.HasOne(e => e.Creator)
                .WithMany()
                .HasForeignKey(e => e.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.Updater)
                .WithMany()
                .HasForeignKey(e => e.UpdatedBy)
                .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ProposalAttachment>(entity =>
            {
                entity.Property(e => e.OriginalFileName).HasMaxLength(255);
                entity.Property(e => e.StoredFileName).HasMaxLength(255);
                entity.Property(e => e.FileExtension).HasMaxLength(20);
                entity.Property(e => e.ContentType).HasMaxLength(100);
                entity.Property(e => e.UploadedBy).HasMaxLength(450);

                entity.HasOne(e => e.Proposal)
                      .WithMany(p => p.Attachments)
                      .HasForeignKey(e => e.ProposalId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Uploader)
                      .WithMany()
                      .HasForeignKey(e => e.UploadedBy)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            modelBuilder.Entity<ApprovalHistory>(entity =>
            {
                entity.Property(e => e.Action)
                    .HasMaxLength(20);

                entity.Property(e => e.Comment)
                    .HasMaxLength(1000);

                entity.Property(e => e.ApprovedBy)
                    .HasMaxLength(450);

                entity.HasOne(e => e.Proposal)
                    .WithMany(p => p.ApprovalHistories)
                    .HasForeignKey(e => e.ProposalId)
                    .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.Approver)
                    .WithMany()
                    .HasForeignKey(e => e.ApprovedBy)
                    .OnDelete(DeleteBehavior.Restrict);
            });
        }

        public DbSet<BuildingProposal> BuildingProposals { get; set; }
        public DbSet<ProposalAttachment> ProposalAttachments { get; set; }
        public DbSet<ApprovalHistory> ApprovalHistories { get; set; }

    }
}
