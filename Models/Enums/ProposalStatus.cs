namespace BuildingProposalSystem.Models.Enums
{
    public enum ProposalStatus : byte
    {
        Draft = 0,
        WaitingManagerApproval = 1,
        WaitingDirectorApproval = 2,
        Approved = 3,
        Rejected = 4,
        Submitted = 5
    }
}