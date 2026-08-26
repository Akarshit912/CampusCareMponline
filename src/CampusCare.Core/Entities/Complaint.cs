using CampusCare.Core.Enums;
using System;
using System.Collections.Generic;

namespace CampusCare.Core.Entities
{
    public class Complaint
    {
        public int Id { get; set; }
        public string ComplaintNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Location { get; set; } = string.Empty;

        public ComplaintStatus Status { get; set; } = ComplaintStatus.Submitted;
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;

        public int CategoryId { get; set; }
        public int DepartmentId { get; set; }

        public string StudentId { get; set; } = string.Empty;
        public string? AssignedStaffId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }

        public bool IsEscalated { get; set; } = false;
        public DateTime? EscalatedAt { get; set; }
        public string? EscalationReason { get; set; }

        public string? ResolutionDetails { get; set; }

        // Navigation
        public virtual ComplaintCategory? Category { get; set; }
        public virtual Department? Department { get; set; }
        public virtual ApplicationUser? Student { get; set; }
        public virtual ApplicationUser? AssignedStaff { get; set; }

        public virtual AIAnalysis? AIAnalysis { get; set; }
        public virtual Feedback? Feedback { get; set; }

        public virtual ICollection<ComplaintComment> Comments { get; set; } = new List<ComplaintComment>();
        public virtual ICollection<ComplaintHistory> History { get; set; } = new List<ComplaintHistory>();
        public virtual ICollection<ComplaintAttachment> Attachments { get; set; } = new List<ComplaintAttachment>();
    }
}
