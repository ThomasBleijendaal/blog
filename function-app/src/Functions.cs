using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;

namespace StaticBlog;

public class Functions
{
    //[Function(nameof(RouteFunctionAsync))]
    //public async Task<IActionResult> RouteFunctionAsync(
    //    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*route=index.html}")] HttpRequest req,
    //    [BlobInput("static/{route}")] BlobClient? blob,
    //    string route)
    //{
    //    if (blob == null || !await blob.ExistsAsync())
    //    {
    //        return new NotFoundResult();
    //    }

    //    var contentType = Path.GetExtension(route) switch
    //    {
    //        ".html" => "text/html",
    //        ".css" => "text/css",
    //        ".ico" => "image/x-icon",
    //        _ => "application/octet-stream"
    //    };

    //    return new FileStreamResult(await blob.OpenReadAsync(), contentType);
    //}

    [Function(nameof(RouteFunction))]
    public IActionResult RouteFunction(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*route=index.html}")] HttpRequest req,
        string route)
    {
        var file = File.OpenRead(@$"..\..\..\..\..\output\{route.Replace("/", @"\")}");

        var contentType = Path.GetExtension(route) switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".ico" => "image/x-icon",
            _ => "application/octet-stream"
        };

        return new FileStreamResult(file, contentType);
    }
}
