using CampusCare.Core.DTOs;
using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using CampusCare.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CampusCare.Web.Controllers
{
    [Authorize(Roles = "Student")]
    public class StudentController : Controller
    {
        private readonly IComplaintRepository _complaintRepository;
        private readonly IAIService _aiService;
        private readonly INotificationService _notificationService;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public StudentController(
            IComplaintRepository complaintRepository,
            IAIService aiService,
            INotificationService notificationService,
            UserManager<ApplicationUser> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment webHostEnvironment)
        {
            _complaintRepository = complaintRepository;
            _aiService = aiService;
            _notificationService = notificationService;
            _userManager = userManager;
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var complaints = await _complaintRepository.GetByStudentIdAsync(user.Id);
            var complaintList = complaints.ToList();

            var viewModel = new StudentDashboardViewModel
            {
                TotalComplaints = complaintList.Count,
                PendingComplaints = complaintList.Count(c => c.Status == ComplaintStatus.Submitted || c.Status == ComplaintStatus.Assigned),
                InProgressComplaints = complaintList.Count(c => c.Status == ComplaintStatus.InProgress || c.Status == ComplaintStatus.Escalated),
                ResolvedComplaints = complaintList.Count(c => c.Status == ComplaintStatus.Resolved || c.Status == ComplaintStatus.Closed),
                Complaints = complaintList
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Create()
        {
            await PopulateCategoriesViewBagAsync();
            return View(new CreateComplaintViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CreateComplaintViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (!ModelState.IsValid)
            {
                await PopulateCategoriesViewBagAsync();
                return View(model);
            }

            // 1. Trigger AI Analysis Engine
            var aiResult = await _aiService.AnalyzeComplaintAsync(model.Title, model.Description, model.Location);

            // 2. Resolve Category & Department
            int categoryId;
            int departmentId;

            var adminDept = await _context.Departments.FirstOrDefaultAsync(d => d.Code == "ADMIN")
                            ?? await _context.Departments.FirstOrDefaultAsync();
            int fallbackDeptId = adminDept?.Id ?? 1;

            if (model.CategoryId.HasValue && model.CategoryId.Value > 0)
            {
                categoryId = model.CategoryId.Value;
                var cat = await _context.ComplaintCategories.FindAsync(categoryId);
                departmentId = cat?.DefaultDepartmentId ?? fallbackDeptId;
            }
            else
            {
                // A. Try to match category by AI suggested category name
                var matchedCategory = await _context.ComplaintCategories
                    .FirstOrDefaultAsync(c => c.Name.ToLower() == aiResult.Category.ToLower() || c.Name.ToLower().Contains(aiResult.Category.ToLower()) || aiResult.Category.ToLower().Contains(c.Name.ToLower()));

                // B. Try to match department by AI suggested department name
                var matchedDepartment = await _context.Departments
                    .FirstOrDefaultAsync(d => d.Name.ToLower() == aiResult.Department.ToLower()
                                            || d.Name.ToLower().Contains(aiResult.Department.ToLower())
                                            || aiResult.Department.ToLower().Contains(d.Name.ToLower())
                                            || d.Code.ToLower() == aiResult.Department.ToLower());

                if (matchedCategory != null)
                {
                    categoryId = matchedCategory.Id;
                    departmentId = matchedDepartment?.Id ?? matchedCategory.DefaultDepartmentId;
                }
                else if (matchedDepartment != null)
                {
                    departmentId = matchedDepartment.Id;
                    var catInDept = await _context.ComplaintCategories.FirstOrDefaultAsync(c => c.DefaultDepartmentId == matchedDepartment.Id)
                                    ?? await _context.ComplaintCategories.FirstOrDefaultAsync(c => c.Name == "Other")
                                    ?? await _context.ComplaintCategories.FirstOrDefaultAsync();
                    categoryId = catInDept?.Id ?? 1;
                }
                else
                {
                    var otherCat = await _context.ComplaintCategories.FirstOrDefaultAsync(c => c.Name == "Other")
                                   ?? await _context.ComplaintCategories.FirstOrDefaultAsync();
                    categoryId = otherCat?.Id ?? 1;
                    departmentId = otherCat?.DefaultDepartmentId ?? fallbackDeptId;
                }
            }

            // 3. Generate Unique Complaint ID (CMP-YYYY-00001)
            string complaintNumber = await _complaintRepository.GenerateUniqueComplaintNumberAsync();

            var complaint = new Complaint
            {
                ComplaintNumber = complaintNumber,
                Title = model.Title,
                Description = model.Description,
                Location = model.Location,
                Status = ComplaintStatus.Submitted,
                Priority = model.Priority != PriorityLevel.Medium ? model.Priority : aiResult.Priority,
                CategoryId = categoryId,
                DepartmentId = departmentId,
                StudentId = user.Id,
                CreatedAt = DateTime.UtcNow
            };

            // 4. Handle Optional File Attachment
            if (model.Attachment != null && model.Attachment.Length > 0)
            {
                string uploadsFolder = Path.Combine(_webHostEnvironment.WebRootPath, "uploads");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(model.Attachment.FileName)}";
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.Attachment.CopyToAsync(fileStream);
                }

                complaint.Attachments.Add(new ComplaintAttachment
                {
                    FileName = model.Attachment.FileName,
                    FilePath = $"/uploads/{uniqueFileName}",
                    ContentType = model.Attachment.ContentType,
                    FileSize = model.Attachment.Length,
                    UploadedAt = DateTime.UtcNow
                });
            }

            // 5. Store AI Analysis
            complaint.AIAnalysis = new AIAnalysis
            {
                SuggestedCategory = aiResult.Category,
                SuggestedPriority = aiResult.Priority,
                SuggestedDepartment = aiResult.Department,
                GeneratedSummary = aiResult.Summary,
                ModelUsed = aiResult.ModelUsed,
                ConfidenceScore = aiResult.IsSuccess ? 0.88 : 0.50,
                AnalyzedAt = DateTime.UtcNow
            };

            // 6. Record Initial History Entry
            complaint.History.Add(new ComplaintHistory
            {
                ChangedByUserId = user.Id,
                Action = "Submitted",
                OldStatus = null,
                NewStatus = ComplaintStatus.Submitted,
                Timestamp = DateTime.UtcNow,
                Notes = "Complaint submitted by student."
            });

            await _complaintRepository.AddAsync(complaint);

            // 7. Trigger Notifications & n8n Automation
            var categoryObj = await _context.ComplaintCategories.FindAsync(categoryId);
            var deptObj = await _context.Departments.FindAsync(departmentId);

            await _notificationService.SendInAppNotificationAsync(
                user.Id,
                "Complaint Created",
                $"Your complaint {complaintNumber} has been submitted successfully.",
                complaint.Id
            );

            await _notificationService.SendNotificationAsync(new NotificationPayload
            {
                EventType = "NewComplaint",
                ComplaintId = complaint.Id,
                ComplaintNumber = complaint.ComplaintNumber,
                Title = complaint.Title,
                Status = complaint.Status.ToString(),
                Priority = complaint.Priority.ToString(),
                Department = deptObj?.Name ?? "General",
                StudentEmail = user.Email ?? string.Empty,
                Timestamp = DateTime.UtcNow
            });

            TempData["SuccessMessage"] = $"Complaint {complaintNumber} has been successfully submitted!";
            return RedirectToAction(nameof(Details), new { id = complaint.Id });
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var complaint = await _complaintRepository.GetByIdAsync(id);
            if (complaint == null) return NotFound();

            // Authorization Check: Student can only view their own complaint
            if (complaint.StudentId != user.Id && !User.IsInRole("Admin"))
            {
                return Forbid();
            }

            var viewModel = new ComplaintDetailsViewModel
            {
                Complaint = complaint,
                FeedbackInput = new SubmitFeedbackViewModel { ComplaintId = complaint.Id }
            };

            return View(viewModel);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddComment(int id, string commentText)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            if (string.IsNullOrWhiteSpace(commentText))
            {
                TempData["ErrorMessage"] = "Comment text cannot be empty.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var complaint = await _complaintRepository.GetByIdAsync(id);
            if (complaint == null) return NotFound();
            if (complaint.StudentId != user.Id) return Forbid();

            var comment = new ComplaintComment
            {
                ComplaintId = id,
                UserId = user.Id,
                CommentText = commentText.Trim(),
                CreatedAt = DateTime.UtcNow,
                IsInternalOnly = false
            };

            _context.ComplaintComments.Add(comment);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Comment added successfully.";
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitFeedback(SubmitFeedbackViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var complaint = await _complaintRepository.GetByIdAsync(model.ComplaintId);
            if (complaint == null) return NotFound();
            if (complaint.StudentId != user.Id) return Forbid();

            if (complaint.Status != ComplaintStatus.Resolved && complaint.Status != ComplaintStatus.Closed)
            {
                TempData["ErrorMessage"] = "Feedback can only be submitted after complaint resolution.";
                return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
            }

            if (complaint.Feedback != null)
            {
                TempData["ErrorMessage"] = "You have already submitted feedback for this complaint.";
                return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
            }

            var feedback = new Feedback
            {
                ComplaintId = model.ComplaintId,
                StudentId = user.Id,
                Rating = model.Rating,
                Comment = model.Comment,
                SubmittedAt = DateTime.UtcNow
            };

            _context.Feedbacks.Add(feedback);
            await _context.SaveChangesAsync();

            TempData["SuccessMessage"] = "Thank you for your feedback!";
            return RedirectToAction(nameof(Details), new { id = model.ComplaintId });
        }

        private async Task PopulateCategoriesViewBagAsync()
        {
            var categories = await _context.ComplaintCategories
                .Select(c => new { c.Id, c.Name })
                .ToListAsync();
            ViewBag.Categories = new SelectList(categories, "Id", "Name");
        }
    }
}
