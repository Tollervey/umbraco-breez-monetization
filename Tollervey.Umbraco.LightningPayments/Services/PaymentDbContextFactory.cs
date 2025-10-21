using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public class PaymentDbContextFactory : IDesignTimeDbContextFactory<PaymentDbContext>
    {
        public PaymentDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<PaymentDbContext>();
            optionsBuilder.UseSqlite("Data Source=payment.db"); // Default for design time
            return new PaymentDbContext(optionsBuilder.Options);
        }
    }
}
