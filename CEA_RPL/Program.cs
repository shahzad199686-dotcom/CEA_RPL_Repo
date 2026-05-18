using Microsoft.EntityFrameworkCore;
using CEA_RPL.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using CEA_RPL.Application.Interfaces;
using CEA_RPL.Infrastructure.Services;
using CEA_RPL.Domain.Entities;

var builder = WebApplication.CreateBuilder(args);


// Add services to the container.
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("CeaRplDbConnectionString"),
        b => b.MigrationsAssembly("CEA_RPL.Infrastructure")));

// Register our Custom Services
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IOtpService, DbOtpService>();
builder.Services.AddScoped<IOtpSender, SmtpOtpSender>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddSingleton<IFileService, LocalFileService>();

var encKey = builder.Configuration["Encryption:SecretKey"] ?? "v9y$B&E)H@McQfTjWnZr4u7x!A%D*G-K";
var encIv = builder.Configuration["Encryption:IV"] ?? "8y/B?E(G+KbPeShV";
builder.Services.AddSingleton<IEncryptionService>(new EncryptionService(encKey, encIv));

// Add basic services required by Security Services
builder.Services.AddMemoryCache();
builder.Services.AddHttpContextAccessor();

// Cookie Authentication & Session Hardening
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
        
        // Secure Cookie Settings (HttpOnly, Secure, SameSite)
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
    });

// Configure Session securely
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(20);
    options.Cookie.HttpOnly = true;
    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = SameSiteMode.Strict;
});

builder.Services.AddAntiforgery(options => options.HeaderName = "X-CSRF-TOKEN");
builder.Services.AddControllersWithViews();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// Add Session middleware before Authentication
app.UseSession();

// Add Authentication before Authorization
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


// Seed Admin User
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<ApplicationDbContext>();

    // Database Patch: Add missing Finance columns if they don't exist
    try
    {
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'PaymentStatus') ALTER TABLE Applicants ADD PaymentStatus NVARCHAR(MAX) DEFAULT 'Pending';");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'FinanceRemarks') ALTER TABLE Applicants ADD FinanceRemarks NVARCHAR(MAX);");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'ProfessionalExperienceSectors') ALTER TABLE Applicants ADD ProfessionalExperienceSectors NVARCHAR(MAX);");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'TechnicalSubjectExpertise') ALTER TABLE Applicants ADD TechnicalSubjectExpertise NVARCHAR(MAX);");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'TotalExperience') ALTER TABLE Applicants ADD TotalExperience NVARCHAR(MAX);");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'HasRecognizedLab') ALTER TABLE Applicants ADD HasRecognizedLab NVARCHAR(MAX);");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'LaboratoryType') ALTER TABLE Applicants ADD LaboratoryType NVARCHAR(MAX);");
        context.Database.ExecuteSqlRaw("IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('Applicants') AND name = 'InHouseLabDetailsJson') ALTER TABLE Applicants ADD InHouseLabDetailsJson NVARCHAR(MAX);");
        
        // Data Patch: Expand Mobile column size for AES encryption
        context.Database.ExecuteSqlRaw("ALTER TABLE Applicants ALTER COLUMN Mobile NVARCHAR(255) NOT NULL;");
        
        // OTP Schema Patch
        context.Database.ExecuteSqlRaw(@"
            IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OtpRecords') AND name = 'IsVerified')
            BEGIN
                IF EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('OtpRecords') AND name = 'IsUsed')
                    EXEC sp_rename 'OtpRecords.IsUsed', 'IsVerified', 'COLUMN';
                ELSE
                    ALTER TABLE OtpRecords ADD IsVerified BIT NOT NULL DEFAULT 0;
            END");

        // Data Patch: Ensure existing NULLs are set to 'Pending'
        context.Database.ExecuteSqlRaw("UPDATE Applicants SET PaymentStatus = 'Pending' WHERE PaymentStatus IS NULL;");
    }
    catch { /* Ignore errors if already patched or schema locked */ }
    
    var encryptionService = services.GetRequiredService<IEncryptionService>();
    
    if (!context.Users.Any(u => u.Email == "admin@cearpl.gov.in"))
    {
        var admin = new User
        {
            Email = "admin@cearpl.gov.in",
            Mobile = encryptionService.Encrypt("9999999999"),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Admin@123"),
            IsEmailVerified = true,
            IsMobileVerified = true,
            Role = "Admin"
        };
        context.Users.Add(admin);
        context.SaveChanges();
    }

    if (!context.Users.Any(u => u.Email == "finance@cearpl.gov.in"))
    {
        var finance = new User
        {
            Email = "finance@cearpl.gov.in",
            Mobile = encryptionService.Encrypt("8888888888"),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword("Finance@123"),
            IsEmailVerified = true,
            IsMobileVerified = true,
            Role = "Finance"
        };
        context.Users.Add(finance);
        context.SaveChanges();
    }
}

app.Run();
