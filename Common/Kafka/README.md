# Common Kafka

`Common.Kafka` is a small wrapper around `Confluent.Kafka`. It provides a shared
publisher, hosted consumers, topic initialization, and distributed trace
propagation through Kafka headers.

## Local Kafka

Start only the broker:

```bash
docker compose up -d kafka
docker compose ps kafka
```

- Applications running on the host connect to `127.0.0.1:9092`.
- Services running in Docker Compose connect to `kafka:29092`.

Broker-side automatic topic creation is disabled. Each service initializes the
topics declared in `KafkaConstants.AllTopics` during startup.

| Topic | Producer | Consumer |
| --- | --- | --- |
| `users.registered.v1` | PartnerGraph | Wallet |
| `commissions.accrued.v1` | Commissions | Wallet |
| `commissions.paid.v1` | Wallet | Commissions |

## Usage

Configure and register Kafka in `Program.cs`:

```csharp
builder.Services.AddKafka(builder.Configuration);
builder.Services.AddKafkaConsumer<MyMessageHandler>("my-topic");

var app = builder.Build();
await app.InitializeKafkaTopicsAsync(KafkaConstants.AllTopics);
```

The corresponding configuration is:

```json
{
  "Kafka": {
    "BootstrapServers": "127.0.0.1:9092",
    "GroupId": "my-service",
    "ClientId": "my-service"
  }
}
```

Implement `IKafkaMessageHandler` for consumers and inject `IKafkaPublisher` for
direct publishing. A consumer commits its offset only after the handler succeeds.
For events produced as part of a database transaction, use
[`Common.Outbox`](../Outbox/README.md) instead of publishing directly.
