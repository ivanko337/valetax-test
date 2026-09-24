using System.Diagnostics;
using Common.Kafka;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using OpenTelemetry.Resources;
using OpenTelemetry.Trace;

namespace Common.Observability;

public static class ObservabilityExtensions
{
    public static WebApplicationBuilder AddServiceObservability(
        this WebApplicationBuilder builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        Activity.DefaultIdFormat = ActivityIdFormat.W3C;
        Activity.ForceDefaultIdFormat = true;

        builder.Logging.Configure(options =>
        {
            options.ActivityTrackingOptions =
                ActivityTrackingOptions.TraceId |
                ActivityTrackingOptions.SpanId |
                ActivityTrackingOptions.ParentId;
        });
        builder.Logging.AddSimpleConsole(options => options.IncludeScopes = true);

        builder.Services.AddProblemDetails(options =>
        {
            options.CustomizeProblemDetails = context =>
            {
                context.ProblemDetails.Extensions["traceId"] =
                    TraceContext.GetTraceId(context.HttpContext);
            };
        });

        builder.Services.Configure<Microsoft.AspNetCore.Mvc.MvcOptions>(options =>
            options.Filters.Add<TraceIdProblemDetailsFilter>());

        var tracing = builder.Services
            .AddOpenTelemetry()
            .ConfigureResource(resource => resource.AddService(
                serviceName: builder.Environment.ApplicationName))
            .WithTracing(provider => provider
                .AddSource(KafkaTraceContext.ActivitySourceName)
                .AddAspNetCoreInstrumentation()
                .AddHttpClientInstrumentation());

        var otlpEndpoint = builder.Configuration["OpenTelemetry:OtlpEndpoint"]
            ?? builder.Configuration["OTEL_EXPORTER_OTLP_ENDPOINT"];

        if (Uri.TryCreate(otlpEndpoint, UriKind.Absolute, out var endpoint))
        {
            tracing.WithTracing(provider => provider.AddOtlpExporter(options =>
                options.Endpoint = endpoint));
        }

        return builder;
    }
}
