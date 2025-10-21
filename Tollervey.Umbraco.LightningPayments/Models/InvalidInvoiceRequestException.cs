namespace Tollervey.Umbraco.LightningPayments.UI.Models
{
    public class InvalidInvoiceRequestException : Exception
    {
        public InvalidInvoiceRequestException()
        {
        }

        public InvalidInvoiceRequestException(string message) : base(message)
        {
        }

        public InvalidInvoiceRequestException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}
