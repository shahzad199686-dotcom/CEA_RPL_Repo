namespace CEA_RPL.Domain.Entities;

public class OtpRecord : BaseEntity
{
    public string ContactKey { get; set; } = string.Empty;
    public string OtpCode { get; set; } = string.Empty;
    public DateTime ExpiryTime { get; set; }
    public bool IsUsed { get; set; }
}
