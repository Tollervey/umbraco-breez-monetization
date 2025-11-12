namespace Our.Umbraco.Bitcoin.LightningPayments.Models
{
    /// <summary>
    /// View model for the TipJar partial, providing configuration and content details.
    /// </summary>
    public class TipJarViewModel
    {
        /// <summary>
        /// The Umbraco content identifier for the content displaying the tip jar.
        /// </summary>
        public int ContentId { get; set; }

        /// <summary>
        /// The configuration for the tip jar.
        /// </summary>
        public TipJarConfig? Config { get; set; }
    }
}