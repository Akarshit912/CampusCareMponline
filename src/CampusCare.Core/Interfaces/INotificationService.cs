using CampusCare.Core.DTOs;
using System.Threading.Tasks;

namespace CampusCare.Core.Interfaces
{
    public interface INotificationService
    {
        Task SendNotificationAsync(NotificationPayload payload);
        Task SendInAppNotificationAsync(string userId, string title, string message, int? complaintId = null);
    }
}
