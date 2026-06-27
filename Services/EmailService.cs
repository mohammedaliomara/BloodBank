using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;
using Microsoft.Extensions.Options;

namespace BloodBank.Services
{
    public class EmailSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; } = 587;
        public string Username { get; set; } = "";
        public string Password { get; set; } = "";
        public string FromName { get; set; } = "BloodBank Egypt";
        public string FromAddress { get; set; } = "";
    }

    public interface IEmailService
    {
        Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody);
        Task<int> SendBulkEmailAsync(List<(string Email, string Name)> recipients, string subject, string htmlBody);
    }

    public class EmailService : IEmailService
    {
        private readonly EmailSettings _settings;
        private readonly ILogger<EmailService> _logger;

        public EmailService(IOptions<EmailSettings> settings, ILogger<EmailService> logger)
        {
            _settings = settings.Value;
            _logger = logger;
        }

        public async Task SendEmailAsync(string toEmail, string toName, string subject, string htmlBody)
        {
            var message = BuildMessage(toEmail, toName, subject, htmlBody);

            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);
            await smtp.SendAsync(message);
            await smtp.DisconnectAsync(true);

            _logger.LogInformation("Email sent to {Email}", toEmail);
        }

        public async Task<int> SendBulkEmailAsync(List<(string Email, string Name)> recipients, string subject, string htmlBody)
        {
            int sent = 0;
            using var smtp = new SmtpClient();
            await smtp.ConnectAsync(_settings.Host, _settings.Port, SecureSocketOptions.StartTls);
            await smtp.AuthenticateAsync(_settings.Username, _settings.Password);

            foreach (var (email, name) in recipients)
            {
                try
                {
                    var message = BuildMessage(email, name, subject, htmlBody);
                    await smtp.SendAsync(message);
                    sent++;
                    _logger.LogInformation("Bulk email sent to {Email}", email);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to send email to {Email}", email);
                }
            }

            await smtp.DisconnectAsync(true);
            return sent;
        }

        private MimeMessage BuildMessage(string toEmail, string toName, string subject, string htmlBody)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromName, _settings.FromAddress));
            message.To.Add(new MailboxAddress(toName, toEmail));
            message.Subject = subject;

            var builder = new BodyBuilder
            {
                HtmlBody = htmlBody
            };
            message.Body = builder.ToMessageBody();
            return message;
        }
    }
}
