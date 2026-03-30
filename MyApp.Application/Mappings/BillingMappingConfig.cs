using Mapster;
using MyApp.Application.DTOs;
using MyApp.Domain.Entities;

namespace MyApp.Application.Mappings;

/// <summary>Mapster type-adapter configuration for Domain → DTO projection.</summary>
public static class BillingMappingConfig
{
    public static void RegisterMappings()
    {
        TypeAdapterConfig<BillingRecord, BillingRecordDto>.NewConfig()
            .Map(dest => dest.Provider,      src => src.Provider.ToString())
            .Map(dest => dest.ServiceName,   src => src.Service.ServiceName)
            .Map(dest => dest.Region,        src => src.Service.Region)
            .Map(dest => dest.Cost,          src => src.Cost.Amount)
            .Map(dest => dest.Currency,      src => src.Cost.Currency)
            .Map(dest => dest.UsageQuantity, src => src.UsageQuantity.Amount)
            .Map(dest => dest.Status,        src => src.Status.ToString());
    }
}
