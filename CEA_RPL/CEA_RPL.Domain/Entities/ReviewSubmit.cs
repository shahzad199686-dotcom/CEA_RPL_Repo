using System;

namespace CEA_RPL.Domain.Entities;

public class ReviewSubmit : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;

    public DateTime SubmissionTime { get; set; } = DateTime.UtcNow;
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string Status { get; set; } = "Submitted";
}
