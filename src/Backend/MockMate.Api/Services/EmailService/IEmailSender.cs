namespace MockMate.Api.Services.EmailService;

public interface IEmailSender
{
    Task SendEmailAsync(string toEmail, string subject, string message);
}
