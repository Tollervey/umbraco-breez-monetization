namespace Tollervey.Umbraco.LightningPayments.UI.Configuration
{
    public interface ILightningPaymentsRuntimeMode
    {
        bool IsOffline { get; }
    }

    internal sealed class LightningPaymentsRuntimeMode : ILightningPaymentsRuntimeMode
    {
        public bool IsOffline { get; }

        public LightningPaymentsRuntimeMode(bool isOffline)
        {
            IsOffline = isOffline;
        }
    }
}