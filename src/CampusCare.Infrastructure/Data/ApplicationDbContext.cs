using CampusCare.Core.Entities;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace CampusCare.Infrastructure.Data
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Department> Departments => Set<Department>();
        public DbSet<ComplaintCategory> ComplaintCategories => Set<ComplaintCategory>();
        public DbSet<Complaint> Complaints => Set<Complaint>();
        public DbSet<ComplaintComment> ComplaintComments => Set<ComplaintComment>();
        public DbSet<ComplaintHistory> ComplaintHistories => Set<ComplaintHistory>();
        public DbSet<ComplaintAttachment> ComplaintAttachments => Set<ComplaintAttachment>();
        public DbSet<Notification> Notifications => Set<Notification>();
        public DbSet<Feedback> Feedbacks => Set<Feedback>();
        public DbSet<AIAnalysis> AIAnalyses => Set<AIAnalysis>();

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // ApplicationUser Department relationship
            builder.Entity<ApplicationUser>()
                .HasOne(u => u.Department)
                .WithMany(d => d.StaffMembers)
                .HasForeignKey(u => u.DepartmentId)
                .OnDelete(DeleteBehavior.SetNull);

            // Complaint Category relationship
            builder.Entity<ComplaintCategory>()
                .HasOne(c => c.DefaultDepartment)
                .WithMany(d => d.Categories)
                .HasForeignKey(c => c.DefaultDepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // Complaint relationships
            builder.Entity<Complaint>()
                .HasIndex(c => c.ComplaintNumber)
                .IsUnique();

            builder.Entity<Complaint>()
                .HasOne(c => c.Student)
                .WithMany()
                .HasForeignKey(c => c.StudentId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(c => c.AssignedStaff)
                .WithMany()
                .HasForeignKey(c => c.AssignedStaffId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(c => c.Category)
                .WithMany()
                .HasForeignKey(c => c.CategoryId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Entity<Complaint>()
                .HasOne(c => c.Department)
                .WithMany()
                .HasForeignKey(c => c.DepartmentId)
                .OnDelete(DeleteBehavior.Restrict);

            // One-to-One Complaint -> Feedback
            builder.Entity<Complaint>()
                .HasOne(c => c.Feedback)
                .WithOne(f => f.Complaint)
                .HasForeignKey<Feedback>(f => f.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            // One-to-One Complaint -> AIAnalysis
            builder.Entity<Complaint>()
                .HasOne(c => c.AIAnalysis)
                .WithOne(a => a.Complaint)
                .HasForeignKey<AIAnalysis>(a => a.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            // ComplaintComment relationships
            builder.Entity<ComplaintComment>()
                .HasOne(cc => cc.Complaint)
                .WithMany(c => c.Comments)
                .HasForeignKey(cc => cc.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ComplaintComment>()
                .HasOne(cc => cc.User)
                .WithMany()
                .HasForeignKey(cc => cc.UserId)
                .OnDelete(DeleteBehavior.Restrict);

            // ComplaintHistory relationships
            builder.Entity<ComplaintHistory>()
                .HasOne(ch => ch.Complaint)
                .WithMany(c => c.History)
                .HasForeignKey(ch => ch.ComplaintId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<ComplaintHistory>()
                .HasOne(ch => ch.ChangedByUser)
                .WithMany()
                .HasForeignKey(ch => ch.ChangedByUserId)
                .OnDelete(DeleteBehavior.Restrict);

            // Notification relationships
            builder.Entity<Notification>()
                .HasOne(n => n.User)
                .WithMany()
                .HasForeignKey(n => n.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Notification>()
                .HasOne(n => n.RelatedComplaint)
                .WithMany()
                .HasForeignKey(n => n.RelatedComplaintId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
