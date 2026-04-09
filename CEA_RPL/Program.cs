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
builder.Services.AddSingleton<IFileService, LocalFileService>();

// Cookie Authentication
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/Auth/Login";
        options.AccessDeniedPath = "/Auth/Login";
        options.ExpireTimeSpan = TimeSpan.FromHours(2);
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
        
        // Data Patch: Ensure existing NULLs are set to 'Pending'
        context.Database.ExecuteSqlRaw("UPDATE Applicants SET PaymentStatus = 'Pending' WHERE PaymentStatus IS NULL;");
    }
    catch { /* Ignore errors if already patched or schema locked */ }
    
    if (!context.Users.Any(u => u.Email == "admin@cearpl.gov.in"))
    {
        var admin = new User
        {
            Email = "admin@cearpl.gov.in",
            Mobile = "9999999999",
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
            Mobile = "8888888888",
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
