namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public interface IEmailService
    {
        Task SendEmailAsync(string to, string subject, string body);
    }
}
