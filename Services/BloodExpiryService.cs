using BloodBank.Data;
using Microsoft.EntityFrameworkCore;

namespace BloodBank.Services
{
    public class BloodExpiryService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<BloodExpiryService> _logger;

        public BloodExpiryService(IServiceProvider serviceProvider, ILogger<BloodExpiryService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                _logger.LogInformation("BloodExpiryService running at: {time}", DateTimeOffset.Now);

                try
                {
                    using (var scope = _serviceProvider.CreateScope())
                    {
                        var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                        var today = DateTime.Today;

                        // Find units that have expired but are still marked as Available
                        var expiredUnits = await context.BloodUnits
                            .Where(b => b.Status == "Available" && b.ExpiryDate.Date < today)
                            .ToListAsync(stoppingToken);

                        if (expiredUnits.Any())
                        {
                            foreach (var unit in expiredUnits)
                            {
                                unit.Status = "Expired";
                                _logger.LogInformation($"BloodUnit {unit.BloodUnitId} (Type: {unit.BloodType}) marked as Expired.");
                            }

                            // Add an audit log entry for the system action
                            var auditLog = new BloodBank.Models.AuditLog
                            {
                                Action = "Expiry Update",
                                EntityName = "BloodUnit",
                                EntityId = 0,
                                Details = $"{expiredUnits.Count} units marked as expired.",
                                Timestamp = DateTime.Now,
                                UserId = "System"
                            };
                            context.AuditLogs.Add(auditLog);

                            await context.SaveChangesAsync(stoppingToken);
                        }
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error occurred executing BloodExpiryService.");
                }

                // Run once a day (or every 1 hour for testing, let's do every 12 hours)
                await Task.Delay(TimeSpan.FromHours(12), stoppingToken);
            }
        }
    }
}
