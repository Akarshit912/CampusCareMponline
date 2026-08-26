using CampusCare.Core.DTOs;
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
    [Authorize(Roles = "Manager,Admin")]
    public class ManagerController : Controller
    {
        private readonly IComplaintRepository _complaintRepository;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public ManagerController(
            IComplaintRepository complaintRepository,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context)
        {
            _complaintRepository = complaintRepository;
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index(string? filter = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            int departmentId = user.DepartmentId ?? 1;
            var department = await _context.Departments.FindAsync(departmentId);

            var deptComplaints = await _complaintRepository.GetByDepartmentIdAsync(departmentId);
            var complaintList = deptComplaints.ToList();

            // Staff workload calculation
            var staffMembers = await _context.Users
                .Where(u => u.DepartmentId == departmentId && u.IsActive)
                .ToListAsync();

            var staffWorkload = new List<StaffWorkloadItem>();
            foreach (var staff in staffMembers)
            {
                if (await _userManager.IsInRoleAsync(staff, "Staff"))
                {
                    var staffComplaints = complaintList.Where(c => c.AssignedStaffId == staff.Id);
                    staffWorkload.Add(new StaffWorkloadItem
                    {
                        StaffId = staff.Id,
                        StaffName = staff.FullName,
                        Email = staff.Email ?? string.Empty,
                        ActiveAssignedCount = staffComplaints.Count(c => c.Status == ComplaintStatus.Assigned || c.Status == ComplaintStatus.InProgress || c.Status == ComplaintStatus.Escalated),
                        ResolvedCount = staffComplaints.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed)
                    });
                }
            }

            // Filter logic
            var displayComplaints = complaintList;
            if (filter == "unassigned") displayComplaints = complaintList.Where(c => string.IsNullOrEmpty(c.AssignedStaffId)).ToList();
            else if (filter == "escalated") displayComplaints = complaintList.Where(c => c.IsEscalated || c.Status == ComplaintStatus.Escalated).ToList();
            else if (filter == "inprogress") displayComplaints = complaintList.Where(c => c.Status == ComplaintStatus.InProgress).ToList();
            else if (filter == "resolved") displayComplaints = complaintList.Where(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed).ToList();

            var viewModel = new ManagerDashboardViewModel
            {
                DepartmentName = department?.Name ?? "Department",
                TotalDepartmentComplaints = complaintList.Count,
                UnassignedCount = complaintList.Count(c => string.IsNullOrEmpty(c.AssignedStaffId)),
                InProgressCount = complaintList.Count(c => c.Status == ComplaintStatus.InProgress),
                EscalatedCount = complaintList.Count(c => c.IsEscalated || c.Status == ComplaintStatus.Escalated),
                ResolvedCount = complaintList.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed),
                Complaints = displayComplaints,
                StaffWorkload = staffWorkload
            };

            ViewBag.CurrentFilter = filter;
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Assign(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var complaint = await _complaintRepository.GetByIdAsync(id);
            if (complaint == null) return NotFound();

            int departmentId = user.DepartmentId ?? complaint.DepartmentId;

            var staffList = await _context.Users
                .Where(u => u.DepartmentId == departmentId && u.IsActive)
                .Select(u => new { u.Id, Name = $"{u.FullName} ({u.Email})" })
                .ToListAsync();

            ViewBag.StaffList = new SelectList(staffList, "Id", "Name", complaint.AssignedStaffId);

            var categories = await _context.ComplaintCategories.ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name", complaint.CategoryId);

            var viewModel = new AssignStaffViewModel
            {
                ComplaintId = complaint.Id,
                ComplaintNumber = complaint.ComplaintNumber,
                Title = complaint.Title,
                SelectedStaffId = complaint.AssignedStaffId ?? string.Empty,
                Priority = complaint.Priority,
                CategoryId = complaint.CategoryId
            };

            ViewBag.Complaint = complaint;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignStaffViewModel model)
        {
            var manager = await _userManager.GetUserAsync(User);
            if (manager == null) return Challenge();

            var complaint = await _complaintRepository.GetByIdAsync(model.ComplaintId);
            if (complaint == null) return NotFound();

            var assignedStaff = await _userManager.FindByIdAsync(model.SelectedStaffId);
            if (assignedStaff == null)
            {
                TempData["ErrorMessage"] = "Selected staff member does not exist.";
                return RedirectToAction(nameof(Assign), new { id = model.ComplaintId });
            }

            var oldStaff = complaint.AssignedStaff?.FullName ?? "Unassigned";
            var oldStatus = complaint.Status;

            complaint.AssignedStaffId = model.SelectedStaffId;
            complaint.Priority = model.Priority;
            complaint.CategoryId = model.CategoryId;
            complaint.UpdatedAt = DateTime.UtcNow;

            if (complaint.Status == ComplaintStatus.Submitted)
            {
                complaint.Status = ComplaintStatus.Assigned;
            }

            complaint.History.Add(new ComplaintHistory
            {
                ComplaintId = complaint.Id,
                ChangedByUserId = manager.Id,
                Action = $"Assigned to {assignedStaff.FullName}",
                OldStatus = oldStatus,
                NewStatus = complaint.Status,
                Timestamp = DateTime.UtcNow,
                Notes = model.Note ?? $"Reassigned from {oldStaff} to {assignedStaff.FullName}"
            });

            await _complaintRepository.UpdateAsync(complaint);

            // Notify Staff and Student
            await _notificationService.SendInAppNotificationAsync(
                assignedStaff.Id,
                "New Complaint Assigned",
                $"You have been assigned complaint {complaint.ComplaintNumber}: {complaint.Title}.",
                complaint.Id
            );

            await _notificationService.SendNotificationAsync(new NotificationPayload
            {
                EventType = "ComplaintAssigned",
                ComplaintId = complaint.Id,
                ComplaintNumber = complaint.ComplaintNumber,
                Title = complaint.Title,
                Status = complaint.Status.ToString(),
                Priority = complaint.Priority.ToString(),
                Department = complaint.Department?.Name ?? "General",
                StudentEmail = complaint.Student?.Email ?? string.Empty,
                StaffEmail = assignedStaff.Email,
                ManagerEmail = manager.Email,
                Timestamp = DateTime.UtcNow
            });

            TempData["SuccessMessage"] = $"Complaint {complaint.ComplaintNumber} assigned to {assignedStaff.FullName}.";
            return RedirectToAction(nameof(Index));
        }
    }
}
