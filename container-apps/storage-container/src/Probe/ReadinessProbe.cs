namespace StorageContainer.Probe;

internal class ReadinessProbe : IProbeStatusResolver
{
    public ProbeType Type => ProbeType.Readiness;

    public Task<bool> GetStatusAsync()
    {
        // if this returned false, the container won't be used by the load balancer
        return Task.FromResult(true);
    }
}
