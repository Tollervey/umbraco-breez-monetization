namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    public interface ILightningService
    {
        Task<string> GetPaymentStatusAsync();
    }
}

