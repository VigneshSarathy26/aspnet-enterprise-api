using FluentAssertions;
using MediatR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using MyApp.Domain.Entities;
using MyApp.Domain.Enums;
using MyApp.Domain.ValueObjects;
using MyApp.Infrastructure.Persistence;
using MyApp.Infrastructure.Repositories;
using MyApp.Application.Interfaces;
using Xunit;

namespace MyApp.Tests.Integration;

/// <summary>
/// Integration tests for BillingRepository using EF Core InMemory provider.
/// For full PostgreSQL integration, swap InMemory for Testcontainers.PostgreSql.
/// </summary>
public sealed class BillingRepositoryIntegrationTests : IDisposable
{
    private readonly AppDbContext    _dbContext;
    private readonly BillingRepository _repository;

    public BillingRepositoryIntegrationTests()
    {
        var mediatorMock = new Mock<IMediator>();
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"BillingDb_{Guid.NewGuid()}")
            .Options;

        _dbContext  = new AppDbContext(options, mediatorMock.Object);
        _repository = new BillingRepository(_dbContext);
    }

    private static BillingRecord MakeRecord(string accountId = "acc-001", CloudProvider provider = CloudProvider.AWS)
        => BillingRecord.Create(
            accountId, provider,
            new ServiceIdentifier(provider.ToString(), "ComputeService", "us-east-1"),
            new MoneyAmount(50.00m, "USD"),
            new MoneyAmount(720m, "USD"),
            "Hrs",
            new DateTime(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc),
            new DateTime(2024, 2, 1, 0, 0, 0, DateTimeKind.Utc),
            """{"cost":50}""");

    [Fact]
    public async Task AddAsync_ThenGetById_ShouldReturnRecord()
    {
        var record = MakeRecord();
        await _repository.AddAsync(record);
        await _repository.SaveChangesAsync();

        var retrieved = await _repository.GetByIdAsync(record.Id);
        retrieved.Should().NotBeNull();
        retrieved!.AccountId.Should().Be("acc-001");
        retrieved.Cost.Amount.Should().Be(50.00m);
    }

    [Fact]
    public async Task GetPagedAsync_FilterByProvider_ShouldReturnOnlyMatching()
    {
        await _repository.AddAsync(MakeRecord("acc-001", CloudProvider.AWS));
        await _repository.AddAsync(MakeRecord("acc-001", CloudProvider.Azure));
        await _repository.AddAsync(MakeRecord("acc-001", CloudProvider.GCP));
        await _repository.SaveChangesAsync();

        var filter = new BillingRecordsFilter { Provider = CloudProvider.AWS, Page = 1, PageSize = 10 };
        var (records, total) = await _repository.GetPagedAsync(filter);

        total.Should().Be(1);
        records.All(r => r.Provider == CloudProvider.AWS).Should().BeTrue();
    }

    [Fact]
    public async Task GetPagedAsync_Pagination_ShouldRespectPageSize()
    {
        for (var i = 0; i < 10; i++)
            await _repository.AddAsync(MakeRecord());
        await _repository.SaveChangesAsync();

        var filter = new BillingRecordsFilter { Page = 1, PageSize = 3 };
        var (records, total) = await _repository.GetPagedAsync(filter);

        total.Should().Be(10);
        records.Count.Should().Be(3);
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistStatusChange()
    {
        var record = MakeRecord();
        await _repository.AddAsync(record);
        await _repository.SaveChangesAsync();

        record.MarkNormalized();
        await _repository.UpdateAsync(record);
        await _repository.SaveChangesAsync();

        var updated = await _repository.GetByIdAsync(record.Id);
        updated!.Status.Should().Be(BillingStatus.Normalized);
    }

    public void Dispose() => _dbContext.Dispose();
}
