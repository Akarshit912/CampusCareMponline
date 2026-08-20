using CampusCare.Core.Enums;
using System;

namespace CampusCare.Core.Entities
{
    public class ComplaintHistory
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string ChangedByUserId { get; set; } = string.Empty;
        public string Action { get; set; } = string.Empty;
        public ComplaintStatus? OldStatus { get; set; }
        public ComplaintStatus NewStatus { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        public string? Notes { get; set; }

        // Navigation
        public virtual Complaint? Complaint { get; set; }
        public virtual ApplicationUser? ChangedByUser { get; set; }
    }
}
