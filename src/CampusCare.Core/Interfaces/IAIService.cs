using CampusCare.Core.DTOs;
using System.Threading.Tasks;

namespace CampusCare.Core.Interfaces
{
    public interface IAIService
    {
        Task<AIAnalysisResult> AnalyzeComplaintAsync(string title, string description, string location);
    }
}
