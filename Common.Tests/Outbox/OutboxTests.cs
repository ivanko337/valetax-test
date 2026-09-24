using System.Diagnostics;
using Common.Kafka;
using Common.Outbox;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Common.Tests.Outbox;

public sealed class OutboxTests
{
    [Fact]
    public async Task Add_CapturesCurrentW3CTraceContext()
    {
        var options = CreateInMemoryOptions();
        await using var dbContext = new TestDbContext(options);
        using var activity = new Activity("request")
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();
        activity.TraceStateString = "vendor=value";

        new EfOutboxWriter<TestDbContext>(dbContext).Add(
            "business.created",
            new TestMessage(Guid.NewGuid(), "created"));
        await dbContext.SaveChangesAsync();

        var message = Assert.Single(await dbContext.OutboxMessages.ToListAsync());
        Assert.Equal(activity.Id, message.TraceParent);
        Assert.Equal(activity.TraceStateString, message.TraceState);
    }

    [Fact]
    public async Task Add_DoesNotSave_ButCallerSavePersistsBusinessEntityAndMessage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = CreateSqliteOptions(connection);

        await using var dbContext = new TestDbContext(options);
        await dbContext.Database.EnsureCreatedAsync();

        var businessEntity = new TestBusinessEntity { Id = Guid.NewGuid() };
        dbContext.BusinessEntities.Add(businessEntity);

        var writer = new EfOutboxWriter<TestDbContext>(dbContext);
        var messageId = writer.Add(
            "business.created",
            new TestMessage(businessEntity.Id, "created"),
            businessEntity.Id.ToString());

        await using (var beforeSave = new TestDbContext(options))
        {
            Assert.Empty(await beforeSave.BusinessEntities.ToListAsync());
            Assert.Empty(await beforeSave.OutboxMessages.ToListAsync());
        }

        await dbContext.SaveChangesAsync();

        await using var afterSave = new TestDbContext(options);
        Assert.Single(await afterSave.BusinessEntities.ToListAsync());

        var message = Assert.Single(await afterSave.OutboxMessages.ToListAsync());
        Assert.Equal(messageId, message.Id);
        Assert.Equal("business.created", message.Topic);
        Assert.Equal(businessEntity.Id.ToString(), message.MessageKey);
        Assert.Equal(typeof(TestMessage).FullName, message.TypeName);
        Assert.Contains("\"State\":\"created\"", message.Payload);
    }

    [Fact]
    public async Task TransactionRollback_RemovesBusinessEntityAndOutboxMessage()
    {
        await using var connection = new SqliteConnection("Data Source=:memory:");
        await connection.OpenAsync();
        var options = CreateSqliteOptions(connection);

        await using (var dbContext = new TestDbContext(options))
        {
            await dbContext.Database.EnsureCreatedAsync();
            await using var transaction = await dbContext.Database.BeginTransactionAsync();

            var businessEntity = new TestBusinessEntity { Id = Guid.NewGuid() };
            dbContext.BusinessEntities.Add(businessEntity);
            new EfOutboxWriter<TestDbContext>(dbContext).Add(
                "business.created",
                new TestMessage(businessEntity.Id, "created"));

            await dbContext.SaveChangesAsync();
            await transaction.RollbackAsync();
        }

        await using var verificationContext = new TestDbContext(options);
        Assert.Empty(await verificationContext.BusinessEntities.ToListAsync());
        Assert.Empty(await verificationContext.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Processor_PublishesStoredValuesAndDeletesSuccessfulMessage()
    {
        var options = CreateInMemoryOptions();
        await using var dbContext = new TestDbContext(options);
        var message = CreateOutboxMessage();
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var transport = new RecordingTransport();
        var processor = new OutboxBatchProcessor<TestDbContext>(dbContext, transport);

        Assert.True(await processor.ProcessAsync(CancellationToken.None));

        var published = Assert.Single(transport.Messages);
        Assert.Equal(message.Id, published.MessageId);
        Assert.Equal(message.Topic, published.Topic);
        Assert.Equal(message.MessageKey, published.Key);
        Assert.Equal(message.TypeName, published.TypeName);
        Assert.Equal(message.Payload, published.Payload);
        Assert.Equal(message.TraceParent, published.TraceParent);
        Assert.Equal(message.TraceState, published.TraceState);
        Assert.Empty(await dbContext.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Processor_RecordsFailureAndDoesNotRetryBeforeNextAttempt()
    {
        var options = CreateInMemoryOptions();
        await using var dbContext = new TestDbContext(options);
        var message = CreateOutboxMessage();
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var transport = new RecordingTransport(
            _ => throw new InvalidOperationException("broker unavailable"));
        var processor = new OutboxBatchProcessor<TestDbContext>(dbContext, transport);

        Assert.True(await processor.ProcessAsync(CancellationToken.None));

        var failedMessage = Assert.Single(await dbContext.OutboxMessages.ToListAsync());
        Assert.Equal(1, failedMessage.Attempts);
        Assert.Equal("broker unavailable", failedMessage.LastError);
        Assert.True(failedMessage.NextAttemptAt > DateTimeOffset.UtcNow);

        Assert.False(await processor.ProcessAsync(CancellationToken.None));
        Assert.Single(transport.Messages);
    }

    [Fact]
    public async Task Processor_DoesNotSelectMessageAtMaximumAttempts()
    {
        var options = CreateInMemoryOptions();
        await using var dbContext = new TestDbContext(options);
        var message = CreateOutboxMessage();
        message.Attempts = OutboxBatchProcessor<TestDbContext>.MaxAttempts;
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        var transport = new RecordingTransport();
        var processor = new OutboxBatchProcessor<TestDbContext>(dbContext, transport);

        Assert.False(await processor.ProcessAsync(CancellationToken.None));
        Assert.Empty(transport.Messages);
        Assert.Single(await dbContext.OutboxMessages.ToListAsync());
    }

    [Fact]
    public async Task Processor_CancellationIsNotRecordedAsDeliveryFailure()
    {
        var options = CreateInMemoryOptions();
        await using var dbContext = new TestDbContext(options);
        var message = CreateOutboxMessage();
        dbContext.OutboxMessages.Add(message);
        await dbContext.SaveChangesAsync();

        using var cancellation = new CancellationTokenSource();
        var transport = new RecordingTransport(_ =>
        {
            cancellation.Cancel();
            throw new OperationCanceledException(cancellation.Token);
        });
        var processor = new OutboxBatchProcessor<TestDbContext>(dbContext, transport);

        await Assert.ThrowsAsync<OperationCanceledException>(
            () => processor.ProcessAsync(cancellation.Token));

        var retainedMessage = Assert.Single(await dbContext.OutboxMessages.ToListAsync());
        Assert.Equal(0, retainedMessage.Attempts);
        Assert.Null(retainedMessage.NextAttemptAt);
        Assert.Null(retainedMessage.LastError);
    }

    [Fact]
    public async Task KafkaTransport_PublishesKeyAndOutboxMetadataHeaders()
    {
        var publisher = new RecordingKafkaPublisher();
        var transport = new KafkaMessageTransport(publisher);
        var messageId = Guid.NewGuid();
        const string traceParent =
            "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01";
        const string traceState = "vendor=value";

        await transport.PublishAsync(
            messageId,
            "business.created",
            "business-key",
            "Contracts.BusinessCreated",
            "{\"state\":\"created\"}",
            traceParent,
            traceState,
            CancellationToken.None);

        Assert.Equal("business.created", publisher.Topic);
        Assert.Equal("business-key", publisher.Key);
        Assert.Equal("{\"state\":\"created\"}", publisher.Message);
        Assert.Equal(
            messageId.ToString(),
            publisher.Headers![KafkaMessageTransport.MessageIdHeaderName]);
        Assert.Equal(
            "Contracts.BusinessCreated",
            publisher.Headers[KafkaMessageTransport.MessageTypeHeaderName]);
        Assert.Equal(
            traceParent,
            publisher.Headers[KafkaTraceContext.TraceParentHeaderName]);
        Assert.Equal(
            traceState,
            publisher.Headers[KafkaTraceContext.TraceStateHeaderName]);
    }

    private static DbContextOptions<TestDbContext> CreateSqliteOptions(
        SqliteConnection connection)
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseSqlite(connection)
            .Options;
    }

    private static DbContextOptions<TestDbContext> CreateInMemoryOptions()
    {
        return new DbContextOptionsBuilder<TestDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
    }

    private static OutboxMessage CreateOutboxMessage()
    {
        return new OutboxMessage
        {
            Id = Guid.NewGuid(),
            Topic = "business.created",
            MessageKey = "business-key",
            TypeName = typeof(TestMessage).FullName!,
            Payload = "{\"state\":\"created\"}",
            TraceParent =
                "00-4bf92f3577b34da6a3ce929d0e0e4736-00f067aa0ba902b7-01",
            TraceState = "vendor=value",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class TestDbContext(DbContextOptions<TestDbContext> options)
        : DbContext(options)
    {
        public DbSet<TestBusinessEntity> BusinessEntities => Set<TestBusinessEntity>();

        public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<TestBusinessEntity>().HasKey(entity => entity.Id);
            modelBuilder.AddOutbox();
        }
    }

    private sealed class TestBusinessEntity
    {
        public Guid Id { get; set; }
    }

    private sealed record TestMessage(Guid Id, string State);

    private sealed record PublishedMessage(
        Guid MessageId,
        string Topic,
        string? Key,
        string TypeName,
        string Payload,
        string? TraceParent,
        string? TraceState);

    private sealed class RecordingTransport(
        Func<CancellationToken, Task>? publish = null) : IMessageTransport
    {
        public List<PublishedMessage> Messages { get; } = [];

        public async Task PublishAsync(
            Guid messageId,
            string topic,
            string? key,
            string typeName,
            string payload,
            string? traceParent,
            string? traceState,
            CancellationToken cancellationToken)
        {
            Messages.Add(new PublishedMessage(
                messageId,
                topic,
                key,
                typeName,
                payload,
                traceParent,
                traceState));

            if (publish is not null)
            {
                await publish(cancellationToken);
            }
        }
    }

    private sealed class RecordingKafkaPublisher : IKafkaPublisher
    {
        public string? Topic { get; private set; }

        public string? Message { get; private set; }

        public string? Key { get; private set; }

        public IReadOnlyDictionary<string, string>? Headers { get; private set; }

        public Task PublishAsync(
            string topic,
            string message,
            CancellationToken cancellationToken = default)
        {
            return PublishAsync(topic, message, null, null, cancellationToken);
        }

        public Task PublishAsync(
            string topic,
            string message,
            string? key,
            IReadOnlyDictionary<string, string>? headers,
            CancellationToken cancellationToken = default)
        {
            Topic = topic;
            Message = message;
            Key = key;
            Headers = headers;

            return Task.CompletedTask;
        }

        public void Dispose()
        {
        }
    }
}
