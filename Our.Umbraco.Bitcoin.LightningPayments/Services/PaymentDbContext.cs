using Microsoft.EntityFrameworkCore;
using Our.Umbraco.Bitcoin.LightningPayments.Models;

namespace Our.Umbraco.Bitcoin.LightningPayments.Services
{
    /// <summary>
    /// EF Core database context for persisting payment state.
    /// </summary>
    public class PaymentDbContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of <see cref="PaymentDbContext"/> with the given options.
        /// </summary>
        public PaymentDbContext(DbContextOptions<PaymentDbContext> options) : base(options) { }

        /// <summary>
        /// Payment states tracked by the system, keyed by <see cref="PaymentState.PaymentHash"/>.
        /// </summary>
        public DbSet<PaymentState> PaymentStates { get; set; }

        /// <summary>
        /// Idempotency key mappings.
        /// </summary>
        public DbSet<IdempotencyMapping> IdempotencyMappings { get; set; }

        /// <inheritdoc />
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

            modelBuilder.Entity<IdempotencyMapping>()
                .HasKey(i => i.IdempotencyKey);

            modelBuilder.Entity<IdempotencyMapping>()
                .Property(i => i.CreatedAt)
                .HasDefaultValueSql("CURRENT_TIMESTAMP");
        }
    }
}

