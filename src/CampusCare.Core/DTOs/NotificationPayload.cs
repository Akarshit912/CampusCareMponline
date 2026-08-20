using System;

namespace CampusCare.Core.DTOs
{
    public class NotificationPayload
    {
        public string EventType { get; set; } = string.Empty; // NewComplaint, ComplaintAssigned, StatusChanged, ComplaintResolved, ComplaintEscalated
        public int ComplaintId { get; set; }
        public string ComplaintNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string Priority { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public string StudentEmail { get; set; } = string.Empty;
        public string? StaffEmail { get; set; }
        public string? ManagerEmail { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }
}
