namespace PartnerGraph.Http;

using Common.Observability;
using Microsoft.AspNetCore.Mvc;
using PartnerGraph.Application.Users;

public sealed class ApiProblemDetailsMapper
{
    public ProblemDetails Map(Exception exception, HttpContext httpContext)
    {
        var problem = exception switch
        {
            ApiProblemException apiProblem => FromApiProblem(apiProblem),
            PartnerGraphCycleException => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "The partner graph is corrupted.",
                Detail = exception.Message,
                Extensions =
                {
                    ["code"] = "partner_cycle_detected"
                }
            },
            _ => new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "An unexpected error occurred.",
                Extensions =
                {
                    ["code"] = "internal_server_error"
                }
            }
        };

        problem.Instance = httpContext.Request.Path;
        problem.Extensions["traceId"] = TraceContext.GetTraceId(httpContext);

        return problem;
    }

    private static ProblemDetails FromApiProblem(ApiProblemException exception)
    {
        ProblemDetails problem = exception.Errors is null
            ? new ProblemDetails()
            : new ValidationProblemDetails(
                exception.Errors.ToDictionary(pair => pair.Key, pair => pair.Value));

        problem.Status = exception.StatusCode;
        problem.Title = exception.Title;
        problem.Detail = exception.Message;
        problem.Extensions["code"] = exception.Code;

        return problem;
    }
}
