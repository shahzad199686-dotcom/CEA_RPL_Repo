using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CEA_RPL.Domain.Entities;

public enum ApplicationStatus
{
    Draft,
    Submitted,
    InReview,
    ClarificationRequested,
    Approved,
    Rejected
}

public class REAApplication
{
    public int Id { get; set; }
    public string? UserId { get; set; }
    public string ApplicationNo { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
    public DateTime LastUpdatedDate { get; set; } = DateTime.UtcNow;
    public ApplicationStatus Status { get; set; } = ApplicationStatus.Draft;
    public string? PaymentStatus { get; set; } = "Pending";
    public string? FinanceRemarks { get; set; }

    // Navigation Properties
    public OrganizationDetail? OrganizationDetail { get; set; }
    public ContactDetail? ContactDetail { get; set; }
    public ICollection<CategorySelection> Categories { get; set; } = new List<CategorySelection>();
    public ICollection<OperationalState> States { get; set; } = new List<OperationalState>();
    public ICollection<FinancialDetail> FinancialDetails { get; set; } = new List<FinancialDetail>();
    public ICollection<CEADetail> CEADetails { get; set; } = new List<CEADetail>();
    public LaboratoryInfo? LaboratoryInfo { get; set; }
    public ICollection<LaboratoryDetail> LaboratoryDetails { get; set; } = new List<LaboratoryDetail>();
    public ICollection<HardwareDetail> HardwareDetails { get; set; } = new List<HardwareDetail>();
    public ICollection<SoftwareDetail> SoftwareDetails { get; set; } = new List<SoftwareDetail>();
    public ICollection<AuditExperience> AuditExperiences { get; set; } = new List<AuditExperience>();
    public ReaPaymentDetail? PaymentDetail { get; set; }
    public ReaDeclaration? Declaration { get; set; }
    public Checklist? Checklist { get; set; }
    public ICollection<UploadedDocument> Documents { get; set; } = new List<UploadedDocument>();
    public ICollection<StatusHistory> StatusHistories { get; set; } = new List<StatusHistory>();
    public ICollection<AdminRemark> AdminRemarks { get; set; } = new List<AdminRemark>();
}

public class OrganizationDetail
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string EntityType { get; set; } = string.Empty;
    public DateTime YearOfEstablishment { get; set; }
    public string IncorpNo { get; set; } = string.Empty;
    public string RegisteredAddress { get; set; } = string.Empty;
    public string? CorrespondenceAddress { get; set; }
    public string? Website { get; set; }
    public string PAN { get; set; } = string.Empty;
    public string? GST { get; set; }
    
    // Root paths for documents
    public string? IncorpCertPath { get; set; }
    public string? PANDocPath { get; set; }
    public string? GSTDocPath { get; set; }
}

public class ContactDetail
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string SignatoryName { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string? AlternatePhone { get; set; }
    public string Email { get; set; } = string.Empty;
}

public class CategorySelection
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public int CategoryId { get; set; } // 1, 2, 3, 4
}

public class OperationalState
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string StateName { get; set; } = string.Empty;
}

public class FinancialDetail
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Year { get; set; } = string.Empty; // e.g. "FY 2022-23"
    public decimal Turnover { get; set; }
    public string? TurnoverStmtPath { get; set; }
    public decimal EnvIncome { get; set; }
    public string? EnvStmtPath { get; set; }
}

public class CEADetail
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string CertNo { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string? ProofOfEmploymentPath { get; set; }
}

public class LaboratoryInfo
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public bool HasLab { get; set; }
    public string? LabType { get; set; } // In-house / Empanelled
}

public class LaboratoryDetail
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string RecognitionTypes { get; set; } = string.Empty; // Semicolon separated
    public string? CertificatePath { get; set; }
}

public class HardwareDetail
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Quantity { get; set; }
    public string? CalibrationStatus { get; set; }
    public string? Purpose { get; set; }
}

public class SoftwareDetail
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Type { get; set; }
    public string? Version { get; set; }
    public string? LicenceType { get; set; }
    public string? Purpose { get; set; }
}

public class AuditExperience
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string ProjectName { get; set; } = string.Empty;
    public string ClientName { get; set; } = string.Empty;
    public int Year { get; set; }
    public string? Scope { get; set; }
    public string? ReportPath { get; set; }
}

public class ReaPaymentDetail
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public decimal Amount { get; set; }
    public DateTime PaymentDate { get; set; }
    public string UTR { get; set; } = string.Empty;
    public string? ReceiptPath { get; set; }
}

public class ReaDeclaration
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string? SignaturePath { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Designation { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string Place { get; set; } = string.Empty;
}

public class Checklist
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public bool IncorpCert { get; set; }
    public bool PANGST { get; set; }
    public bool CEACerts { get; set; }
    public bool EmploymentProof { get; set; }
    public bool LabCerts { get; set; }
    public bool FinancialStmts { get; set; }
    public bool AuditReports { get; set; }
    public bool PaymentReceipt { get; set; }
    public bool OtherDocs { get; set; }
    public string? OtherDocsPath { get; set; }
}

public class StatusHistory
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public ApplicationStatus Status { get; set; }
    public DateTime ChangedDate { get; set; } = DateTime.UtcNow;
    public string? ChangedBy { get; set; }
}

public class AdminRemark
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string Remark { get; set; } = string.Empty;
    public string? AdminId { get; set; }
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public class UploadedDocument
{
    public int Id { get; set; }
    public int ApplicationId { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public string FileType { get; set; } = string.Empty; // e.g. "PassportPhoto", "PANDoc"
    public DateTime UploadedDate { get; set; } = DateTime.UtcNow;
}
