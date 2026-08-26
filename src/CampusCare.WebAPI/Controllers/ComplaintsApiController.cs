using CampusCare.Core.Entities;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace CampusCare.WebAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ComplaintsApiController : ControllerBase
    {
        private readonly IComplaintRepository _complaintRepository;
        private readonly IEscalationService _escalationService;
        private readonly ApplicationDbContext _context;

        public ComplaintsApiController(
            IComplaintRepository complaintRepository,
            IEscalationService escalationService,
            ApplicationDbContext context)
        {
            _complaintRepository = complaintRepository;
            _escalationService = escalationService;
            _context = context;
        }

       
        // Retrieves a list of all complaints with optional filtering by status and department.
       
        [HttpGet]
        public async Task<IActionResult> GetComplaints([FromQuery] string? status, [FromQuery] int? departmentId)
        {
            var complaints = await _complaintRepository.GetAllAsync();
            var list = complaints.Select(c => new
            {
                c.Id,
                c.ComplaintNumber,
                c.Title,
                c.Location,
                Status = c.Status.ToString(),
                Priority = c.Priority.ToString(),
                Category = c.Category?.Name,
                Department = c.Department?.Name,
                Student = c.Student?.FullName,
                AssignedStaff = c.AssignedStaff?.FullName,
                c.CreatedAt,
                c.IsEscalated
            });

            return Ok(list);
        }

        
        //Retrieves detailed complaint information by ID including AI triage analysis and audit history.
        
        [HttpGet("{id}")]
        public async Task<IActionResult> GetComplaintDetails(int id)
        {
            var complaint = await _complaintRepository.GetByIdAsync(id);
            if (complaint == null) return NotFound(new { Message = $"Complaint ID {id} not found." });

            return Ok(new
            {
                complaint.Id,
                complaint.ComplaintNumber,
                complaint.Title,
                complaint.Description,
                complaint.Location,
                Status = complaint.Status.ToString(),
                Priority = complaint.Priority.ToString(),
                Category = complaint.Category?.Name,
                Department = complaint.Department?.Name,
                Student = complaint.Student?.FullName,
                AssignedStaff = complaint.AssignedStaff?.FullName,
                AISummary = complaint.AIAnalysis?.GeneratedSummary,
                AISuggestedPriority = complaint.AIAnalysis?.SuggestedPriority.ToString(),
                AISuggestedCategory = complaint.AIAnalysis?.SuggestedCategory,
                complaint.ResolutionDetails,
                complaint.CreatedAt,
                complaint.ResolvedAt,
                History = complaint.History.Select(h => new
                {
                    h.Action,
                    Status = h.NewStatus.ToString(),
                    h.Timestamp,
                    User = h.ChangedByUser?.FullName ?? "System",
                    h.Notes
                })
            });
        }

        
        //Triggers SLA breach escalation check for overdue complaints (> 48h). Designed for n8n cron scheduled triggers.
       
        [HttpPost("escalate-overdue")]
        public async Task<IActionResult> TriggerEscalationCheck([FromQuery] int overdueHours = 48)
        {
            int count = await _escalationService.ProcessOverdueComplaintsAsync(overdueHours);
            return Ok(new
            {
                Success = true,
                Message = $"Escalation scan completed. {count} overdue complaints escalated.",
                EscalatedCount = count,
                Timestamp = DateTime.UtcNow
            });
        }

       
        // Endpoint receiving webhook callback notifications from n8n automation workflows.
        
        [HttpPost("n8n/webhook-callback")]
        public IActionResult ReceiveN8nWebhookCallback([FromBody] object payload)
        {
            Console.WriteLine($"[n8n Webhook Received Callback] {payload}");
            return Ok(new { Status = "Acknowledged", Timestamp = DateTime.UtcNow });
        }

        /// <summary>
        /// Deletes a complaint record and its history by ID.
        /// </summary>
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteComplaint(int id)
        {
            var complaint = await _complaintRepository.GetByIdAsync(id);
            if (complaint == null)
            {
                return NotFound(new { Success = false, Message = $"Complaint ID {id} not found." });
            }

            string number = complaint.ComplaintNumber;
            await _complaintRepository.DeleteAsync(id);
            return Ok(new
            {
                Success = true,
                Message = $"Complaint '{number}' (ID: {id}) permanently deleted.",
                Timestamp = DateTime.UtcNow
            });
        }
    }
}
