namespace Tollervey.Umbraco.LightningPayments.Models
{
    public class PaywallViewModel
    {
        public int ContentId { get; set; }
        public string PreviewContent { get; set; } = string.Empty;
        public ulong Fee { get; set; }
    }
}