using Microsoft.EntityFrameworkCore;
using Tollervey.LightningPayments.Breez.Models;

namespace Tollervey.LightningPayments.Breez.Services
{
    public class PaymentDbContext : DbContext
    {
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        public DbSet<PaymentState> PaymentStates { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<PaymentState>()
                .HasKey(p => p.PaymentHash);
        }
    }
}
