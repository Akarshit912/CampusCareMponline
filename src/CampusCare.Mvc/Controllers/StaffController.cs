using CampusCare.Core.DTOs;
using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using CampusCare.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CampusCare.Web.Controllers
{
    [Authorize(Roles = "Staff,Manager,Admin")]
    public class StaffController : Controller
    {
        private readonly IComplaintRepository _complaintRepository;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;

        public StaffController(
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
        public async Task<IActionResult> Index(string? statusFilter = null)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var complaints = await _complaintRepository.GetByAssignedStaffIdAsync(user.Id);
            var complaintList = complaints.ToList();

            if (!string.IsNullOrEmpty(statusFilter) && Enum.TryParse<ComplaintStatus>(statusFilter, out var filterEnum))
            {
                complaintList = complaintList.Where(c => c.Status == filterEnum).ToList();
            }

            var viewModel = new StaffDashboardViewModel
            {
                TotalAssigned = complaints.Count(),
                PendingAction = complaints.Count(c => c.Status == ComplaintStatus.Assigned),
                InProgress = complaints.Count(c => c.Status == ComplaintStatus.InProgress),
                Escalated = complaints.Count(c => c.Status == ComplaintStatus.Escalated),
                Resolved = complaints.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed),
                Complaints = complaintList
            };

            ViewBag.StatusFilter = statusFilter;
            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var complaint = await _complaintRepository.GetByIdAsync(id);
            if (complaint == null) return NotFound();

            var viewModel = new UpdateStatusViewModel
            {
                ComplaintId = complaint.Id,
                NewStatus = complaint.Status
            };

            ViewBag.Complaint = complaint;
            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateStatus(UpdateStatusViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var complaint = await _complaintRepository.GetByIdAsync(model.ComplaintId);
            if (complaint == null) return NotFound();

            // Validate Workflow State Machine Rules
            if (!IsValidStateTransition(complaint.Status, model.NewStatus))
            {
                TempData["ErrorMessage"] = $"Invalid status transition from '{complaint.Status}' to '{model.NewStatus}'.";
                return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
            }

            if (model.NewStatus == ComplaintStatus.Resolved && string.IsNullOrWhiteSpace(model.ResolutionDetails))
            {
                TempData["ErrorMessage"] = "Resolution details are required when marking a complaint as Resolved.";
                return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
            }

            var oldStatus = complaint.Status;
            complaint.Status = model.NewStatus;
            complaint.UpdatedAt = DateTime.UtcNow;

            if (model.NewStatus == ComplaintStatus.Resolved)
            {
                complaint.ResolvedAt = DateTime.UtcNow;
                complaint.ResolutionDetails = model.ResolutionDetails;
            }
            else if (model.NewStatus == ComplaintStatus.Closed)
            {
                complaint.ClosedAt = DateTime.UtcNow;
            }

            // Record History Entry
            complaint.History.Add(new ComplaintHistory
            {
                ComplaintId = complaint.Id,
                ChangedByUserId = user.Id,
                Action = $"Status updated to {model.NewStatus}",
                OldStatus = oldStatus,
                NewStatus = model.NewStatus,
                Timestamp = DateTime.UtcNow,
                Notes = model.CommentText ?? (model.NewStatus == ComplaintStatus.Resolved ? model.ResolutionDetails : null)
            });

            // Optional Comment
            if (!string.IsNullOrWhiteSpace(model.CommentText))
            {
                complaint.Comments.Add(new ComplaintComment
                {
                    ComplaintId = complaint.Id,
                    UserId = user.Id,
                    CommentText = model.CommentText.Trim(),
                    CreatedAt = DateTime.UtcNow,
                    IsInternalOnly = model.IsInternalOnly
                });
            }

            await _complaintRepository.UpdateAsync(complaint);

            // Trigger Notifications & n8n Automation
            await _notificationService.SendInAppNotificationAsync(
                complaint.StudentId,
                $"Complaint {complaint.ComplaintNumber} Updated",
                $"Status changed to '{model.NewStatus}'.",
                complaint.Id
            );

            if (model.NewStatus == ComplaintStatus.Resolved)
            {
                await _notificationService.SendNotificationAsync(new NotificationPayload
                {
                    EventType = "ComplaintResolved",
                    ComplaintId = complaint.Id,
                    ComplaintNumber = complaint.ComplaintNumber,
                    Title = complaint.Title,
                    Status = complaint.Status.ToString(),
                    Priority = complaint.Priority.ToString(),
                    Department = complaint.Department?.Name ?? "General",
                    StudentEmail = complaint.Student?.Email ?? string.Empty,
                    StaffEmail = user.Email,
                    Timestamp = DateTime.UtcNow
                });
            }

            TempData["SuccessMessage"] = $"Complaint status updated to '{model.NewStatus}'.";
            return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int complaintId, string commentText, bool isInternalOnly)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["ErrorMessage"] = "Comment text cannot be empty.";
                return RedirectToAction(nameof(Details), new { id = complaintId });
            }

            var comment = new ComplaintComment
            {
                ComplaintId = complaintId,
                UserId = user.Id,
                CommentText = commentText.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsInternalOnly = isInternalOnly
            };

            _context.ComplaintComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added successfully.";
            return RedirectToAction(nameof(Details), new { id = complaintId });
        }

        private bool IsValidStateTransition(ComplaintStatus current, ComplaintStatus target)
        {
            if (current == target) return true;

            return current switch
            {
                ComplaintStatus.Submitted => target == ComplaintStatus.Assigned || target == ComplaintStatus.InProgress || target == ComplaintStatus.Rejected,
                ComplaintStatus.Assigned => target == ComplaintStatus.InProgress || target == ComplaintStatus.Rejected || target == ComplaintStatus.Escalated,
                ComplaintStatus.InProgress => target == ComplaintStatus.Resolved || target == ComplaintStatus.Escalated || target == ComplaintStatus.Rejected,
                ComplaintStatus.Escalated => target == ComplaintStatus.InProgress || target == ComplaintStatus.Resolved,
                ComplaintStatus.Resolved => target == ComplaintStatus.Closed || target == ComplaintStatus.InProgress,
                ComplaintStatus.Closed => false, // Cannot transition out of closed
                ComplaintStatus.Rejected => false,
                _ => false
            };
        }
    }
}
