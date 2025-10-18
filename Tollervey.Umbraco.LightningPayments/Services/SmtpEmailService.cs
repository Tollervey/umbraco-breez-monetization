using System.Net.Mail;
using Microsoft.Extensions.Options;
using Tollervey.Umbraco.LightningPayments.Configuration;

namespace Tollervey.Umbraco.LightningPayments.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly LightningPaymentsSettings _settings;

        public SmtpEmailService(IOptions<LightningPaymentsSettings> settings)
        {
            _settings = settings.Value;
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            // Basic SMTP implementation - in production, use a service like SendGrid
            using var client = new SmtpClient(_settings.SmtpHost, _settings.SmtpPort)
            {
                Credentials = new System.Net.NetworkCredential(_settings.SmtpUsername, _settings.SmtpPassword),
                EnableSsl = true
            };

            var mailMessage = new MailMessage
            {
                From = new MailAddress(_settings.FromEmailAddress),
                Subject = subject,
                Body = body,
                IsBodyHtml = false
            };

            mailMessage.To.Add(to);

            await client.SendMailAsync(mailMessage);
        }
    }
}