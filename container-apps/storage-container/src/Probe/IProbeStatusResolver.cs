namespace StorageContainer.Probe;

internal interface IProbeStatusResolver
{
    ProbeType Type { get; }

    Task<bool> GetStatusAsync();
}
