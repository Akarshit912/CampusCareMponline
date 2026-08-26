using System;

namespace CampusCare.Core.Entities
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
        public bool IsRead { get; set; } = false;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public int? RelatedComplaintId { get; set; }

        // Navigation
        public virtual ApplicationUser? User { get; set; }
        public virtual Complaint? RelatedComplaint { get; set; }
    }
}
