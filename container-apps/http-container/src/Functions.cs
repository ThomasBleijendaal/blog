using Grpc.Core;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Shared.Storage;

namespace HttpContainer;

public class Functions
{
    private readonly FileServer.FileServerClient _client;

    public Functions(
        FileServer.FileServerClient client)
    {
        _client = client;
    }

    [Function(nameof(RouteFunctionAsync))]
    public async Task<IActionResult> RouteFunctionAsync(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = @"{*route:regex(^[a-z0-9-_\/]+(\.[a-z]{{1,4}})?$)=index.html}")] HttpRequest req,
        string route)
    {
        if (route.EndsWith('/'))
        {
            route += "index.html";
        }

        using var request = _client.GetFileAsync(new GetFileRequest { FileName = route });

        try
        {
            var response = await request;

            req.HttpContext.Response.Headers.CacheControl = $"public, max-age:{60 * 60 * 24}";

            var result = new FileContentResult(response.Contents.ToByteArray(), response.ContentType);

            return result;
        }
        catch (RpcException ex) when (ex.StatusCode == StatusCode.NotFound)
        {
            return new StatusCodeResult(StatusCodes.Status404NotFound);
        }
        catch
        {
            return new StatusCodeResult(StatusCodes.Status500InternalServerError);
        }
    }

    [Function(nameof(ContainerProbe))]
    public IActionResult ContainerProbe(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "container/{action:regex(live|ready|startup)}")] HttpRequest req,
        string action)
        => new OkObjectResult(action);
}
