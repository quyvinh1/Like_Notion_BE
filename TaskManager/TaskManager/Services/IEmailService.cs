namespace TaskManager.Services
{
    public interface IEmailService
    {
        Task SendEmailReminderAsync(string userEmail, string taskName, DateTime dueDate);
    }
}
