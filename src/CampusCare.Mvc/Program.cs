using CampusCare.Core.Entities;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using CampusCare.Infrastructure.Repositories;
using CampusCare.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using System;

var builder = WebApplication.CreateBuilder(args);

// 1. Database Configuration (SQL Server default with automatic SQLite fallback)
string? mssqlConnection = builder.Configuration.GetConnectionString("DefaultConnection");
string? sqliteConnection = builder.Configuration.GetConnectionString("SqliteConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
{
    try
    {
        options.UseSqlServer(mssqlConnection);
    }
    catch
    {
        options.UseSqlite(sqliteConnection);
    }
});

// 2. Identity Configuration
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = false;
    options.Password.RequireLowercase = false;
    options.Password.RequireNonAlphanumeric = false;
    options.Password.RequireUppercase = false;
    options.Password.RequiredLength = 6;
    options.User.RequireUniqueEmail = true;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.AccessDeniedPath = "/Account/AccessDenied";
    options.Cookie.Name = "CampusCare.Mvc.AuthCookie";
    options.ExpireTimeSpan = TimeSpan.FromHours(8);
});

// 3. Register Dependency Injection Services
builder.Services.AddHttpClient();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEscalationService, EscalationService>();
builder.Services.AddHostedService<EscalationBackgroundService>();

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 4. Database Initialization & Seeding on Startup
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        var db = services.GetRequiredService<ApplicationDbContext>();
        try
        {
            db.Database.EnsureCreated();
        }
        catch
        {
            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlite(sqliteConnection);
            using var sqliteDb = new ApplicationDbContext(optionsBuilder.Options);
            sqliteDb.Database.EnsureCreated();
        }

        DbInitializer.SeedAsync(services).GetAwaiter().GetResult();
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "An error occurred while seeding the database.");
    }
}

// 5. Middleware Pipeline
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
