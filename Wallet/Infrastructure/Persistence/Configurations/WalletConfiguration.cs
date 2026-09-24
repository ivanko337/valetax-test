using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Wallet.Domain.Wallets;

namespace Wallet.Infrastructure.Persistence.Configurations;

internal sealed class WalletConfiguration : IEntityTypeConfiguration<Domain.Wallets.Wallet>
{
    public void Configure(EntityTypeBuilder<Domain.Wallets.Wallet> builder)
    {
        builder.ToTable("Wallets");

        builder.HasKey(x => x.UserExternalId);

        builder.Property(x => x.UserExternalId)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.BalanceCents)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
