using Microsoft.EntityFrameworkCore;
using CEA_RPL.Domain.Entities;

namespace CEA_RPL.Infrastructure.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<User> Users => Set<User>();
    public DbSet<Applicant> Applicants => Set<Applicant>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<ProfessionalExperience> ProfessionalExperiences => Set<ProfessionalExperience>();
    public DbSet<ProjectExperience> ProjectExperiences => Set<ProjectExperience>();
    public DbSet<OtpRecord> OtpRecords => Set<OtpRecord>();
    public DbSet<Award> Awards => Set<Award>();
    public DbSet<CertificationTraining> CertificationTrainings => Set<CertificationTraining>();
    public DbSet<Membership> Memberships => Set<Membership>();
    public DbSet<PaperPublished> PaperPublisheds => Set<PaperPublished>();
    public DbSet<SoftwareSkill> SoftwareSkills => Set<SoftwareSkill>();
    public DbSet<UploadReport> UploadReports => Set<UploadReport>();
    public DbSet<PaymentDetail> PaymentDetails => Set<PaymentDetail>();
    public DbSet<Declaration> Declarations => Set<Declaration>();
    public DbSet<OtherEnclosure> OtherEnclosures => Set<OtherEnclosure>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        
        // User
        modelBuilder.Entity<User>(e =>
        {
            e.HasKey(u => u.Id);
            e.HasIndex(u => u.Email).IsUnique();
            e.HasIndex(u => u.Mobile).IsUnique();
            
            // 1-to-1 User <-> Applicant
            e.HasOne(u => u.Applicant)
             .WithOne(a => a.User)
             .HasForeignKey<Applicant>(a => a.UserId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Applicant
        modelBuilder.Entity<Applicant>(e =>
        {
            e.Property(a => a.FullName).IsRequired().HasMaxLength(255);
            e.Property(a => a.Email).IsRequired().HasMaxLength(255); 
            e.Property(a => a.Mobile).IsRequired().HasMaxLength(255);

            // 1-to-1 Applicant <-> Declaration
            e.HasOne(a => a.Declaration)
             .WithOne(d => d.Applicant)
             .HasForeignKey<Declaration>(d => d.ApplicantId)
             .OnDelete(DeleteBehavior.Cascade);
        });
        
        // Education
        modelBuilder.Entity<Education>(e => 
        {
            e.HasOne(ed => ed.Applicant)
             .WithMany(a => a.Educations)
             .HasForeignKey(ed => ed.ApplicantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Professional Experience
        modelBuilder.Entity<ProfessionalExperience>(e => 
        {
            e.HasOne(pe => pe.Applicant)
             .WithMany(a => a.ProfessionalExperiences)
             .HasForeignKey(pe => pe.ApplicantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // Project Experience
        modelBuilder.Entity<ProjectExperience>(e => 
        {
            e.HasOne(pe => pe.Applicant)
             .WithMany(a => a.ProjectExperiences)
             .HasForeignKey(pe => pe.ApplicantId)
             .OnDelete(DeleteBehavior.Cascade);
        });

        // PaperPublished
        modelBuilder.Entity<PaperPublished>(e =>
        {
            e.ToTable("PaperPublishedEntries");
            e.HasOne(p => p.Applicant)
             .WithMany(a => a.PaperPublisheds)
             .HasForeignKey(p => p.ApplicantId)
             .OnDelete(DeleteBehavior.Cascade);
        });
    }
}
