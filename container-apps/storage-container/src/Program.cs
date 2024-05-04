using StorageContainer.Server;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddGrpc();
builder.Services.AddOptions<StorageServerConfig>().Bind(builder.Configuration.GetSection("Storage"));

var app = builder.Build();

app.MapGrpcService<StorageServer>();

// when k8s probes support http/2 these endpoints become useful
app.MapGet("/container/{action:regex(live|ready|startup)}", (string action, HttpContext context) =>
{
    return Results.Ok($"{context.Request.Protocol} {action}");
});

app.Run();
