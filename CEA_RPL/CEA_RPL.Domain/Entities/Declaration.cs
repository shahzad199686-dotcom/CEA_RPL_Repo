using System;

namespace CEA_RPL.Domain.Entities;

public class Declaration : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? Place { get; set; }
    public string? SignaturePath { get; set; }
}
