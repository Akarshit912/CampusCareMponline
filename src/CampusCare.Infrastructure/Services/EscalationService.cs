using CampusCare.Core.DTOs;
using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using CampusCare.Core.Interfaces;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CampusCare.Infrastructure.Services
{
    public class EscalationService : IEscalationService
    {
        private readonly IComplaintRepository _complaintRepository;
        private readonly INotificationService _notificationService;

        public EscalationService(IComplaintRepository complaintRepository, INotificationService notificationService)
        {
            _complaintRepository = complaintRepository;
            _notificationService = notificationService;
        }

        public async Task<int> ProcessOverdueComplaintsAsync(int overdueHoursThreshold = 48)
        {
            var overdueComplaints = await _complaintRepository.GetOverdueComplaintsAsync(overdueHoursThreshold);
            int count = 0;

            foreach (var complaint in overdueComplaints)
            {
                var oldStatus = complaint.Status;
                complaint.IsEscalated = true;
                complaint.EscalatedAt = DateTime.UtcNow;
                complaint.EscalationReason = $"Automated Escalation: Unresolved after {overdueHoursThreshold} hours.";
                complaint.Status = ComplaintStatus.Escalated;
                complaint.UpdatedAt = DateTime.UtcNow;

                complaint.History.Add(new ComplaintHistory
                {
                    ComplaintId = complaint.Id,
                    ChangedByUserId = complaint.StudentId, // System event
                    Action = "System Auto-Escalation",
                    OldStatus = oldStatus,
                    NewStatus = ComplaintStatus.Escalated,
                    Notes = $"Complaint breached resolution SLA window ({overdueHoursThreshold}h)."
                });

                await _complaintRepository.UpdateAsync(complaint);
                count++;

                // Notify department staff and student
                await _notificationService.SendNotificationAsync(new NotificationPayload
                {
                    EventType = "ComplaintEscalated",
                    ComplaintId = complaint.Id,
                    ComplaintNumber = complaint.ComplaintNumber,
                    Title = complaint.Title,
                    Status = complaint.Status.ToString(),
                    Priority = complaint.Priority.ToString(),
                    Department = complaint.Department?.Name ?? "General",
                    StudentEmail = complaint.Student?.Email ?? string.Empty,
                    StaffEmail = complaint.AssignedStaff?.Email,
                    Timestamp = DateTime.UtcNow
                });
            }

            return count;
        }
    }
}
