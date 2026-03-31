namespace CEA_RPL.Domain.Entities;

public class SoftwareSkill : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;

    public string SoftwareName { get; set; } = string.Empty;
    public string ProficiencyLevel { get; set; } = string.Empty;
}
