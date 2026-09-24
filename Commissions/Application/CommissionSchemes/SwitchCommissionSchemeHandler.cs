using Commissions.Domain.CommissionSchemes;
using Common.Application;

namespace Commissions.Application.CommissionSchemes;

public sealed class SwitchCommissionSchemeHandler(
    ICommissionSchemeRepository commissionSchemeRepository,
    TimeProvider timeProvider,
    ILogger<SwitchCommissionSchemeHandler> logger)
    : ICommandHandler<SwitchCommissionSchemeCommand, CommissionSchemeResult>
{
    public async Task<CommissionSchemeResult> HandleAsync(
        SwitchCommissionSchemeCommand command,
        CancellationToken cancellationToken)
    {
        if (!Enum.IsDefined(command.SchemaType))
        {
            throw new ArgumentOutOfRangeException(
                nameof(command),
                command.SchemaType,
                "Unsupported commission schema type.");
        }

        var commissionScheme = new CommissionScheme(
            command.SchemaType,
            timeProvider.GetUtcNow());

        await commissionSchemeRepository.AddAsync(
            commissionScheme,
            cancellationToken);

        logger.LogInformation(
            "Switched commission scheme to {SchemaType} version {Version}",
            commissionScheme.SchemaType,
            commissionScheme.Version);

        return new CommissionSchemeResult(
            commissionScheme.Version,
            commissionScheme.SchemaType,
            commissionScheme.ChangedAt);
    }
}
