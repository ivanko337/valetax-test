using System.Diagnostics;
using Common.Observability;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Xunit;

namespace Common.Tests.Observability;

public sealed class TraceContextTests
{
    [Fact]
    public void GetTraceId_ReturnsW3CTraceIdFromCurrentActivity()
    {
        using var activity = new Activity("request")
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();

        var traceId = TraceContext.GetTraceId(new DefaultHttpContext());

        Assert.Equal(activity.TraceId.ToString(), traceId);
        Assert.Equal(32, traceId.Length);
    }

    [Fact]
    public void GetTraceId_FallsBackToHttpTraceIdentifier()
    {
        var context = new DefaultHttpContext
        {
            TraceIdentifier = "request-trace-id"
        };

        Assert.Equal("request-trace-id", TraceContext.GetTraceId(context));
    }

    [Fact]
    public void ProblemDetailsFilter_AddsTraceIdToControllerResponse()
    {
        using var activity = new Activity("request")
            .SetIdFormat(ActivityIdFormat.W3C)
            .Start();
        var problem = new ProblemDetails { Title = "Request failed" };
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor(),
            new ModelStateDictionary());
        var resultContext = new ResultExecutingContext(
            actionContext,
            [],
            new ObjectResult(problem),
            controller: new object());

        new TraceIdProblemDetailsFilter().OnResultExecuting(resultContext);

        Assert.Equal(
            activity.TraceId.ToString(),
            problem.Extensions["traceId"]);
    }
}
