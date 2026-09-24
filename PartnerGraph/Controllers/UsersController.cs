using Common.Application;
using Microsoft.AspNetCore.Mvc;
using PartnerGraph.Application.Users;
using PartnerGraph.Controllers.Models;
using PartnerGraph.Http;

namespace PartnerGraph.Controllers;

[ApiController]
[Route("api/[controller]")]
public sealed class UsersController(
    ICommandHandler<CreateUserCommand, CreateUserResult> createUserHandler,
    ICommandHandler<UpdateUserPartnerCommand, UpdateUserPartnerResult> updateUserPartnerHandler,
    IQueryHandler<GetUplineQuery, IReadOnlyList<PartnerAtLevel>?> getUplineHandler,
    IQueryHandler<GetDownlineQuery, IReadOnlyList<Domain.Users.User>?> getDownlineHandler)
    : ControllerBase
{
    [HttpPost]
    [ProducesResponseType<UserResponse>(StatusCodes.Status201Created)]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    public async Task<IActionResult> Create(
        [FromBody] CreateUserRequest request,
        CancellationToken cancellationToken)
    {
        var result = await createUserHandler.HandleAsync(
            new CreateUserCommand(request.ExternalId, request.PartnerId),
            cancellationToken);

        return result.Status switch
        {
            CreateUserStatus.Created => StatusCode(
                StatusCodes.Status201Created,
                UserResponse.From(result.User!)),
            CreateUserStatus.AlreadyExists => Ok(UserResponse.From(result.User!)),
            CreateUserStatus.InvalidExternalId => throw ApiProblemException.Validation(
                "externalId",
                "ExternalId must be a non-empty UUID."),
            CreateUserStatus.InvalidPartnerId => throw ApiProblemException.Validation(
                "partnerId",
                "PartnerId must be null or a non-empty UUID."),
            CreateUserStatus.PartnerDoesNotExist => throw ApiProblemException.Conflict(
                "The user could not be created.",
                "partner_not_found",
                "The requested partner does not exist."),
            CreateUserStatus.CycleDetected => throw ApiProblemException.Conflict(
                "The user could not be created.",
                "partner_cycle",
                "The requested partner relationship would create a cycle."),
            CreateUserStatus.ExternalIdConflict => throw ApiProblemException.Conflict(
                "The user could not be created.",
                "external_id_conflict",
                "A user with this externalId already exists with a different partnerId."),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null)
        };
    }

    [HttpPut("{externalId:guid}/partner")]
    [ProducesResponseType<UserResponse>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status409Conflict)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> UpdatePartner(
        Guid externalId,
        [FromBody] UpdateUserPartnerRequest request,
        CancellationToken cancellationToken)
    {
        var result = await updateUserPartnerHandler.HandleAsync(
            new UpdateUserPartnerCommand(externalId, request.PartnerId),
            cancellationToken);

        return result.Status switch
        {
            UpdateUserPartnerStatus.Updated or UpdateUserPartnerStatus.Unchanged =>
                Ok(UserResponse.From(result.User!)),
            UpdateUserPartnerStatus.InvalidExternalId => throw ApiProblemException.Validation(
                "externalId",
                "ExternalId must be a non-empty UUID."),
            UpdateUserPartnerStatus.InvalidPartnerId => throw ApiProblemException.Validation(
                "partnerId",
                "PartnerId must be null or a non-empty UUID."),
            UpdateUserPartnerStatus.UserDoesNotExist => throw UserNotFound(externalId),
            UpdateUserPartnerStatus.PartnerDoesNotExist => throw ApiProblemException.Conflict(
                "The partner link could not be updated.",
                "partner_not_found",
                "The requested partner does not exist."),
            UpdateUserPartnerStatus.CycleDetected => throw ApiProblemException.Conflict(
                "The partner link could not be updated.",
                "partner_cycle",
                "The requested partner relationship would create a cycle."),
            _ => throw new ArgumentOutOfRangeException(nameof(result.Status), result.Status, null)
        };
    }

    [HttpGet("{externalId:guid}/upline")]
    [ProducesResponseType<IReadOnlyList<UplinePartnerResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetUpline(
        Guid externalId,
        CancellationToken cancellationToken)
    {
        var partners = await getUplineHandler.HandleAsync(
            new GetUplineQuery(externalId),
            cancellationToken);

        return partners is null
            ? throw UserNotFound(externalId)
            : Ok(partners.Select(UplinePartnerResponse.From).ToArray());
    }

    [HttpGet("{externalId:guid}/downline")]
    [ProducesResponseType<IReadOnlyList<UserResponse>>(StatusCodes.Status200OK)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status500InternalServerError)]
    public async Task<IActionResult> GetDownline(
        Guid externalId,
        CancellationToken cancellationToken)
    {
        var partners = await getDownlineHandler.HandleAsync(
            new GetDownlineQuery(externalId),
            cancellationToken);

        return partners is null
            ? throw UserNotFound(externalId)
            : Ok(partners.Select(UserResponse.From).ToArray());
    }

    private static ApiProblemException UserNotFound(Guid externalId)
    {
        return ApiProblemException.NotFound(
            "User not found.",
            "user_not_found",
            $"User '{externalId}' does not exist.");
    }
}
