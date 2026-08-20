using CampusCare.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace CampusCare.Infrastructure.Services
{
    public class EscalationBackgroundService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<EscalationBackgroundService> _logger;
        private readonly TimeSpan _checkInterval = TimeSpan.FromHours(1);

        public EscalationBackgroundService(IServiceProvider serviceProvider, ILogger<EscalationBackgroundService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("CampusCare SLA Escalation Worker Service Started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var escalationService = scope.ServiceProvider.GetRequiredService<IEscalationService>();
                        int count = await escalationService.ProcessOverdueComplaintsAsync(48);
                        if (count > 0)
                        {
                            _logger.LogInformation("SLA Worker escalated {Count} overdue complaints.", count);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred during background SLA escalation worker execution.");
                }

                await Task.Delay(_checkInterval, stoppingToken);
            }
        }
    }
}
