using CampusCare.Core.DTOs;
using CampusCare.Core.Entities;
using CampusCare.Core.Interfaces;
using CampusCare.Infrastructure.Data;
using Microsoft.Extensions.Configuration;
using System;
using System.Net.Http;
using System.Text.Json;
using System.Threading.Tasks;

namespace CampusCare.Infrastructure.Services
{
    public class NotificationService : INotificationService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _configuration;
        private readonly HttpClient _httpClient;

        public NotificationService(ApplicationDbContext context, IConfiguration configuration, HttpClient httpClient)
        {
            _context = context;
            _configuration = configuration;
            _httpClient = httpClient;
        }

        public async Task SendInAppNotificationAsync(string userId, string title, string message, int? complaintId = null)
        {
            var notification = new Notification
            {
                UserId = userId,
                Title = title,
                Message = message,
                RelatedComplaintId = complaintId,
                CreatedAt = DateTime.UtcNow,
                IsRead = false
            };

            await _context.Notifications.AddAsync(notification);
            await _context.SaveChangesAsync();
        }

        public async Task SendNotificationAsync(NotificationPayload payload)
        {
            // Determine n8n webhook URL based on event type
            string? webhookUrl = payload.EventType switch
            {
                "NewComplaint" => _configuration["n8nSettings:NewComplaintWebhookUrl"],
                "ComplaintResolved" => _configuration["n8nSettings:ResolvedWebhookUrl"],
                "ComplaintEscalated" => _configuration["n8nSettings:EscalatedWebhookUrl"],
                _ => _configuration["n8nSettings:GeneralWebhookUrl"]
            };

            if (!string.IsNullOrWhiteSpace(webhookUrl))
            {
                try
                {
                    using var cts = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(5));
                    var content = new StringContent(JsonSerializer.Serialize(payload), System.Text.Encoding.UTF8, "application/json");
                    await _httpClient.PostAsync(webhookUrl, content, cts.Token);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[n8n Webhook Warning] Failed to reach n8n webhook ({webhookUrl}): {ex.Message}");
                }
            }
        }
    }
}
