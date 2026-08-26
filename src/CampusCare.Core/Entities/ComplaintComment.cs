using System;

namespace CampusCare.Core.Entities
{
    public class ComplaintComment
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string CommentText { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsInternalOnly { get; set; } = false;

        // Navigation
        public virtual Complaint? Complaint { get; set; }
        public virtual ApplicationUser? User { get; set; }
    }
}
