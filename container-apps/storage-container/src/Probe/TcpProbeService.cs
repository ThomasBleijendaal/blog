namespace StorageContainer.Probe;

internal class TcpProbeService : BackgroundService
{
    private static Dictionary<int, (IProbeStatusResolver statusProvider, TcpProbe? probe)> _probes = new();

    public TcpProbeService(
        IEnumerable<IProbeStatusResolver> probeStatusResolvers)
    {
        foreach (var resolver in probeStatusResolvers)
        {
            var port = GetProbePort(resolver.Type);
            _probes.Add(port, (resolver, new TcpProbe(port)));
        }

        foreach (var type in Enum.GetValues<ProbeType>())
        {
            var port = GetProbePort(type);

            if (!_probes.ContainsKey(port))
            {
                var statusResolver = new AlwaysHealthyProbeStatusResolver
                {
                    Type = type
                };

                _probes[port] = (statusResolver, new TcpProbe(port));
            }
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        foreach (var probe in _probes)
        {
            probe.Value.probe?.StartProbe();
        }

        do
        {
            foreach (var kv in _probes.ToArray())
            {
                var port = kv.Key;
                var (status, probe) = kv.Value;

                if (!await status.GetStatusAsync())
                {
                    Console.WriteLine($"Probe {port}: Unhealthy");

                    probe?.StopProbe();
                    _probes[port] = (status, null);
                }
                else if (probe == null)
                {
                    Console.WriteLine($"Probe {port}: Restore");

                    var newProbe = new TcpProbe(port);
                    newProbe.StartProbe();
                    _probes[port] = (status, newProbe);
                }
                else
                {
                    Console.WriteLine($"Probe {port}: Healthy");
                }
            }

            await Task.Delay(1000, stoppingToken);
        }
        while (!stoppingToken.IsCancellationRequested);

        foreach (var kv in _probes)
        {
            var (_, probe) = kv.Value;

            probe?.StopProbe();
        }
    }

    private int GetProbePort(ProbeType type)
        => type switch
        {
            ProbeType.Startup => 8081,
            ProbeType.Liveness => 8082,
            ProbeType.Readiness => 8083,
            _ => 8084
        };

    private sealed class AlwaysHealthyProbeStatusResolver : IProbeStatusResolver
    {
        public ProbeType Type { get; set; }

        public Task<bool> GetStatusAsync() => Task.FromResult(true);
    }
}
