using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using MyApp.Application.Commands.IngestBillingRecord;
using MyApp.Application.DTOs;
using MyApp.Application.Interfaces;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;
using MyApp.Domain.ValueObjects;
using Xunit;

namespace MyApp.Tests.Unit.Application;

public sealed class IngestBillingRecordHandlerTests
{
    private readonly Mock<IBillingRepository>       _repoMock      = new();
    private readonly Mock<IBillingNormalizerFactory> _factoryMock  = new();
    private readonly Mock<IProviderNormalizer>       _normalizerMock = new();

    private IngestBillingRecordHandler CreateHandler() => new(
        _repoMock.Object,
        _factoryMock.Object,
        NullLogger<IngestBillingRecordHandler>.Instance);

    private static BillingRecord MakeFakeRecord() =>
        BillingRecord.Create(
            "account-123", CloudProvider.AWS,
            new ServiceIdentifier("AWS", "EC2", "us-east-1"),
            new MoneyAmount(5.0m, "USD"),
            new MoneyAmount(1.0m, "USD"),
            "Hrs",
            DateTime.UtcNow.Date,
            DateTime.UtcNow.Date.AddDays(1),
            "{}");

    [Fact]
    public async Task Handle_ValidCommand_ReturnsIngestionResponse()
    {
        var fakeRecord = MakeFakeRecord();
        _normalizerMock.Setup(n => n.NormalizeAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                       .ReturnsAsync(fakeRecord);
        _factoryMock.Setup(f => f.GetNormalizer(CloudProvider.AWS))
                    .Returns(_normalizerMock.Object);
        _repoMock.Setup(r => r.AddAsync(It.IsAny<BillingRecord>(), It.IsAny<CancellationToken>()))
                 .Returns(Task.CompletedTask);
        _repoMock.Setup(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()))
                 .ReturnsAsync(1);

        var command = new IngestBillingRecordCommand("AWS", "account-123", "{}", "corr-1");
        var handler = CreateHandler();
        var result  = await handler.Handle(command, CancellationToken.None);

        result.Should().NotBeNull();
        result.RecordId.Should().Be(fakeRecord.Id);
        result.Provider.Should().Be("AWS");
        _repoMock.Verify(r => r.AddAsync(fakeRecord, It.IsAny<CancellationToken>()), Times.Once);
        _repoMock.Verify(r => r.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task Handle_UnknownProvider_ThrowsArgumentException()
    {
        var command = new IngestBillingRecordCommand("UNKNOWN_PROVIDER", "account-123", "{}", null);
        var handler = CreateHandler();

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<ArgumentException>()
                 .WithMessage("*UNKNOWN_PROVIDER*");
    }

    [Fact]
    public async Task Handle_NormalizerNotFound_PropagatesException()
    {
        _factoryMock.Setup(f => f.GetNormalizer(CloudProvider.GCP))
                    .Throws(new NotSupportedException("No normalizer for GCP"));

        var command = new IngestBillingRecordCommand("GCP", "acc", "{}", null);
        var handler = CreateHandler();

        var act = async () => await handler.Handle(command, CancellationToken.None);
        await act.Should().ThrowAsync<NotSupportedException>();
    }
}
