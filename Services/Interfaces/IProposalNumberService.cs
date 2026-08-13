namespace BuildingProposalSystem.Services.Interfaces
{
    public interface IProposalNumberService
    {
        Task<string> GenerateNumberAsync();
    }
}