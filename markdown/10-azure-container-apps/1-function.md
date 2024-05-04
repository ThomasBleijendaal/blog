---
theme: Azure Container Apps
title: Azure Function
visible: True
---

To start hosting this blog, I'll employ an Azure Function to serve the handwritten html files from a blob storage. It will also serve as a baseline to compare the Container App solution to.

The base implementation of this Azure Function is something like this:

```csharp
[Function(nameof(RouteFunctionAsync))]
public async Task<IActionResult> RouteFunctionAsync(
    [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "{*route=index.html}")] HttpRequest req,
    [BlobInput("static/{route}")] BlobClient? blob,
    string route)
{
    if (blob == null || !await blob.ExistsAsync())
    {
        return new NotFoundResult();
    }

    var contentType = Path.GetExtension(route) switch
    {
        ".html" => "text/html",
        ".css" => "text/css",
        ".ico" => "image/x-icon",
        _ => "application/octet-stream"
    };

    return new FileStreamResult(await blob.OpenReadAsync(), contentType);
}
```
This consumption tier linux function app has an average response time of around 50ms if I do some manual tests in Postman, and gets to around 130ms when I hit it with 25 concurrent users using some load test (I've used octoperf for that). I've saved the test report so will compare it to the Container Apps version.
