using Commissions.Domain;
using Commissions.Domain.CommissionSchemes;
using Common.Enums;
using Common.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Commissions.Infrastructure.Persistence.Configurations;

internal sealed class CommissionSchemeConfiguration : IEntityTypeConfiguration<CommissionScheme>
{
    public void Configure(EntityTypeBuilder<CommissionScheme> builder)
    {
        builder.ToTable("CommissionsSchemes");

        builder.HasKey(x => x.Version);

        builder.Property(x => x.Version)
            .HasColumnType("integer")
            .ValueGeneratedOnAdd();

        builder.Property(x => x.SchemaType)
            .HasConversion<byte>()
            .HasColumnType("smallint")
            .HasComment(EnumExtensions.BuildDescription<CommissionSchemaType>())
            .IsRequired();

        builder.Property(x => x.ChangedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();
    }
}
