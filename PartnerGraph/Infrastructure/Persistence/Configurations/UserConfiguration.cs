using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using PartnerGraph.Domain.Users;

namespace PartnerGraph.Infrastructure.Persistence.Configurations;

internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.ToTable(
            "Users",
            tableBuilder => tableBuilder.HasCheckConstraint(
                "CK_Users_PartnerId_NotSelf",
                "\"PartnerId\" IS NULL OR \"PartnerId\" <> \"ExternalId\""));

        builder.HasKey(x => x.ExternalId);

        builder.Property(x => x.ExternalId)
            .HasColumnType("uuid")
            .ValueGeneratedNever();

        builder.Property(x => x.PartnerId)
            .HasColumnType("uuid");

        builder.Property(x => x.CreatedAt)
            .HasColumnType("timestamp with time zone")
            .IsRequired();

        builder.HasOne<User>()
            .WithMany()
            .HasForeignKey(x => x.PartnerId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
