using System.Diagnostics;
using Microsoft.AspNetCore.Http;

namespace Common.Observability;

public static class TraceContext
{
    public static string GetTraceId(HttpContext httpContext)
    {
        ArgumentNullException.ThrowIfNull(httpContext);

        var traceId = Activity.Current?.TraceId ?? default;
        return traceId != default
            ? traceId.ToString()
            : httpContext.TraceIdentifier;
    }
}
