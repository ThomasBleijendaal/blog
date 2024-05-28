using System.Net;
using System.Net.Sockets;
using System.Text;

namespace StorageContainer.Probe;

internal class TcpProbe
{
    private readonly int _port;
    private readonly CancellationTokenSource _cts = new();
    private Task? _probeTask;

    public TcpProbe(int port)
    {
        _port = port;
    }

    public void StartProbe()
    {
        Console.WriteLine($"Probe {_port}: Starting");
        _probeTask = StartProbeAsync();
    }

    public void StopProbe()
    {
        Console.WriteLine($"Probe {_port}: Stopping");
        _cts.Cancel();
        _probeTask = null;
    }

    private async Task StartProbeAsync()
    {
        var ipEndPoint = new IPEndPoint(IPAddress.Any, _port);
        using var listener = new TcpListener(ipEndPoint);

        try
        {
            listener.Start();

            do
            {
                using var handler = await listener.AcceptTcpClientAsync(_cts.Token);

                Console.WriteLine($"Probe {_port}: Probing");

                await using var stream = handler.GetStream();

                var message = "I'm OK";
                var okBytes = Encoding.UTF8.GetBytes(message);
                await stream.WriteAsync(okBytes);

                Console.WriteLine($"Probe {_port}: Probed");
            }
            while (!_cts.IsCancellationRequested);
        }
        finally
        {
            listener.Stop();
        }
    }
}
