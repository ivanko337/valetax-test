using Common.Application;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using PartnerGraph.Application.Users;
using PartnerGraph.Controllers;
using PartnerGraph.Controllers.Models;
using PartnerGraph.Domain.Users;
using PartnerGraph.Http;
using Xunit;

namespace PartnerGraph.Tests.Controllers;

public sealed class UsersControllerTests
{
    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task PartnerTraversal_PropagatesCycleToTheExceptionPipeline(
        bool queryUpline)
    {
        var externalId = Guid.NewGuid();
        var controller = new UsersController(
            new UnusedCreateUserHandler(),
            new StubUpdateUserPartnerHandler(),
            new CycleUplineHandler(),
            new CycleDownlineHandler());

        Task<IActionResult> Act() => queryUpline
            ? controller.GetUpline(externalId, CancellationToken.None)
            : controller.GetDownline(externalId, CancellationToken.None);

        await Assert.ThrowsAsync<PartnerGraphCycleException>(Act);
    }

    [Theory]
    [InlineData(UpdateUserPartnerStatus.PartnerDoesNotExist, "partner_not_found")]
    [InlineData(UpdateUserPartnerStatus.CycleDetected, "partner_cycle")]
    public async Task UpdatePartner_ReturnsConflictForInvalidRelationship(
        UpdateUserPartnerStatus status,
        string expectedCode)
    {
        var externalId = Guid.NewGuid();
        var controller = new UsersController(
            new UnusedCreateUserHandler(),
            new StubUpdateUserPartnerHandler(new UpdateUserPartnerResult(status)),
            new CycleUplineHandler(),
            new CycleDownlineHandler());

        var exception = await Assert.ThrowsAsync<ApiProblemException>(() =>
            controller.UpdatePartner(
                externalId,
                new UpdateUserPartnerRequest(Guid.NewGuid()),
                CancellationToken.None));

        Assert.Equal(StatusCodes.Status409Conflict, exception.StatusCode);
        Assert.Equal(expectedCode, exception.Code);
    }

    [Fact]
    public async Task UpdatePartner_ReturnsInternalServerErrorForCorruptedGraph()
    {
        var externalId = Guid.NewGuid();
        var controller = new UsersController(
            new UnusedCreateUserHandler(),
            new StubUpdateUserPartnerHandler(exception: new PartnerGraphCycleException(
                externalId,
                "upline")),
            new CycleUplineHandler(),
            new CycleDownlineHandler());

        await Assert.ThrowsAsync<PartnerGraphCycleException>(() =>
            controller.UpdatePartner(
                externalId,
                new UpdateUserPartnerRequest(Guid.NewGuid()),
                CancellationToken.None));
    }

    private sealed class UnusedCreateUserHandler
        : ICommandHandler<CreateUserCommand, CreateUserResult>
    {
        public Task<CreateUserResult> HandleAsync(
            CreateUserCommand command,
            CancellationToken cancellationToken)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubUpdateUserPartnerHandler(
        UpdateUserPartnerResult? result = null,
        Exception? exception = null)
        : ICommandHandler<UpdateUserPartnerCommand, UpdateUserPartnerResult>
    {
        public Task<UpdateUserPartnerResult> HandleAsync(
            UpdateUserPartnerCommand command,
            CancellationToken cancellationToken)
        {
            if (exception is not null)
            {
                throw exception;
            }

            return Task.FromResult(
                result ?? new UpdateUserPartnerResult(UpdateUserPartnerStatus.UserDoesNotExist));
        }
    }

    private sealed class CycleUplineHandler
        : IQueryHandler<GetUplineQuery, IReadOnlyList<PartnerAtLevel>?>
    {
        public Task<IReadOnlyList<PartnerAtLevel>?> HandleAsync(
            GetUplineQuery query,
            CancellationToken cancellationToken)
        {
            throw new PartnerGraphCycleException(query.ExternalId, "upline");
        }
    }

    private sealed class CycleDownlineHandler
        : IQueryHandler<GetDownlineQuery, IReadOnlyList<User>?>
    {
        public Task<IReadOnlyList<User>?> HandleAsync(
            GetDownlineQuery query,
            CancellationToken cancellationToken)
        {
            throw new PartnerGraphCycleException(query.ExternalId, "downline");
        }
    }
}
