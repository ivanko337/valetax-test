using Commissions.Domain;
using Commissions.Domain.ProfitEvents;
using Common.Enums;
using Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commissions.Infrastructure.Persistence.Configurations;

internal sealed class ProfitEventConfiguration : IEntityTypeConfiguration<ProfitEvent>
{
    public void Configure(EntityTypeBuilder<ProfitEvent> builder)
    {
        builder.ToTable(
            "ProfitEvents",
            table => table.HasCheckConstraint(
                "CK_ProfitEvents_CalculationAttempts",
                "\"CalculationAttempts\" >= 0"));

        builder.HasKey(x => x.ExternalEventId);

        builder.Property(x => x.ExternalEventId)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.UserExternalId)
            .HasColumnType("uuid")
            .IsRequired();

        builder.Property(x => x.ProfitCents)
            .HasColumnType("bigint")
            .IsRequired();

        builder.Property(x => x.OcurredAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.ReceivedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.Property(x => x.Status)
            .HasConversion<byte>()
            .HasColumnType("smallint")
            .HasComment(EnumExtensions.BuildDescription<ProfitEventStatus>())
            .IsRequired();

        builder.Property(x => x.SchemaType)
            .HasConversion<byte>()
            .HasColumnType("smallint")
            .HasComment(EnumExtensions.BuildDescription<CommissionSchemaType>());

        builder.Property(x => x.CalculatedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CalculationAttempts)
            .HasColumnType("integer")
            .HasDefaultValue(0)
            .IsRequired();

        builder.Property(x => x.NextCalculationAttemptAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CalculationStartedAt)
            .HasColumnType("timestamp with time zone");

        builder.Property(x => x.CalculationToken)
            .HasColumnType("uuid");

        builder.HasIndex(x => new
            {
                x.Status,
                x.NextCalculationAttemptAt,
                x.ReceivedAt
            })
            .HasDatabaseName("IX_ProfitEvents_PendingCalculation");

        builder.HasIndex(x => new
            {
                x.Status,
                x.CalculationStartedAt
            })
            .HasDatabaseName("IX_ProfitEvents_StaleCalculation");

        builder.HasIndex(x => new
            {
                x.UserExternalId,
                x.OcurredAt
            })
            .HasDatabaseName("IX_ProfitEvents_UserExternalId_OcurredAt")
            .IsDescending(false, true);
    }
}
