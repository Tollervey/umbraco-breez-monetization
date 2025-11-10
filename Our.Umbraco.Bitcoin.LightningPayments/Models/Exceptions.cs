namespace Our.Umbraco.Bitcoin.LightningPayments.Models
{
    /// <summary>
    /// Exception thrown for payment-related errors.
    /// </summary>
    public class PaymentException : Exception
    {
        public PaymentException() { }
        public PaymentException(string message) : base(message) { }
        public PaymentException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown for webhook-related errors.
    /// </summary>
    public class WebhookException : Exception
    {
        public WebhookException() { }
        public WebhookException(string message) : base(message) { }
        public WebhookException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown for invoice-related errors.
    /// </summary>
    public class InvoiceException : Exception
    {
        public InvoiceException() { }
        public InvoiceException(string message) : base(message) { }
        public InvoiceException(string message, Exception innerException) : base(message, innerException) { }
    }

    /// <summary>
    /// Exception thrown for invoice request-related errors.
    /// </summary>
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
