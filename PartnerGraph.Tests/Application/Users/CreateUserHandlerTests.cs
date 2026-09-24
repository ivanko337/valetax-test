using Common.Constants;
using Common.Messages;
using Common.Outbox;
using PartnerGraph.Application.Users;
using PartnerGraph.Domain.Users;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace PartnerGraph.Tests.Application.Users;

public sealed class CreateUserHandlerTests
{
    private static readonly DateTimeOffset Now = new(2026, 9, 23, 12, 0, 0, TimeSpan.Zero);

    [Fact]
    public async Task HandleAsync_CreatesUserWithExistingPartner()
    {
        var repository = new FakeUserRepository();
        var outbox = new FakeOutboxWriter();
        var partner = CreateUser();
        repository.Users[partner.ExternalId] = partner;
        var handler = CreateHandler(repository, outbox);
        var externalId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new CreateUserCommand(externalId, partner.ExternalId),
            CancellationToken.None);

        Assert.Equal(CreateUserStatus.Created, result.Status);
        Assert.NotNull(result.User);
        Assert.Equal(externalId, result.User.ExternalId);
        Assert.Equal(partner.ExternalId, result.User.PartnerId);
        Assert.Equal(Now, result.User.CreatedAt);
        Assert.Same(result.User, repository.Users[externalId]);

        var message = Assert.IsType<UserRegisteredMessage>(outbox.Message);
        Assert.Equal(KafkaConstants.UserRegisteredTopic, outbox.Topic);
        Assert.Equal(externalId.ToString(), outbox.Key);
        Assert.Equal(externalId, message.ExternalId);
        Assert.Equal(Now, message.CreatedAt);
    }

    [Fact]
    public async Task HandleAsync_ReturnsAlreadyExistsForIdenticalRequest()
    {
        var repository = new FakeUserRepository();
        var outbox = new FakeOutboxWriter();
        var user = CreateUser(partnerId: Guid.NewGuid());
        repository.Users[user.ExternalId] = user;
        var handler = CreateHandler(repository, outbox);

        var result = await handler.HandleAsync(
            new CreateUserCommand(user.ExternalId, user.PartnerId),
            CancellationToken.None);

        Assert.Equal(CreateUserStatus.AlreadyExists, result.Status);
        Assert.Same(user, result.User);
        Assert.Equal(0, repository.InsertAttempts);
        Assert.Null(outbox.Message);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflictForExistingIdWithDifferentPartner()
    {
        var repository = new FakeUserRepository();
        var user = CreateUser(partnerId: Guid.NewGuid());
        repository.Users[user.ExternalId] = user;
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(
            new CreateUserCommand(user.ExternalId, Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(CreateUserStatus.ExternalIdConflict, result.Status);
        Assert.Same(user, result.User);
        Assert.Equal(0, repository.InsertAttempts);
    }

    [Fact]
    public async Task HandleAsync_ReturnsPartnerDoesNotExist()
    {
        var repository = new FakeUserRepository();
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(
            new CreateUserCommand(Guid.NewGuid(), Guid.NewGuid()),
            CancellationToken.None);

        Assert.Equal(CreateUserStatus.PartnerDoesNotExist, result.Status);
        Assert.Equal(0, repository.InsertAttempts);
    }

    [Fact]
    public async Task HandleAsync_RejectsSelfReference()
    {
        var repository = new FakeUserRepository();
        var handler = CreateHandler(repository);
        var externalId = Guid.NewGuid();

        var result = await handler.HandleAsync(
            new CreateUserCommand(externalId, externalId),
            CancellationToken.None);

        Assert.Equal(CreateUserStatus.CycleDetected, result.Status);
        Assert.Equal(0, repository.InsertAttempts);
    }

    [Fact]
    public async Task HandleAsync_TreatsConcurrentIdenticalInsertAsIdempotentSuccess()
    {
        var externalId = Guid.NewGuid();
        var partner = CreateUser();
        var winner = new User(externalId, partner.ExternalId, Now.AddSeconds(-1));
        var repository = new FakeUserRepository
        {
            Result = AddUserResult.ExternalIdAlreadyExists,
            ConcurrentWinner = winner
        };
        repository.Users[partner.ExternalId] = partner;
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(
            new CreateUserCommand(externalId, partner.ExternalId),
            CancellationToken.None);

        Assert.Equal(CreateUserStatus.AlreadyExists, result.Status);
        Assert.Same(winner, result.User);
        Assert.Equal(1, repository.InsertAttempts);
    }

    [Fact]
    public async Task HandleAsync_ReturnsConflictWhenConcurrentInsertUsedDifferentPartner()
    {
        var externalId = Guid.NewGuid();
        var requestedPartner = CreateUser();
        var winner = new User(externalId, null, Now.AddSeconds(-1));
        var repository = new FakeUserRepository
        {
            Result = AddUserResult.ExternalIdAlreadyExists,
            ConcurrentWinner = winner
        };
        repository.Users[requestedPartner.ExternalId] = requestedPartner;
        var handler = CreateHandler(repository);

        var result = await handler.HandleAsync(
            new CreateUserCommand(externalId, requestedPartner.ExternalId),
            CancellationToken.None);

        Assert.Equal(CreateUserStatus.ExternalIdConflict, result.Status);
        Assert.Same(winner, result.User);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task HandleAsync_RejectsEmptyIds(bool externalIdIsEmpty)
    {
        var repository = new FakeUserRepository();
        var handler = CreateHandler(repository);
        var command = externalIdIsEmpty
            ? new CreateUserCommand(Guid.Empty, null)
            : new CreateUserCommand(Guid.NewGuid(), Guid.Empty);

        var result = await handler.HandleAsync(command, CancellationToken.None);

        Assert.Equal(
            externalIdIsEmpty
                ? CreateUserStatus.InvalidExternalId
                : CreateUserStatus.InvalidPartnerId,
            result.Status);
        Assert.Equal(0, repository.InsertAttempts);
    }

    private static CreateUserHandler CreateHandler(
        IUserRepository repository,
        IOutboxWriter? outbox = null)
    {
        return new CreateUserHandler(
            repository,
            outbox ?? new FakeOutboxWriter(),
            new FixedTimeProvider(Now),
            NullLogger<CreateUserHandler>.Instance);
    }

    private static User CreateUser(Guid? partnerId = null)
    {
        return new User(Guid.NewGuid(), partnerId, Now.AddDays(-1));
    }

    private sealed class FixedTimeProvider(DateTimeOffset utcNow) : TimeProvider
    {
        public override DateTimeOffset GetUtcNow() => utcNow;
    }

    private sealed class FakeOutboxWriter : IOutboxWriter
    {
        public string? Topic { get; private set; }

        public object? Message { get; private set; }

        public string? Key { get; private set; }

        public Guid Add<TMessage>(string topic, TMessage message, string? key = null)
        {
            Topic = topic;
            Message = message;
            Key = key;

            return Guid.NewGuid();
        }
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        public Dictionary<Guid, User> Users { get; } = [];

        public AddUserResult Result { get; init; } = AddUserResult.Added;

        public User? ConcurrentWinner { get; init; }

        public int InsertAttempts { get; private set; }

        public Task<User?> FindByExternalIdAsync(
            Guid externalId,
            CancellationToken cancellationToken)
        {
            Users.TryGetValue(externalId, out var user);
            return Task.FromResult(user);
        }

        public Task<bool> ExistsAsync(Guid externalId, CancellationToken cancellationToken)
        {
            return Task.FromResult(Users.ContainsKey(externalId));
        }

        public Task<AddUserResult> TryAddAsync(
            User user,
            CancellationToken cancellationToken)
        {
            InsertAttempts++;

            if (Result == AddUserResult.Added)
            {
                Users[user.ExternalId] = user;
            }
            else if (ConcurrentWinner is not null)
            {
                Users[ConcurrentWinner.ExternalId] = ConcurrentWinner;
            }

            return Task.FromResult(Result);
        }
    }
}
