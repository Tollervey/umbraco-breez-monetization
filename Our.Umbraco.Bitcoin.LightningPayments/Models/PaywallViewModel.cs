namespace Our.Umbraco.Bitcoin.LightningPayments.Models
{
    /// <summary>
    /// View model for the paywall page, providing preview content and fee information.
    /// </summary>
    public class PaywallViewModel
    {
        /// <summary>
        /// The Umbraco content identifier for the locked content.
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// HTML preview content shown when the page is locked.
        /// </summary>
        public string? PreviewContent { get; set; } = string.Empty;

        /// <summary>
        /// The fee in satoshis required to unlock the content.
        /// </summary>
        public ulong Fee { get; set; }

        /// <summary>
        /// The custom paywall message to display.
        /// </summary>
        public string? CustomMessage { get; set; } = string.Empty;
    }
}
