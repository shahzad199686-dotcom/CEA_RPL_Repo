using Microsoft.EntityFrameworkCore;
using CEA_RPL.Domain.Entities;

namespace CEA_RPL.Infrastructure.Data;

public class ReaDbContext : DbContext
{
    public ReaDbContext(DbContextOptions<ReaDbContext> options)
        : base(options)
    {
    }

    public DbSet<REAApplication> Applications { get; set; }
    public DbSet<OrganizationDetail> OrganizationDetails { get; set; }
    public DbSet<ContactDetail> ContactDetails { get; set; }
    public DbSet<CategorySelection> CategorySelections { get; set; }
    public DbSet<OperationalState> OperationalStates { get; set; }
    public DbSet<FinancialDetail> FinancialDetails { get; set; }
    public DbSet<CEADetail> CEADetails { get; set; }
    public DbSet<LaboratoryInfo> LaboratoryInfos { get; set; }
    public DbSet<LaboratoryDetail> LaboratoryDetails { get; set; }
    public DbSet<HardwareDetail> HardwareDetails { get; set; }
    public DbSet<SoftwareDetail> SoftwareDetails { get; set; }
    public DbSet<AuditExperience> AuditExperiences { get; set; }
    public DbSet<ReaPaymentDetail> PaymentDetails { get; set; }
    public DbSet<ReaDeclaration> Declarations { get; set; }
    public DbSet<Checklist> Checklists { get; set; }
    public DbSet<UploadedDocument> UploadedDocuments { get; set; }
    public DbSet<StatusHistory> StatusHistories { get; set; }
    public DbSet<AdminRemark> AdminRemarks { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("rea");
        base.OnModelCreating(modelBuilder);

        // Configure relationships matching REA
        modelBuilder.Entity<REAApplication>()
            .HasOne(a => a.OrganizationDetail)
            .WithOne()
            .HasForeignKey<OrganizationDetail>(o => o.ApplicationId);

        modelBuilder.Entity<REAApplication>()
            .HasOne(a => a.ContactDetail)
            .WithOne()
            .HasForeignKey<ContactDetail>(c => c.ApplicationId);

        modelBuilder.Entity<REAApplication>()
            .HasOne(a => a.LaboratoryInfo)
            .WithOne()
            .HasForeignKey<LaboratoryInfo>(l => l.ApplicationId);

        modelBuilder.Entity<REAApplication>()
            .HasOne(a => a.PaymentDetail)
            .WithOne()
            .HasForeignKey<ReaPaymentDetail>(p => p.ApplicationId);

        modelBuilder.Entity<REAApplication>()
            .HasOne(a => a.Declaration)
            .WithOne()
            .HasForeignKey<ReaDeclaration>(d => d.ApplicationId);

        modelBuilder.Entity<REAApplication>()
            .HasOne(a => a.Checklist)
            .WithOne()
            .HasForeignKey<Checklist>(c => c.ApplicationId);

        // Precision for decimals
        modelBuilder.Entity<FinancialDetail>()
            .Property(f => f.Turnover)
            .HasPrecision(18, 2);

        modelBuilder.Entity<FinancialDetail>()
            .Property(f => f.EnvIncome)
            .HasPrecision(18, 2);

        modelBuilder.Entity<ReaPaymentDetail>()
            .Property(p => p.Amount)
            .HasPrecision(18, 2);
    }
}
