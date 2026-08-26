using System;

namespace CampusCare.Core.Entities
{
    public class Feedback
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string StudentId { get; set; } = string.Empty;
        public int Rating { get; set; } // 1 to 5
        public string? Comment { get; set; }
        public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Complaint? Complaint { get; set; }
        public virtual ApplicationUser? Student { get; set; }
    }
}
