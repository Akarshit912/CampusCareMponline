using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CampusCare.Web.ViewModels
{
    public class AdminDashboardViewModel
    {
        public int TotalComplaints { get; set; }
        public int PendingComplaints { get; set; }
        public int InProgressComplaints { get; set; }
        public int EscalatedComplaints { get; set; }
        public int ResolvedComplaints { get; set; }
        public double AverageResolutionHours { get; set; }
        public double AverageFeedbackRating { get; set; }

        public IEnumerable<DepartmentStatItem> DepartmentStats { get; set; } = new List<DepartmentStatItem>();
        public IEnumerable<CategoryStatItem> CategoryStats { get; set; } = new List<CategoryStatItem>();
        public IEnumerable<Complaint> RecentComplaints { get; set; } = new List<Complaint>();
    }

    public class DepartmentStatItem
    {
        public string DepartmentName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
        public int ResolvedCount { get; set; }
        public int EscalatedCount { get; set; }
    }

    public class CategoryStatItem
    {
        public string CategoryName { get; set; } = string.Empty;
        public int TotalCount { get; set; }
    }

    public class UserManagementItem
    {
        public string UserId { get; set; } = string.Empty;
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public string? DepartmentName { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreateDepartmentViewModel
    {
        [Required(ErrorMessage = "Department Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Department Code is required")]
        [StringLength(10)]
        public string Code { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }
    }

    public class CreateCategoryViewModel
    {
        [Required(ErrorMessage = "Category Name is required")]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(300)]
        public string? Description { get; set; }

        [Required(ErrorMessage = "Default Department is required")]
        public int DefaultDepartmentId { get; set; }
    }

    public class CreateStaffViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        [StringLength(100, MinimumLength = 2)]
        [Display(Name = "Full Name")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        [Display(Name = "Staff Email Address")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [StringLength(100, MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string Password { get; set; } = "Password123!";

        [Required(ErrorMessage = "Assigned Department is required")]
        [Display(Name = "Assigned Department")]
        public int DepartmentId { get; set; }

        [Display(Name = "Account Role")]
        public string Role { get; set; } = "Staff";
    }
}
