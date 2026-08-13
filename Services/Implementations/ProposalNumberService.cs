using BuildingProposalSystem.Data;
using BuildingProposalSystem.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BuildingProposalSystem.Services.Implementations
{
    public class ProposalNumberService : IProposalNumberService
    {
        private readonly ApplicationDbContext _context;

        public ProposalNumberService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<string> GenerateNumberAsync()
        {
            var today = DateTime.UtcNow;
            var prefix = $"PGD-{today:yyyyMMdd}-";

            // Hitung berapa proposal yang SUDAH dibuat hari ini, buat nentuin nomor urut berikutnya.
            var countToday = await _context.BuildingProposals
                .CountAsync(p => p.ProposalNumber.StartsWith(prefix));

            var sequence = countToday + 1;
            return $"{prefix}{sequence:D4}"; // contoh: PGD-20260813-0001
        }
    }
}