using CampusCare.Core.Entities;
using CampusCare.Core.Enums;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CampusCare.Web.ViewModels
{
    public class StaffDashboardViewModel
    {
        public int TotalAssigned { get; set; }
        public int PendingAction { get; set; }
        public int InProgress { get; set; }
        public int Escalated { get; set; }
        public int Resolved { get; set; }

        public IEnumerable<Complaint> Complaints { get; set; } = new List<Complaint>();
    }

    public class UpdateStatusViewModel
    {
        public int ComplaintId { get; set; }
        public ComplaintStatus NewStatus { get; set; }

        [Display(Name = "Resolution Details (Required when resolving)")]
        public string? ResolutionDetails { get; set; }

        [Display(Name = "Internal Note / Comment")]
        public string? CommentText { get; set; }

        [Display(Name = "Internal Comment Only (Hidden from Student)")]
        public bool IsInternalOnly { get; set; } = false;
    }
}
