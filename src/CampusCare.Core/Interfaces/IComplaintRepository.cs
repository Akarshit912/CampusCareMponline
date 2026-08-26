using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CampusCare.Core.Interfaces
{
    public interface IComplaintRepository
    {
        Task<Complaint?> GetByIdAsync(int id);
        Task<Complaint?> GetByComplaintNumberAsync(string complaintNumber);
        Task<IEnumerable<Complaint>> GetAllAsync();
        Task<IEnumerable<Complaint>> GetByStudentIdAsync(string studentId);
        Task<IEnumerable<Complaint>> GetByDepartmentIdAsync(int departmentId);
        Task<IEnumerable<Complaint>> GetByAssignedStaffIdAsync(string staffId);
        Task<IEnumerable<Complaint>> GetOverdueComplaintsAsync(int overdueHours);
        Task AddAsync(Complaint complaint);
        Task UpdateAsync(Complaint complaint);
        Task DeleteAsync(int id);
        Task<string> GenerateUniqueComplaintNumberAsync();
    }
}
