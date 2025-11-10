using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    /// <summary>
    /// Design-time factory for EF Core tooling (migrations) to create <see cref="PaymentDbContext"/>.
    /// </summary>
    public class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
    {
        /// <summary>
        /// Creates a new <see cref="PaymentDbContext"/> instance for design-time operations.
        /// </summary>
        public PaymentDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
            optionsBuilder.UseSqlite("Data Source=payment.db"); // Default for design time
            return new PaymentDbContext(optionsBuilder.Options);
        }
    }
}
