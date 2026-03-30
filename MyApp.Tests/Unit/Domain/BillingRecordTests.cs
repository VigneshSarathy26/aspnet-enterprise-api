using FluentAssertions;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;
using MyApp.Domain.ValueObjects;
using Xunit;

namespace MyApp.Tests.Unit.Domain;

public sealed class BillingRecordTests
{
    private static BillingRecord CreateValidRecord(decimal cost = 10.50m) =>
        BillingRecord.Create(
            accountId     : "account-123",
            provider      : CloudProvider.AWS,
            service       : new ServiceIdentifier("AWS", "AmazonEC2", "us-east-1"),
            cost          : new MoneyAmount(cost, "USD"),
            usageQuantity : new MoneyAmount(2m, "USD"),
            usageUnit     : "Hrs",
            periodStart   : new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            periodEnd     : new DateTime(2024, 1, 2, 0, 0, 0, DateTimeKind.Utc),
            rawPayload    : """{"lineItem/UnblendedCost":"10.50"}""");

    [Fact]
    public void Create_WithValidData_ShouldSetPendingStatus()
    {
        var record = CreateValidRecord();
        record.Status.Should().Be(BillingStatus.Pending);
    }

    [Fact]
    public void Create_ShouldRaiseBillingRecordIngestedEvent()
    {
        var record = CreateValidRecord();
        record.DomainEvents.Should().ContainSingle()
              .Which.Should().BeOfType<MyApp.Domain.Events.BillingRecordIngested>();
    }

    [Fact]
    public void MarkNormalized_ShouldChangeStatusToNormalized()
    {
        var record = CreateValidRecord();
        record.MarkNormalized();
        record.Status.Should().Be(BillingStatus.Normalized);
        record.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public void MarkFailed_ShouldSetFailureReason()
    {
        var record = CreateValidRecord();
        record.MarkFailed("Parse error: invalid JSON");
        record.Status.Should().Be(BillingStatus.Failed);
        record.FailureReason.Should().Be("Parse error: invalid JSON");
    }

    [Fact]
    public void Create_WithPeriodStartAfterEnd_ShouldThrow()
    {
        var act = () => BillingRecord.Create(
            accountId     : "acc",
            provider      : CloudProvider.AWS,
            service       : new ServiceIdentifier("AWS", "EC2", "us-east-1"),
            cost          : new MoneyAmount(1, "USD"),
            usageQuantity : new MoneyAmount(1, "USD"),
            usageUnit     : "Hrs",
            periodStart   : new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            periodEnd     : new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            rawPayload    : "{}");

        act.Should().Throw<ArgumentException>()
           .WithMessage("*PeriodStart*");
    }

    [Fact]
    public void MoneyAmount_AddDifferentCurrencies_ShouldThrow()
    {
        var usd = new MoneyAmount(10m, "USD");
        var eur = new MoneyAmount(8m,  "EUR");
        var act = () => usd.Add(eur);
        act.Should().Throw<InvalidOperationException>();
    }

    [Fact]
    public void MoneyAmount_NegativeAmount_ShouldThrow()
    {
        var act = () => new MoneyAmount(-1m, "USD");
        act.Should().Throw<ArgumentException>();
    }
}
