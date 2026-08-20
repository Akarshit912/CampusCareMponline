using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CampusCare.Infrastructure.Repositories
{
    public class ComplaintRepository : IComplaintRepository
    {
        private readonly ApplicationDbContext _context;

        public ComplaintRepository(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<Complaint?> GetByIdAsync(int id)
        {
            return await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff)
                .Include(c => c.Category)
                .Include(c => c.Department)
                .Include(c => c.AIAnalysis)
                .Include(c => c.Feedback)
                .Include(c => c.Attachments)
                .Include(c => c.Comments.OrderBy(cm => cm.CreatedAt))
                    .ThenInclude(cm => cm.User)
                .Include(c => c.History.OrderByDescending(h => h.Timestamp))
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(c => c.Id == id);
        }

        public async Task<Complaint?> GetByComplaintNumberAsync(string complaintNumber)
        {
            return await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff)
                .Include(c => c.Category)
                .Include(c => c.Department)
                .Include(c => c.AIAnalysis)
                .Include(c => c.Feedback)
                .Include(c => c.Attachments)
                .Include(c => c.Comments.OrderBy(cm => cm.CreatedAt))
                    .ThenInclude(cm => cm.User)
                .Include(c => c.History.OrderByDescending(h => h.Timestamp))
                    .ThenInclude(h => h.ChangedByUser)
                .FirstOrDefaultAsync(c => c.ComplaintNumber == complaintNumber);
        }

        public async Task<IEnumerable<Complaint>> GetAllAsync()
        {
            return await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.AssignedStaff)
                .Include(c => c.Category)
                .Include(c => c.Department)
                .Include(c => c.Feedback)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Complaint>> GetByStudentIdAsync(string studentId)
        {
            return await _context.Complaints
                .Include(c => c.Category)
                .Include(c => c.Department)
                .Include(c => c.AssignedStaff)
                .Include(c => c.Feedback)
                .Where(c => c.StudentId == studentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Complaint>> GetByDepartmentIdAsync(int departmentId)
        {
            return await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.Category)
                .Include(c => c.AssignedStaff)
                .Include(c => c.Department)
                .Where(c => c.DepartmentId == departmentId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Complaint>> GetByAssignedStaffIdAsync(string staffId)
        {
            return await _context.Complaints
                .Include(c => c.Student)
                .Include(c => c.Category)
                .Include(c => c.Department)
                .Where(c => c.AssignedStaffId == staffId)
                .OrderByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Complaint>> GetOverdueComplaintsAsync(int overdueHours)
        {
            var cutoffDate = DateTime.UtcNow.AddHours(-overdueHours);
            return await _context.Complaints
                .Include(c => c.Department)
                .Include(c => c.AssignedStaff)
                .Include(c => c.Student)
                .Where(c => !c.IsEscalated && 
                            c.Status != ComplaintStatus.Resolved && 
                            c.Status != ComplaintStatus.Closed && 
                            c.Status != ComplaintStatus.Rejected &&
                            c.CreatedAt <= cutoffDate)
                .ToListAsync();
        }

        public async Task AddAsync(Complaint complaint)
        {
            await _context.Complaints.AddAsync(complaint);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateAsync(Complaint complaint)
        {
            _context.Complaints.Update(complaint);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(int id)
        {
            var complaint = await _context.Complaints.FindAsync(id);
            if (complaint != null)
            {
                // 1. Clean up notifications linked to this complaint
                var notifications = await _context.Notifications
                    .Where(n => n.RelatedComplaintId == id)
                    .ToListAsync();
                if (notifications.Any())
                {
                    _context.Notifications.RemoveRange(notifications);
                }

                // 2. Clean up attachments
                var attachments = await _context.ComplaintAttachments
                    .Where(a => a.ComplaintId == id)
                    .ToListAsync();
                if (attachments.Any())
                {
                    _context.ComplaintAttachments.RemoveRange(attachments);
                }

                // 3. Clean up comments
                var comments = await _context.ComplaintComments
                    .Where(c => c.ComplaintId == id)
                    .ToListAsync();
                if (comments.Any())
                {
                    _context.ComplaintComments.RemoveRange(comments);
                }

                // 4. Clean up audit history logs
                var history = await _context.ComplaintHistories
                    .Where(h => h.ComplaintId == id)
                    .ToListAsync();
                if (history.Any())
                {
                    _context.ComplaintHistories.RemoveRange(history);
                }

                // 5. Clean up AI Analysis
                var aiAnalysis = await _context.AIAnalyses
                    .FirstOrDefaultAsync(a => a.ComplaintId == id);
                if (aiAnalysis != null)
                {
                    _context.AIAnalyses.Remove(aiAnalysis);
                }

                // 6. Clean up Feedback
                var feedback = await _context.Feedbacks
                    .FirstOrDefaultAsync(f => f.ComplaintId == id);
                if (feedback != null)
                {
                    _context.Feedbacks.Remove(feedback);
                }

                // 7. Remove parent Complaint
                _context.Complaints.Remove(complaint);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<string> GenerateUniqueComplaintNumberAsync()
        {
            int currentYear = DateTime.UtcNow.Year;
            string prefix = $"CMP-{currentYear}-";

            var lastComplaint = await _context.Complaints
                .Where(c => c.ComplaintNumber.StartsWith(prefix))
                .OrderByDescending(c => c.Id)
                .FirstOrDefaultAsync();

            int nextSequence = 1;
            if (lastComplaint != null && !string.IsNullOrEmpty(lastComplaint.ComplaintNumber))
            {
                string parts = lastComplaint.ComplaintNumber.Substring(prefix.Length);
                if (int.TryParse(parts, out int lastSeq))
                {
                    nextSequence = lastSeq + 1;
                }
            }

            return $"{prefix}{nextSequence:D5}";
        }
    }
}
