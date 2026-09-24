using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Payouts;

namespace Wallet.Infrastructure.Persistence.Configurations;

internal sealed class PayoutConfiguration : IEntityTypeConfiguration<Payout>
{
    public void Configure(EntityTypeBuilder<Payout> builder)
    {
        builder.ToTable("Payouts");

        builder.HasKey(x => x.CommissionId);

        builder.Property(x => x.CommissionId)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.UserExternalId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.AmountCents)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<Domain.Wallets.Wallet>()
            .WithMany()
            .HasForeignKey(x => x.UserExternalId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
