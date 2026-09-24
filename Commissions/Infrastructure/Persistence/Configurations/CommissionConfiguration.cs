using Commissions.Domain.Commissions;
using Commissions.Domain.CommissionSchemes;
using Commissions.Domain.ProfitEvents;
using Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commissions.Infrastructure.Persistence.Configurations;

internal sealed class CommissionConfiguration : IEntityTypeConfiguration<Commission>
{
    public void Configure(EntityTypeBuilder<Commission> builder)
    {
        builder.ToTable("Commissions");

        builder.HasKey(x => x.Id);

        builder.Property(x => x.Id)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.ExternalEventId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.BeneficiaryId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.Level)
            .HasColumnType("integer")
            .HasDefaultValue(1)
            .IsRequired();

        builder.Property(x => x.AmountCents)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.SchemaVersion)
            .HasColumnType("integer")
            .IsRequired();

        builder.Property(x => x.PaymentStatus)
            .HasConversion<byte>()
            .HasColumnType("smallint")
            .HasComment(EnumExtensions.BuildDescription<CommissionPaymentStatus>())
            .IsRequired();

        builder.Property(x => x.PaidAt)
            .HasColumnType("timestamp with time zone");

        builder.HasOne<ProfitEvent>()
            .WithMany()
            .HasForeignKey(x => x.ExternalEventId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne<CommissionScheme>()
            .WithMany()
            .HasForeignKey(x => x.SchemaVersion)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasIndex(x => new
            {
                x.ExternalEventId,
                x.BeneficiaryId
            })
            .IsUnique();
    }
}
