using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Options;
using MimeKit;
using Our.Umbraco.Bitcoin.LightningPayments.Configuration;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// SMTP-based implementation of <see cref="IEmailService"/> using MailKit.
    /// </summary>
    public class SmtpEmailService : IEmailService
    {
        private readonly LightningPaymentsSettings _settings;
        private readonly Func<ISmtpClient> _clientFactory;

        /// <summary>
        /// Creates a new instance of <see cref="SmtpEmailService"/>.
        /// </summary>
        /// <param name="settings">Lightning Payments settings (SMTP config).</param>
        /// <param name="clientFactory">Optional factory for creating <see cref="ISmtpClient"/> (useful for tests).</param>
        public SmtpEmailService(IOptions<LightningPaymentsSettings> settings, Func<ISmtpClient> clientFactory = null)
        {
            _settings = settings.Value;
            _clientFactory = clientFactory ?? (() => new SmtpClient());
        }

        /// <summary>
        /// Sends a plain-text email message.
        /// </summary>
        /// <param name="to">Recipient email address.</param>
        /// <param name="subject">Email subject.</param>
        /// <param name="body">Plain-text body.</param>
        public async Task SendEmailAsync(string to, string subject, string body)
        {
            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(_settings.FromEmailAddress, _settings.FromEmailAddress));
            message.To.Add(new MailboxAddress(to, to));
            message.Subject = subject;
            message.Body = new TextPart("plain") { Text = body };

            using var client = _clientFactory();
            try
            {
                await client.ConnectAsync(_settings.SmtpHost, _settings.SmtpPort, SecureSocketOptions.StartTls);
                await client.AuthenticateAsync(_settings.SmtpUsername, _settings.SmtpPassword);
                await client.SendAsync(message);
            }
            finally
            {
                try { await client.DisconnectAsync(true); } catch { /* swallow disconnect errors */ }
                (client as IDisposable)?.Dispose();
            }
        }
    }
}

