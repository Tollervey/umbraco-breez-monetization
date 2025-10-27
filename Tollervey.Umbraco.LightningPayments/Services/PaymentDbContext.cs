using Microsoft.EntityFrameworkCore;
using Tollervey.Umbraco.LightningPayments.UI.Models;

namespace Tollervey.Umbraco.LightningPayments.UI.Services
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<PaymentState> PaymentStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentState>()
                .HasKey(p => p.PaymentHash);
            modelBuilder.Entity<PaymentState>()
                .Property(p => p.AmountSat)
                .HasDefaultValue(0UL);
            modelBuilder.Entity<PaymentState>()
                .Property(p => p.Kind)
                .HasDefaultValue(PaymentKind.Paywall);
        }
    }
}
