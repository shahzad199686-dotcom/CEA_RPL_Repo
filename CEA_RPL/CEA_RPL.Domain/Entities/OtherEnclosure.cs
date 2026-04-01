using System;

namespace CEA_RPL.Domain.Entities;

public class OtherEnclosure : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;

    public string FilePath { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
}
