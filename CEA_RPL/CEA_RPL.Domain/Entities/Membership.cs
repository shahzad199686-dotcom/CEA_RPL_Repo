namespace CEA_RPL.Domain.Entities;

public class Membership : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string ObtainedFrom { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Duration { get; set; } = string.Empty;
    public string? ProofPath { get; set; }
}
