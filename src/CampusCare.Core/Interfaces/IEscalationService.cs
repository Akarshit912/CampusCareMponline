using System.Threading.Tasks;

namespace CampusCare.Core.Interfaces
{
    public interface IEscalationService
    {
        Task<int> ProcessOverdueComplaintsAsync(int overdueHoursThreshold = 48);
    }
}
