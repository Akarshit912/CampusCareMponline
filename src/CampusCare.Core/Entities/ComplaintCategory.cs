namespace CampusCare.Core.Entities
{
    public class ComplaintCategory
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public int DefaultDepartmentId { get; set; }

        // Navigation
        public virtual Department? DefaultDepartment { get; set; }
    }
}
