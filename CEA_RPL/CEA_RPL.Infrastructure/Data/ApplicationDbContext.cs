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
            e.Property(a => a.Mobile).IsRequired().HasMaxLength(15);
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
    }
}
