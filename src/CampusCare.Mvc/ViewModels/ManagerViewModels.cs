using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CampusCare.Web.ViewModels
{
    public class ManagerDashboardViewModel
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalDepartmentComplaints { get; set; }
        public int UnassignedCount { get; set; }
        public int InProgressCount { get; set; }
        public int EscalatedCount { get; set; }
        public int ResolvedCount { get; set; }

        public IEnumerable<Complaint> Complaints { get; set; } = new List<Complaint>();
        public IEnumerable<StaffWorkloadItem> StaffWorkload { get; set; } = new List<StaffWorkloadItem>();
    }

    public class StaffWorkloadItem
    {
        public string StaffId { get; set; } = string.Empty;
        public string StaffName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public int ActiveAssignedCount { get; set; }
        public int ResolvedCount { get; set; }
    }

    public class AssignStaffViewModel
    {
        public int ComplaintId { get; set; }
        public string ComplaintNumber { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;

        [Required(ErrorMessage = "Please select a staff member")]
        [Display(Name = "Assign To Staff Member")]
        public string SelectedStaffId { get; set; } = string.Empty;

        [Display(Name = "Priority Level")]
        public PriorityLevel Priority { get; set; }

        [Display(Name = "Category")]
        public int CategoryId { get; set; }

        public string? Note { get; set; }
    }
}
