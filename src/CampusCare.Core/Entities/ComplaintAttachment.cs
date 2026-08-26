using System;

namespace CampusCare.Core.Entities
{
    public class ComplaintAttachment
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string FileName { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ContentType { get; set; } = string.Empty;
        public long FileSize { get; set; }
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public virtual Complaint? Complaint { get; set; }
    }
}
