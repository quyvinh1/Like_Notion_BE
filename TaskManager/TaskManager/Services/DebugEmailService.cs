
namespace TaskManager.Services
{
    public class DebugEmailService : IEmailService
    {
        private readonly ILogger<DebugEmailService> _logger;
        public DebugEmailService(ILogger<DebugEmailService> logger)
        {
            _logger = logger;
        }

        public Task SendEmailReminderAsync(string userEmail, string taskName, DateTime dueDate)
        {
            _logger.LogInformation("Debug Email Service: Sending email to {UserEmail} about task '{TaskName}' due on {DueDate}", userEmail, taskName, dueDate);
            _logger.LogInformation("Email content: Reminder - Your task '{TaskName}' is due on {DueDate}.", taskName, dueDate);
            _logger.LogInformation("Debug Email Service: Email sent successfully to {UserEmail}", userEmail);
            return Task.CompletedTask;
        }
    }
}
