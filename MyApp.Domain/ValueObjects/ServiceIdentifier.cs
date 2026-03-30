namespace MyApp.Domain.ValueObjects;

/// <summary>Identifies a cloud service by its canonical provider name, service name, and region.</summary>
public sealed class ServiceIdentifier
{
    public string ProviderName  { get; private set; } = default!;
    public string ServiceName   { get; private set; } = default!;
    public string Region        { get; private set; } = default!;

    // Required by EF Core owned entity
    private ServiceIdentifier() { }

    public ServiceIdentifier(string providerName, string serviceName, string region)
    {
        ProviderName = providerName ?? throw new ArgumentNullException(nameof(providerName));
        ServiceName  = serviceName  ?? throw new ArgumentNullException(nameof(serviceName));
        Region       = region       ?? throw new ArgumentNullException(nameof(region));
    }

    public override string ToString() => $"{ProviderName}/{ServiceName}/{Region}";
}
