namespace CEA_RPL.Domain.Entities;

public class Award : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string ReceivedFrom { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? ProofPath { get; set; }
}
