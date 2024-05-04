using Google.Protobuf;
using Grpc.Core;
using Microsoft.Extensions.Options;
using Shared.Storage;

namespace StorageContainer.Server;

public class StorageServer : FileServer.FileServerBase
{
    private readonly StorageServerConfig _config;

    public StorageServer(
        IOptions<StorageServerConfig> config)
    {
        _config = config.Value;
    }

    public override Task<FileResponse> GetFile(GetFileRequest request, ServerCallContext context)
    {
        if (request.FileName.Contains(".."))
        {
            throw new RpcException(new Status(StatusCode.InvalidArgument, "Bad request"));
        }

        var path = Path.Combine(_config.FileRoot, request.FileName);

        var contentType = Path.GetExtension(request.FileName) switch
        {
            ".html" => "text/html",
            ".css" => "text/css",
            ".ico" => "image/x-icon",
            ".txt" => "text/plain",
            _ => throw new RpcException(new Status(StatusCode.NotFound, "Incorrect file extension"))
        };

        if (!File.Exists(path))
        {
            return Task.FromException<FileResponse>(new RpcException(new Status(StatusCode.NotFound, "File not found")));
        }

        return Task.FromResult(new FileResponse
        {
            Contents = ByteString.FromStream(File.OpenRead(path)),
            ContentType = contentType
        });
    }
}
