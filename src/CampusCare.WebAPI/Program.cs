using CampusCare.Core.Entities;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using CampusCare.Infrastructure.Repositories;
using CampusCare.Infrastructure.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

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
    options.Password.RequiredLength = 6;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Register Services & CORS
builder.Services.AddHttpClient();
builder.Services.AddScoped<IComplaintRepository, ComplaintRepository>();
builder.Services.AddScoped<IAIService, AIService>();
builder.Services.AddScoped<INotificationService, NotificationService>();
builder.Services.AddScoped<IEscalationService, EscalationService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

builder.Services.AddControllers();

// 4. Configure Swagger / OpenAPI Generator
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// 5. Middleware Pipeline & Swagger UI
if (app.Environment.IsDevelopment() || true)
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "CampusCare Web API v1");
        c.RoutePrefix = string.Empty; // Swagger UI at root URL (e.g. http://localhost:5001/)
    });
}

app.UseCors("AllowAll");
app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
