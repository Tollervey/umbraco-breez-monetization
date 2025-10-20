using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Tollervey.LightningPayments.Breez.Configuration;

namespace Tollervey.LightningPayments.Breez.Services
{
    public class SmtpEmailService : IEmailService
    {
        private readonly LightningPaymentsSettings _settings;
        private readonly Func<ISmtpClient> _clientFactory;

        public SmtpEmailService(IOptions<LightningPaymentsSettings> settings, Func<ISmtpClient> clientFactory = null)
        {
            _settings = settings.Value;
            _clientFactory = clientFactory ?? (() => new SmtpClient());
        }

        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromEmailAddress, _settings.FromEmailAddress));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = _clientFactory();
            await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
            await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
            await client.SendAsync(message);
            await client.DisconnectAsync(true);
        }
    }
}
