using StorageContainer.Probe;
using StorageContainer.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddOptions<StorageServerConfig>().Bind(builder.Configuration.GetSection("Storage"));

builder.Services.AddSingleton<IProbeStatusResolver, ReadinessProbe>();

builder.Services.AddHostedService<TcpProbeService>();

var app = builder.Build();

app.MapGrpcService<StorageServer>();

app.Run();
