using FluentAssertions;
using FluentValidation;
using MyApp.Application.Commands.IngestBillingRecord;
using MyApp.Application.Validators;
using Xunit;

namespace MyApp.Tests.Unit.Application;

public sealed class IngestionRequestValidatorTests
{
    private readonly IngestBillingRecordCommandValidator _validator = new();

    [Theory]
    [InlineData("AWS")]
    [InlineData("azure")]
    [InlineData("GCP")]
    public void ValidProvider_ShouldPassValidation(string provider)
    {
        var cmd    = new IngestBillingRecordCommand(provider, "account-123", "{}", null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeTrue();
    }

    [Theory]
    [InlineData("")]
    [InlineData("DIGITALOCEAN")]
    [InlineData("Unknown")]
    public void InvalidProvider_ShouldFailValidation(string provider)
    {
        var cmd    = new IngestBillingRecordCommand(provider, "account-123", "{}", null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "Provider");
    }

    [Fact]
    public void EmptyAccountId_ShouldFailValidation()
    {
        var cmd    = new IngestBillingRecordCommand("AWS", "", "{}", null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "AccountId");
    }

    [Fact]
    public void EmptyRawPayload_ShouldFailValidation()
    {
        var cmd    = new IngestBillingRecordCommand("AWS", "account-123", "", null);
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "RawPayload");
    }

    [Fact]
    public void LongCorrelationId_ShouldFailValidation()
    {
        var cmd    = new IngestBillingRecordCommand("AWS", "account-123", "{}", new string('x', 65));
        var result = _validator.Validate(cmd);
        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(e => e.PropertyName == "CorrelationId");
    }
}
