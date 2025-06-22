using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Company.Function;
using Microsoft.Extensions.Logging;
using Microsoft.ApplicationInsights.Extensibility;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices(services =>
    {
        services.AddHttpClient<ProcessOrderFunction>();

        services.AddApplicationInsightsTelemetryWorkerService();
        services.Configure<TelemetryConfiguration>(config =>
        {
            config.ConnectionString = Environment.GetEnvironmentVariable("APPLICATIONINSIGHTS_CONNECTION_STRING");
        });
    })
    .Build();

host.Run();
