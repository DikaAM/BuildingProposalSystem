namespace BuildingProposalSystem.Services.Interfaces
{
    public interface IRecaptchaService
    {
        Task<bool> VerifyAsync(string recaptchaToken);
    }
}
