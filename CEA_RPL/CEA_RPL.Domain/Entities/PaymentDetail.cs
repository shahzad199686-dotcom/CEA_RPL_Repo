using System;

namespace CEA_RPL.Domain.Entities;

public class PaymentDetail : BaseEntity
{
    public int ApplicantId { get; set; }
    public virtual Applicant Applicant { get; set; } = null!;

    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string UtrNumber { get; set; } = string.Empty;
    public string? ReceiptPath { get; set; }
}
