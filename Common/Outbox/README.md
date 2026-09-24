# Common Outbox

`Common.Outbox` stores outgoing messages in the service database together with
business changes. A background worker publishes them to Kafka after the database
transaction commits.

## Setup

Add the outbox model to the service `DbContext`:

```csharp
public DbSet<OutboxMessage> OutboxMessages => Set<OutboxMessage>();

protected override void OnModelCreating(ModelBuilder modelBuilder)
{
    modelBuilder.ApplyConfigurationsFromAssembly(GetType().Assembly);
    modelBuilder.AddOutbox();
}
```

Register the Kafka transport in `Program.cs`:

```csharp
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddOutbox<AppDbContext, KafkaMessageTransport>();
```

Generate an EF Core migration after adding the model. Each service owns its own
`outbox_messages` table.

## Write a message

Add the business change and outbox message through the same scoped `DbContext`,
then save once:

```csharp
dbContext.Users.Add(user);

outbox.Add(
    topic: "users.registered.v1",
    message: new UserRegisteredMessage(user.ExternalId, user.CreatedAt),
    key: user.ExternalId.ToString());

await dbContext.SaveChangesAsync(cancellationToken);
```

Delivery is **at least once**. Successful rows are deleted; messages are attempted
up to five times. Consumers should deduplicate using the
`outbox-message-id` Kafka header.

The current worker does not coordinate row claims between replicas, so only one
instance of each service should process a given outbox table.
