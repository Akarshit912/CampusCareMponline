using System.Collections.Generic;

namespace CampusCare.Core.Entities
{
    public class Department
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string? Description { get; set; }

        // Navigation
        public virtual ICollection<ApplicationUser> StaffMembers { get; set; } = new List<ApplicationUser>();
        public virtual ICollection<ComplaintCategory> Categories { get; set; } = new List<ComplaintCategory>();
    }
}
