using CampusCare.Core.Enums;

namespace CampusCare.Core.DTOs
{
    public class AIAnalysisResult
    {
        public string Category { get; set; } = string.Empty;
        public string Department { get; set; } = string.Empty;
        public PriorityLevel Priority { get; set; } = PriorityLevel.Medium;
        public string Summary { get; set; } = string.Empty;
        public bool IsSuccess { get; set; } = true;
        public string? ErrorMessage { get; set; }
        public string ModelUsed { get; set; } = "LocalFallback";
    }
}
