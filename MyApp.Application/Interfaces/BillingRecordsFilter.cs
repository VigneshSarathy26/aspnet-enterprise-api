namespace MyApp.Application.Interfaces;

/// <summary>Filter parameters for paged billing record queries.</summary>
public sealed class BillingRecordsFilter
{
    public string?                    AccountId { get; init; }
    public Domain.Enums.CloudProvider? Provider  { get; init; }
    public DateTime?                  From      { get; init; }
    public DateTime?                  To        { get; init; }
    public Domain.Enums.BillingStatus? Status   { get; init; }
    public int  Page     { get; init; } = 1;
    public int  PageSize { get; init; } = 50;
}
