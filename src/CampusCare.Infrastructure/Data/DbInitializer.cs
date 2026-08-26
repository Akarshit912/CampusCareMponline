using CampusCare.Core.Entities;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusCare.Infrastructure.Data
{
    public static class DbInitializer
    {
        public static async Task SeedAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
            var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

            // Ensure Database is Created
            await context.Database.EnsureCreatedAsync();

            // 1. Seed Roles
            string[] roles = new string[] { "Admin", "Manager", "Staff", "Student" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole(role));
                }
            }

            // 2. Seed Departments
            if (!await context.Departments.AnyAsync())
            {
                var departments = new List<Department>
                {
                    new Department { Name = "Information Technology", Code = "IT", Description = "Handles Wi-Fi, lab PCs, servers, network & digital infrastructure" },
                    new Department { Name = "Facility Maintenance", Code = "MAINT", Description = "Handles plumbing, electrical, furniture, building repairs" },
                    new Department { Name = "Hostel Administration", Code = "HOSTEL", Description = "Manages hostel room allocations, mess, facilities" },
                    new Department { Name = "Library Services", Code = "LIB", Description = "Handles library access, book requests, reading rooms" },
                    new Department { Name = "Campus Security", Code = "SEC", Description = "Manages campus safety, access gates, parking, ID cards" },
                    new Department { Name = "Transport & Fleet", Code = "TRANS", Description = "Manages college buses, shuttles, transport schedules" },
                    new Department { Name = "General Administration", Code = "ADMIN", Description = "Handles general student queries, fees, official requests" }
                };

                await context.Departments.AddRangeAsync(departments);
                await context.SaveChangesAsync();
            }

            var depts = await context.Departments.ToDictionaryAsync(d => d.Code, d => d.Id);
            int getDeptId(string code) => depts.TryGetValue(code, out int id) ? id : depts.Values.First();

            // 3. Seed Complaint Categories
            if (!await context.ComplaintCategories.AnyAsync())
            {
                var categories = new List<ComplaintCategory>
                {
                    new ComplaintCategory { Name = "IT / Wi-Fi", Description = "Internet connectivity, Wi-Fi passwords, signal issues", DefaultDepartmentId = getDeptId("IT") },
                    new ComplaintCategory { Name = "Classroom", Description = "Projector, AC, bench, whiteboards in lecture halls", DefaultDepartmentId = getDeptId("MAINT") },
                    new ComplaintCategory { Name = "Laboratory", Description = "Lab computers, equipment, software licenses", DefaultDepartmentId = getDeptId("IT") },
                    new ComplaintCategory { Name = "Hostel", Description = "Hostel room repair, hot water, mess quality", DefaultDepartmentId = getDeptId("HOSTEL") },
                    new ComplaintCategory { Name = "Library", Description = "Library books, e-resource access, quiet zones", DefaultDepartmentId = getDeptId("LIB") },
                    new ComplaintCategory { Name = "Maintenance", Description = "General plumbing, electrical fixtures, doors", DefaultDepartmentId = getDeptId("MAINT") },
                    new ComplaintCategory { Name = "Cleanliness", Description = "Sanitation, washrooms, garbage disposal", DefaultDepartmentId = getDeptId("MAINT") },
                    new ComplaintCategory { Name = "Transportation", Description = "College bus routes, driver behavior, delays", DefaultDepartmentId = getDeptId("TRANS") },
                    new ComplaintCategory { Name = "Security", Description = "Safety concerns, lost & found, gate issues", DefaultDepartmentId = getDeptId("SEC") },
                    new ComplaintCategory { Name = "Other", Description = "General complaints not covered in above categories", DefaultDepartmentId = getDeptId("ADMIN") }
                };

                await context.ComplaintCategories.AddRangeAsync(categories);
                await context.SaveChangesAsync();
            }

            // 4. Seed Seed Users
            string defaultPassword = "Password123!";

            var seedUsers = new[]
            {
                new { Email = "admin@college.com", Name = "System Administrator", Role = "Admin", DeptId = (int?)null },
                new { Email = "manager@college.com", Name = "IT Department Manager", Role = "Manager", DeptId = (int?)getDeptId("IT") },
                new { Email = "staff1@college.com", Name = "Alex Staff (IT Tech)", Role = "Staff", DeptId = (int?)getDeptId("IT") },
                new { Email = "staff2@college.com", Name = "Bob Staff (Maintenance)", Role = "Staff", DeptId = (int?)getDeptId("MAINT") },
                new { Email = "student1@college.com", Name = "John Student", Role = "Student", DeptId = (int?)null },
                new { Email = "student2@college.com", Name = "Emma Student", Role = "Student", DeptId = (int?)null }
            };

            foreach (var u in seedUsers)
            {
                var existingUser = await userManager.FindByEmailAsync(u.Email);
                if (existingUser == null)
                {
                    var newUser = new ApplicationUser
                    {
                        UserName = u.Email,
                        Email = u.Email,
                        EmailConfirmed = true,
                        FullName = u.Name,
                        DepartmentId = u.DeptId,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                    var result = await userManager.CreateAsync(newUser, defaultPassword);
                    if (result.Succeeded)
                    {
                        await userManager.AddToRoleAsync(newUser, u.Role);
                    }
                }
            }
        }
    }
}
