namespace CEA_RPL.Domain.Entities;

public class Publication : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string Place { get; set; } = string.Empty;
    public int Year { get; set; }
    public string Role { get; set; } = string.Empty;
    public string? ProofPath { get; set; }
}
