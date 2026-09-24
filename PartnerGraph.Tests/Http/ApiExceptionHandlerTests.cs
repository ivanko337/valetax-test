namespace PartnerGraph.Tests.Http;

using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using PartnerGraph.Application.Users;
using PartnerGraph.Http;
using Xunit;

public sealed class ApiExceptionHandlerTests
{
    [Fact]
    public async Task Handler_MapsExceptionAndRequestContextToProblemDetails()
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(
            new ApiProblemDetailsMapper(),
            problemDetailsService,
            NullLogger<ApiExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext
        {
            TraceIdentifier = "request-trace-id"
        };
        httpContext.Request.Method = HttpMethods.Get;
        httpContext.Request.Path = "/api/users/user-id/upline";

        var handled = await handler.TryHandleAsync(
            httpContext,
            new PartnerGraphCycleException(Guid.NewGuid(), "upline"),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(
            StatusCodes.Status500InternalServerError,
            httpContext.Response.StatusCode);

        var problem = Assert.IsType<ProblemDetails>(
            problemDetailsService.WrittenContext?.ProblemDetails);
        Assert.Equal("partner_cycle_detected", problem.Extensions["code"]);
        Assert.Equal("/api/users/user-id/upline", problem.Instance);
        Assert.Equal("request-trace-id", problem.Extensions["traceId"]);
    }

    [Fact]
    public async Task Handler_WritesValidationProblemForExpectedApiFailure()
    {
        var problemDetailsService = new CapturingProblemDetailsService();
        var handler = new ApiExceptionHandler(
            new ApiProblemDetailsMapper(),
            problemDetailsService,
            NullLogger<ApiExceptionHandler>.Instance);
        var httpContext = new DefaultHttpContext();

        var handled = await handler.TryHandleAsync(
            httpContext,
            ApiProblemException.Validation("externalId", "ExternalId is invalid."),
            CancellationToken.None);

        Assert.True(handled);
        Assert.Equal(StatusCodes.Status400BadRequest, httpContext.Response.StatusCode);

        var problem = Assert.IsType<ValidationProblemDetails>(
            problemDetailsService.WrittenContext?.ProblemDetails);
        Assert.Equal("validation_error", problem.Extensions["code"]);
        Assert.Equal("ExternalId is invalid.", Assert.Single(problem.Errors["externalId"]));
    }

    private sealed class CapturingProblemDetailsService : IProblemDetailsService
    {
        public ProblemDetailsContext? WrittenContext { get; private set; }

        public ValueTask WriteAsync(ProblemDetailsContext context)
        {
            WrittenContext = context;
            return ValueTask.CompletedTask;
        }

        public ValueTask<bool> TryWriteAsync(ProblemDetailsContext context)
        {
            WrittenContext = context;
            return ValueTask.FromResult(true);
        }
    }
}
