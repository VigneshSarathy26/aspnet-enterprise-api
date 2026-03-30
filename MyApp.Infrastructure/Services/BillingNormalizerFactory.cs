using MyApp.Application.Interfaces;
using MyApp.Domain.Enums;

namespace MyApp.Infrastructure.Services;

/// <summary>
/// Strategy factory: resolves the correct IProviderNormalizer based on the cloud provider.
/// All normalizers are injected via DI, eliminating the use of a service locator.
/// </summary>
public sealed class BillingNormalizerFactory : IBillingNormalizerFactory
{
    private readonly IReadOnlyDictionary<CloudProvider, IProviderNormalizer> _normalizers;

    public BillingNormalizerFactory(IEnumerable<IProviderNormalizer> normalizers)
    {
        _normalizers = normalizers.ToDictionary(n => n.SupportedProvider);
    }

    public IProviderNormalizer GetNormalizer(CloudProvider provider)
    {
        if (_normalizers.TryGetValue(provider, out var normalizer))
            return normalizer;

        throw new NotSupportedException(
            $"No normalizer registered for cloud provider '{provider}'. " +
            $"Supported: {string.Join(", ", _normalizers.Keys)}");
    }
}
