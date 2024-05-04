using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Shared.Storage;

var host = new HostBuilder()
    .ConfigureFunctionsWebApplication()
    .ConfigureServices((context, services) =>
    {
        var grpcAddress = context.Configuration["StorageContainer:Address"]
            ?? throw new InvalidOperationException("Storage Container Address unknown");

        services.AddGrpcClient<FileServer.FileServerClient>(options =>
        {
            options.Address = new Uri(grpcAddress);
        });

        services.AddApplicationInsightsTelemetryWorkerService();
        services.ConfigureFunctionsApplicationInsights();
    })
    .Build();

host.Run();
