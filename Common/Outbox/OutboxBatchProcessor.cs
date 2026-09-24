using Microsoft.EntityFrameworkCore;

namespace Common.Outbox;

public sealed class OutboxBatchProcessor<TDbContext>(
    TDbContext dbContext,
    IMessageTransport transport)
    where TDbContext : DbContext
{
    public const int MaxAttempts = 5;

    public const int BatchSize = 20;

    public async Task<bool> ProcessAsync(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;

        var messages = await dbContext.Set<OutboxMessage>()
            .Where(message =>
                message.Attempts < MaxAttempts &&
                (message.NextAttemptAt == null || message.NextAttemptAt <= now))
            .OrderBy(message => message.CreatedAt)
            .Take(BatchSize)
            .ToListAsync(cancellationToken)
            .ConfigureAwait(false);

        foreach (var message in messages)
        {
            try
            {
                await transport.PublishAsync(
                        message.Id,
                        message.Topic,
                        message.MessageKey,
                        message.TypeName,
                        message.Payload,
                        message.TraceParent,
                        message.TraceState,
                        cancellationToken)
                    .ConfigureAwait(false);

                dbContext.Remove(message);
            }
            catch (OperationCanceledException)
                when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception exception)
            {
                message.Attempts++;
                message.NextAttemptAt = DateTimeOffset.UtcNow.AddSeconds(
                    Math.Min(60, Math.Pow(2, message.Attempts)));
                message.LastError = exception.Message.Length <= 2000
                    ? exception.Message
                    : exception.Message[..2000];
            }

            await dbContext.SaveChangesAsync(cancellationToken).ConfigureAwait(false);
        }

        return messages.Count > 0;
    }
}
