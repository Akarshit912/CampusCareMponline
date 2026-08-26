using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using CampusCare.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusCare.Web.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly IComplaintRepository _complaintRepository;
        private readonly IEscalationService _escalationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApplicationDbContext _context;

        public AdminController(
            IComplaintRepository complaintRepository,
            IEscalationService escalationService,
            UserManager<ApplicationUser> userManager,
            RoleManager<IdentityRole> roleManager,
            ApplicationDbContext context)
        {
            _complaintRepository = complaintRepository;
            _escalationService = escalationService;
            _userManager = userManager;
            _roleManager = roleManager;
            _context = context;
        }

        [HttpGet]
        // This action retrieves all complaints, calculates various KPIs (Key Performance Indicators), and filters
        // the complaints based on the provided parameters (status, departmentId, categoryId, search). It then passes the data to the view for display.
        public async Task<IActionResult> Index(string? status, int? departmentId, int? categoryId, string? search)
        {
            var allComplaints = await _complaintRepository.GetAllAsync();
            var complaintList = allComplaints.ToList();

            // Calculate KPIs (Key performance indicators-it willcalculate how many complaints are in each status)
            int total = complaintList.Count;
            int pending = complaintList.Count(c => c.Status == ComplaintStatus.Submitted || c.Status == ComplaintStatus.Assigned);
            int inProgress = complaintList.Count(c => c.Status == ComplaintStatus.InProgress);
            int escalated = complaintList.Count(c => c.IsEscalated || c.Status == ComplaintStatus.Escalated);
            int resolved = complaintList.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed);

            // Average resolution time calculation in hours
            var resolvedComplaints = complaintList.Where(c => c.ResolvedAt.HasValue).ToList();
            double avgResHours = resolvedComplaints.Any()
                ? Math.Round(resolvedComplaints.Average(c => (c.ResolvedAt!.Value - c.CreatedAt).TotalHours), 1)
                : 0.0;

            // Average feedback rating
            var feedbacks = await _context.Feedbacks.ToListAsync();
            double avgRating = feedbacks.Any() ? Math.Round(feedbacks.Average(f => f.Rating), 1) : 5.0;

            // Department Stats
            var departments = await _context.Departments.ToListAsync();
            var deptStats = departments.Select(d => new DepartmentStatItem
            {
                DepartmentName = d.Name,
                TotalCount = complaintList.Count(c => c.DepartmentId == d.Id),
                ResolvedCount = complaintList.Count(c => c.DepartmentId == d.Id && (c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed)),
                EscalatedCount = complaintList.Count(c => c.DepartmentId == d.Id && (c.IsEscalated || c.Status == ComplaintStatus.Escalated))
            }).ToList();

            // Category Stats
            var categories = await _context.ComplaintCategories.ToListAsync();
            var catStats = categories.Select(c => new CategoryStatItem
            {
                CategoryName = c.Name,
                TotalCount = complaintList.Count(comp => comp.CategoryId == c.Id)
            }).ToList();

            // Filter logic for recent list
            var filtered = complaintList;
            if (!string.IsNullOrWhiteSpace(search))
            {
                search = search.Trim().ToLower();
                filtered = filtered.Where(c => c.ComplaintNumber.ToLower().Contains(search) || c.Title.ToLower().Contains(search) || c.Location.ToLower().Contains(search)).ToList();
            }
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<ComplaintStatus>(status, out var statusEnum))
            {
                filtered = filtered.Where(c => c.Status == statusEnum).ToList();
            }
            if (departmentId.HasValue && departmentId.Value > 0)
            {
                filtered = filtered.Where(c => c.DepartmentId == departmentId.Value).ToList();
            }
            if (categoryId.HasValue && categoryId.Value > 0)
            {
                filtered = filtered.Where(c => c.CategoryId == categoryId.Value).ToList();
            }

            ViewBag.Departments = new SelectList(departments, "Id", "Name", departmentId);
            ViewBag.Categories = new SelectList(categories, "Id", "Name", categoryId);
            ViewBag.SelectedStatus = status;
            ViewBag.Search = search;

            var viewModel = new AdminDashboardViewModel
            {
                TotalComplaints = total,
                PendingComplaints = pending,
                InProgressComplaints = inProgress,
                EscalatedComplaints = escalated,
                ResolvedComplaints = resolved,
                AverageResolutionHours = avgResHours,
                AverageFeedbackRating = avgRating,
                DepartmentStats = deptStats,
                CategoryStats = catStats,
                RecentComplaints = filtered
            };

            return View(viewModel);
        }

        [HttpGet]
        // This action retrieves all users along with their associated departments and roles, and passes them to the view for display.
        public async Task<IActionResult> Users()
        {
            var users = await _userManager.Users.Include(u => u.Department).ToListAsync();
            var userItems = new List<UserManagementItem>();

            foreach (var user in users)
            {
                var roles = await _userManager.GetRolesAsync(user);
                userItems.Add(new UserManagementItem
                {
                    UserId = user.Id,
                    FullName = user.FullName,
                    Email = user.Email ?? string.Empty,
                    Role = roles.FirstOrDefault() ?? "Student",
                    DepartmentName = user.Department?.Name,
                    IsActive = user.IsActive,
                    CreatedAt = user.CreatedAt
                });
            }

            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name");
            return View(userItems);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // This action creates a new staff member (either Manager or Staff) and assigns them to a department.
        public async Task<IActionResult> CreateStaff(CreateStaffViewModel model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    TempData["ErrorMessage"] = $"User with email {model.Email} already exists.";
                    return RedirectToAction(nameof(Users));
                }

                var staffUser = new ApplicationUser
                {
                    UserName = model.Email,
                    Email = model.Email,
                    FullName = model.FullName,
                    DepartmentId = model.DepartmentId,
                    EmailConfirmed = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                };

                var result = await _userManager.CreateAsync(staffUser, model.Password);
                if (result.Succeeded)
                {
                    string targetRole = (model.Role == "Manager") ? "Manager" : "Staff";
                    await _userManager.AddToRoleAsync(staffUser, targetRole);
                    TempData["SuccessMessage"] = $"{targetRole} member '{model.FullName}' added successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = string.Join("; ", result.Errors.Select(e => e.Description));
                }
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // This action toggles the active status of a user account based on the provided user ID.
        public async Task<IActionResult> ToggleUserStatus(string id)
        {
            var user = await _userManager.FindByIdAsync(id);
            if (user != null)
            {
                user.IsActive = !user.IsActive;
                await _userManager.UpdateAsync(user);
                TempData["SuccessMessage"] = $"Account status for {user.Email} updated.";
            }
            return RedirectToAction(nameof(Users));
        }

        [HttpGet]
        // This action retrieves all departments along with their associated staff members and categories, and passes them to the view for display.
        public async Task<IActionResult> Departments()
        {
            var departments = await _context.Departments
                .Include(d => d.StaffMembers)
                .Include(d => d.Categories)
                .ToListAsync();
            return View(departments);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // This action creates a new department and saves it to the database.
        public async Task<IActionResult> CreateDepartment(CreateDepartmentViewModel model)
        {
            if (ModelState.IsValid)
            {
                var dept = new Department
                {
                    Name = model.Name,
                    Code = model.Code.ToUpper(),
                    Description = model.Description
                };
                _context.Departments.Add(dept);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Department '{model.Name}' created successfully.";
            }
            return RedirectToAction(nameof(Departments));
        }

        [HttpGet]
        // This action retrieves all complaint categories along with their associated default departments and passes them to the view for display.
        public async Task<IActionResult> Categories()
        {
            var categories = await _context.ComplaintCategories
                .Include(c => c.DefaultDepartment)
                .ToListAsync();

            ViewBag.Departments = new SelectList(await _context.Departments.ToListAsync(), "Id", "Name");
            return View(categories);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // This action creates a new complaint category and associates it with a default department.
        public async Task<IActionResult> CreateCategory(CreateCategoryViewModel model)
        {
            if (ModelState.IsValid)
            {
                var cat = new ComplaintCategory
                {
                    Name = model.Name,
                    Description = model.Description,
                    DefaultDepartmentId = model.DefaultDepartmentId
                };
                _context.ComplaintCategories.Add(cat);
                await _context.SaveChangesAsync();
                TempData["SuccessMessage"] = $"Category '{model.Name}' created successfully.";
            }
            return RedirectToAction(nameof(Categories));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // This action triggers the automated SLA escalation check for complaints that have been overdue for more than 48 hours.
        public async Task<IActionResult> RunEscalationCheck()
        {
            int escalatedCount = await _escalationService.ProcessOverdueComplaintsAsync(48);
            TempData["SuccessMessage"] = $"Automated SLA escalation complete. {escalatedCount} overdue complaints escalated.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        // This action deletes a specific complaint and its associated history from the database.
        public async Task<IActionResult> DeleteComplaint(int id)
        {
            var complaint = await _complaintRepository.GetByIdAsync(id);
            if (complaint == null)
            {
                TempData["ErrorMessage"] = $"Complaint ID {id} not found.";
                return RedirectToAction(nameof(Index));
            }

            string trackingNo = complaint.ComplaintNumber;
            await _complaintRepository.DeleteAsync(id);
            TempData["SuccessMessage"] = $"Complaint '{trackingNo}' and associated history have been deleted from the database.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]

        // This action removes complaints that are older than a specified number of days and have a status of Closed, Resolved, or Rejected.
        public async Task<IActionResult> PurgePastComplaints(int daysOlderThan = 30)
        {
            var cutoff = DateTime.UtcNow.AddDays(-daysOlderThan);
            var complaintsToPurge = await _context.Complaints
                .Where(c => (c.Status == ComplaintStatus.Closed || c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Rejected)
                            && c.CreatedAt <= cutoff)
                .ToListAsync();

            int count = complaintsToPurge.Count;
            if (count > 0)
            {
                foreach (var c in complaintsToPurge)
                {
                    await _complaintRepository.DeleteAsync(c.Id);
                }
                TempData["SuccessMessage"] = $"Successfully purged {count} past records older than {daysOlderThan} days.";
            }
            else
            {
                TempData["InfoMessage"] = $"No past closed/resolved records found older than {daysOlderThan} days.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
