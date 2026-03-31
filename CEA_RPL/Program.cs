using Microsoft.EntityFrameworkCore;
using CEA_RPL.Infrastructure.Data;
using Microsoft.AspNetCore.Authentication.Cookies;
using CEA_RPL.Application.Interfaces;
using CEA_RPL.Infrastructure.Services;

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


app.Run();
