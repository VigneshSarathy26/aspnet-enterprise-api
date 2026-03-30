using MyApp.Domain.Enums;

namespace MyApp.Application.Interfaces;

/// <summary>Strategy factory — returns the correct normalizer for a given CloudProvider.</summary>
public interface IBillingNormalizerFactory
{
    IProviderNormalizer GetNormalizer(CloudProvider provider);
}
