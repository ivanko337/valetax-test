namespace ApiGateway;

using Common.Observability;

public static class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);
        builder.AddServiceObservability();

        builder.Services.AddControllers();
        builder.Services
            .AddReverseProxy()
            .LoadFromConfig(builder.Configuration.GetSection("ReverseProxy"));

        var app = builder.Build();

        app.UseExceptionHandler();
        app.MapControllers();
        app.UseSwaggerUI(options =>
        {
            options.RoutePrefix = "swagger";
            options.DocumentTitle = "Valetax services";
            options.SwaggerEndpoint(
                "/partner-graph/swagger/v1/swagger.json",
                "PartnerGraph v1");
            options.SwaggerEndpoint(
                "/commissions/swagger/v1/swagger.json",
                "Commissions v1");
            options.SwaggerEndpoint(
                "/wallet/swagger/v1/swagger.json",
                "Wallet v1");
        });
        app.MapReverseProxy();

        await app.RunAsync();
    }
}
