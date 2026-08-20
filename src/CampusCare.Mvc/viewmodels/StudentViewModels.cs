using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CampusCare.Web.ViewModels
{
    public class CreateComplaintViewModel
    {
        [Required(ErrorMessage = "Title is required")]
        [StringLength(150, MinimumLength = 5, ErrorMessage = "Title must be between 5 and 150 characters")]
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required")]
        [StringLength(2000, MinimumLength = 10, ErrorMessage = "Description must be at least 10 characters long")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Location is required")]
        [StringLength(100, ErrorMessage = "Location cannot exceed 100 characters")]
        public string Location { get; set; } = string.Empty; // e.g. "Computer Lab 3", "Hostel Block B, Room 204"

        [Display(Name = "Category (Optional - AI will auto-suggest if left blank)")]
        public int? CategoryId { get; set; }

        [Display(Name = "Priority (Optional - AI will auto-suggest if default)")]
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        [Display(Name = "Upload Attachment (Photo/Document - Max 5MB)")]
        public IFormFile? Attachment { get; set; }
    }

    public class StudentDashboardViewModel
    {
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int ResolvedComplaints { get; set; }

        public IEnumerable<Complaint> Complaints { get; set; } = new List<Complaint>();
    }

    public class ComplaintDetailsViewModel
    {
        public Complaint Complaint { get; set; } = null!;
        public string NewCommentText { get; set; } = string.Empty;
        public SubmitFeedbackViewModel FeedbackInput { get; set; } = new SubmitFeedbackViewModel();
    }

    public class SubmitFeedbackViewModel
    {
        public int ComplaintId { get; set; }

        [Range(1, 5, ErrorMessage = "Rating must be between 1 and 5 stars")]
        public int Rating { get; set; } = 5;

        [StringLength(500, ErrorMessage = "Comment cannot exceed 500 characters")]
        public string? Comment { get; set; }
    }
}
