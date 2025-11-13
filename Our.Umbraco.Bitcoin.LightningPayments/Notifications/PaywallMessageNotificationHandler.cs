using Microsoft.Extensions.Logging;
using Umbraco.Cms.Core.Events;
using Umbraco.Cms.Core.Notifications;

namespace Our.Umbraco.Bitcoin.LightningPayments.Notifications
{
    /// <summary>
    /// Notification handler for paywall message-related content events (POC).
    /// </summary>
    public class PaywallMessageNotificationHandler : INotificationHandler<ContentPublishedNotification>
    {
        private readonly ILogger<PaywallMessageNotificationHandler> _logger;

        public PaywallMessageNotificationHandler(ILogger<PaywallMessageNotificationHandler> logger)
        {
            _logger = logger;
        }

        public void Handle(ContentPublishedNotification notification)
        {
            // POC: Log when content with paywall message is published
            foreach (var content in notification.PublishedEntities)
            {
                var paywallMessage = content.GetValue<string>("paywallMessage");
                if (!string.IsNullOrEmpty(paywallMessage))
                {
                    _logger.LogInformation("Content '{ContentName}' (ID: {ContentId}) published with paywall message: {Message}",
                        content.Name, content.Id, paywallMessage);
                }
            }
        }
    }
}