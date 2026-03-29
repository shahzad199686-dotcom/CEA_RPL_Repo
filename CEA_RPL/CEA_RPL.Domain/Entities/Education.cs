namespace CEA_RPL.Domain.Entities;

public class Education : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;
    
    public string Degree { get; set; } = string.Empty;
    public string Discipline { get; set; } = string.Empty;
    public string Institution { get; set; } = string.Empty;
    public int Year { get; set; }
    
    public string? CertificatePath { get; set; }
}
