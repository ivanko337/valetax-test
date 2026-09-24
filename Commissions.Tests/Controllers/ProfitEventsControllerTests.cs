using Commissions.Application.ProfitEvents;
using Commissions.Controllers;
using Commissions.Controllers.Models;
using Commissions.Domain.Commissions;
using Commissions.Domain.ProfitEvents;
using Common.Application;
using Common.Enums;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Xunit;

namespace Commissions.Tests.Controllers;

public sealed class ProfitEventsControllerTests
{
    [Fact]
    public async Task GetDetails_ReturnsCompleteEventDetails()
    {
        var externalEventId = Guid.NewGuid();
        var partnerId = Guid.NewGuid();
        var details = new ProfitEventDetails(
            externalEventId,
            Guid.NewGuid(),
            50_000,
            DateTimeOffset.Parse("2026-09-24T10:00:00Z"),
            DateTimeOffset.Parse("2026-09-24T10:00:01Z"),
            ProfitEventStatus.Calculated,
            CommissionSchemaType.Linear,
            DateTimeOffset.Parse("2026-09-24T10:00:02Z"),
            [
                new ProfitEventCommissionDetails(
                    Guid.NewGuid(),
                    partnerId,
                    1,
                    5_000,
                    7,
                    CommissionSchemaType.Linear,
                    CommissionPaymentStatus.Paid,
                    DateTimeOffset.Parse("2026-09-24T10:01:00Z"))
            ]);
        var controller = CreateController(details);

        var actionResult = await controller.GetDetails(
            externalEventId,
            CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(actionResult);
        var response = Assert.IsType<ProfitEventDetailsResponse>(ok.Value);
        Assert.Equal(externalEventId, response.ExternalEventId);
        Assert.Equal("Calculated", response.Status);
        Assert.Equal("Linear", response.SchemaType);
        var commission = Assert.Single(response.Commissions);
        Assert.Equal(partnerId, commission.PartnerExternalId);
        Assert.Equal(5_000, commission.AmountCents);
        Assert.Equal(7, commission.Scheme.Version);
        Assert.Equal("Linear", commission.Scheme.Type);
        Assert.Equal("Paid", commission.PaymentStatus);
        Assert.True(commission.IsPaid);
        Assert.NotNull(commission.PaidAt);
    }

    [Fact]
    public async Task GetDetails_ReturnsNotFoundWhenEventDoesNotExist()
    {
        var controller = CreateController(details: null);

        var actionResult = await controller.GetDetails(
            Guid.NewGuid(),
            CancellationToken.None);

        var notFound = Assert.IsType<NotFoundObjectResult>(actionResult);
        var problem = Assert.IsType<ProblemDetails>(notFound.Value);
        Assert.Equal("profit_event_not_found", problem.Extensions["code"]);
    }

    [Fact]
    public async Task GetDetails_ReturnsBadRequestForEmptyEventId()
    {
        var handler = new StubDetailsHandler(null);
        var controller = CreateController(detailsHandler: handler);

        var actionResult = await controller.GetDetails(
            Guid.Empty,
            CancellationToken.None);

        Assert.IsType<BadRequestObjectResult>(actionResult);
        Assert.False(handler.WasCalled);
    }

    [Fact]
    public async Task GetUserProfitEvents_ReturnsProfitEvents()
    {
        var userExternalId = Guid.NewGuid();
        var item = new ProfitEventListItem(
            Guid.NewGuid(),
            12_500,
            Utc(10),
            Utc(11),
            ProfitEventStatus.Calculated,
            CommissionSchemaType.Fibonacci,
            Utc(12));
        var controller = CreateController(userEvents: [item]);

        var response = await controller.GetUserProfitEvents(
            userExternalId,
            CancellationToken.None);

        var result = Assert.IsType<OkObjectResult>(response);
        var events = Assert.IsType<ProfitEventResponse[]>(result.Value);
        var profitEvent = Assert.Single(events);
        Assert.Equal(item.ExternalEventId, profitEvent.ExternalEventId);
        Assert.Equal(item.ProfitCents, profitEvent.ProfitCents);
        Assert.Equal(item.OccurredAt, profitEvent.OccurredAt);
        Assert.Equal(item.ReceivedAt, profitEvent.ReceivedAt);
        Assert.Equal(item.Status, profitEvent.Status);
        Assert.Equal(item.SchemaType, profitEvent.SchemaType);
        Assert.Equal(item.CalculatedAt, profitEvent.CalculatedAt);
    }

    private static ProfitEventsController CreateController(
        ProfitEventDetails? details = null,
        StubDetailsHandler? detailsHandler = null,
        IReadOnlyList<ProfitEventListItem>? userEvents = null)
    {
        return new ProfitEventsController(
            new ReceiveHandler(),
            new UserEventsHandler(userEvents ?? []),
            detailsHandler ?? new StubDetailsHandler(details));
    }

    private static DateTimeOffset Utc(int hour)
    {
        return new DateTimeOffset(2026, 9, 24, hour, 0, 0, TimeSpan.Zero);
    }

    private sealed class ReceiveHandler
        : ICommandHandler<ReceiveProfitEventCommand, ReceiveProfitEventResult>
    {
        public Task<ReceiveProfitEventResult> HandleAsync(
            ReceiveProfitEventCommand command,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(ReceiveProfitEventResult.Accepted);
        }
    }

    private sealed class StubDetailsHandler(ProfitEventDetails? details)
        : IQueryHandler<GetProfitEventDetailsQuery, ProfitEventDetails?>
    {
        public bool WasCalled { get; private set; }

        public Task<ProfitEventDetails?> HandleAsync(
            GetProfitEventDetailsQuery query,
            CancellationToken cancellationToken)
        {
            WasCalled = true;
            return Task.FromResult(details);
        }
    }

    private sealed class UserEventsHandler(IReadOnlyList<ProfitEventListItem> result)
        : IQueryHandler<GetUserProfitEventsQuery, IReadOnlyList<ProfitEventListItem>>
    {
        public Task<IReadOnlyList<ProfitEventListItem>> HandleAsync(
            GetUserProfitEventsQuery query,
            CancellationToken cancellationToken)
        {
            return Task.FromResult(result);
        }
    }
}
