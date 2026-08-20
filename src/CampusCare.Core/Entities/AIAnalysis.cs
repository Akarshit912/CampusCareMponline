using CampusCare.Core.Enums;
using System;

namespace CampusCare.Core.Entities
{
    public class AIAnalysis
    {
        public int Id { get; set; }
        public int ComplaintId { get; set; }
        public string SuggestedCategory { get; set; } = string.Empty;
        public PriorityLevel SuggestedPriority { get; set; }
        public string SuggestedDepartment { get; set; } = string.Empty;
        public string GeneratedSummary { get; set; } = string.Empty;
        public DateTime AnalyzedAt { get; set; } = DateTime.UtcNow;
        public string ModelUsed { get; set; } = string.Empty;
        public double ConfidenceScore { get; set; } = 0.85;

        // Navigation
        public virtual Complaint? Complaint { get; set; }
    }
}
