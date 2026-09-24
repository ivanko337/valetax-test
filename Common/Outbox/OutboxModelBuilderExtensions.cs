using Microsoft.EntityFrameworkCore;

namespace Common.Outbox;

public static class OutboxModelBuilderExtensions
{
    public static void AddOutbox(this ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(modelBuilder);

        var entity = modelBuilder.Entity<OutboxMessage>();

        entity.ToTable(
            "outbox_messages",
            table => table.HasCheckConstraint(
                "ck_outbox_attempts",
                "attempts >= 0"));

        entity.HasKey(message => message.Id);

        entity.Property(message => message.Id)
            .HasColumnName("id");

        entity.Property(message => message.Topic)
            .HasColumnName("topic")
            .IsRequired();

        entity.Property(message => message.MessageKey)
            .HasColumnName("message_key");

        entity.Property(message => message.TypeName)
            .HasColumnName("type_name")
            .IsRequired();

        entity.Property(message => message.Payload)
            .HasColumnName("payload")
            .HasColumnType("jsonb")
            .IsRequired();

        entity.Property(message => message.TraceParent)
            .HasColumnName("trace_parent")
            .HasMaxLength(55);

        entity.Property(message => message.TraceState)
            .HasColumnName("trace_state")
            .HasMaxLength(512);

        entity.Property(message => message.CreatedAt)
            .HasColumnName("created_at")
            .HasColumnType("timestamp with time zone")
            .HasDefaultValueSql("now()");

        entity.Property(message => message.Attempts)
            .HasColumnName("attempts")
            .HasDefaultValue(0);

        entity.Property(message => message.NextAttemptAt)
            .HasColumnName("next_attempt_at")
            .HasColumnType("timestamp with time zone");

        entity.Property(message => message.LastError)
            .HasColumnName("last_error");

        entity.HasIndex(message => message.CreatedAt)
            .HasDatabaseName("ix_outbox_messages_created_at");
    }
}
