using System.Diagnostics;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace Common.Outbox;

public sealed class EfOutboxWriter<TDbContext>(TDbContext dbContext) : IOutboxWriter
    where TDbContext : DbContext
{
    public Guid Add<TMessage>(
        string topic,
        TMessage message,
        string? key = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(topic);
        ArgumentNullException.ThrowIfNull(message);

        var activity = Activity.Current;
        var row = new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = topic,
            MessageKey = key,
            TypeName = typeof(TMessage).FullName ?? typeof(TMessage).Name,
            Payload = JsonSerializer.Serialize(message),
            TraceParent = activity?.Id,
            TraceState = activity?.TraceStateString,
            CreatedAt = DateTimeOffset.UtcNow
        };

        dbContext.Set<OutboxMessage>().Add(row);

        return row.Id;
    }
}
