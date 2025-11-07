using SendGrid;
using System.ComponentModel.DataAnnotations;

namespace TaskManager.Services
{
    public class SendGridEmailService : IEmailService
    {
        private readonly ILogger<SendGridEmailService> _logger;
        private readonly IConfiguration _configuration;

        public SendGridEmailService(ILogger<SendGridEmailService> logger, IConfiguration configuration)
        {
            _logger = logger;
            _configuration = configuration;
        }

        public Task SendEmailReminderAsync(string userEmail, string taskName, DateTime dueDate)
        {
            var apiKey = _configuration["SendGrid:ApiKey"];
            var fromEmail = _configuration["SendGrid:SenderEmail"];
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(fromEmail))
            {
                _logger.LogError("SendGrid configuration is missing.");
                throw new InvalidOperationException("SendGrid configuration is missing.");
            }
            var client = new SendGridClient(apiKey);
            var from = new SendGrid.Helpers.Mail.EmailAddress(fromEmail, "Task Manager");
            var to = new SendGrid.Helpers.Mail.EmailAddress(userEmail);
            var subject = "Task Reminder";
            var plainTextContent = $"Reminder - Your task '{taskName}' is due on {dueDate:d}.";
            var htmlContent = $"<strong>Reminder</strong> - Your task '<em>{taskName}</em>' is due on <em>{dueDate:d}</em>.";
            var msg = SendGrid.Helpers.Mail.MailHelper.CreateSingleEmail(from, to, subject, plainTextContent, htmlContent);
            return client.SendEmailAsync(msg);
        }
    }
}
