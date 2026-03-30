using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyApp.Domain.Entities;
using System.Text.Json;

namespace MyApp.Infrastructure.Persistence.Configurations;

public sealed class BillingRecordConfiguration : IEntityTypeConfiguration<BillingRecord>
{
    private static readonly JsonSerializerOptions JsonOpts = new(JsonSerializerDefaults.Web);

    public void Configure(EntityTypeBuilder<BillingRecord> b)
    {
        b.ToTable("billing_records");
        b.HasKey(x => x.Id);

        b.Property(x => x.Id).HasColumnName("id").ValueGeneratedNever();
        b.Property(x => x.AccountId).HasColumnName("account_id").HasMaxLength(128).IsRequired();

        b.Property(x => x.Provider)
         .HasColumnName("provider")
         .HasConversion<string>()
         .HasMaxLength(32)
         .IsRequired();

        // ServiceIdentifier — owned entity (flattened columns)
        b.OwnsOne(x => x.Service, svc =>
        {
            svc.Property(s => s.ProviderName).HasColumnName("svc_provider").HasMaxLength(64).IsRequired();
            svc.Property(s => s.ServiceName ).HasColumnName("svc_name").HasMaxLength(256).IsRequired();
            svc.Property(s => s.Region      ).HasColumnName("svc_region").HasMaxLength(64).IsRequired();
        });

        // MoneyAmount — Cost
        b.OwnsOne(x => x.Cost, cost =>
        {
            cost.Property(c => c.Amount  ).HasColumnName("cost_amount").HasPrecision(18, 8).IsRequired();
            cost.Property(c => c.Currency).HasColumnName("cost_currency").HasMaxLength(3).IsRequired();
        });

        // MoneyAmount — UsageQuantity
        b.OwnsOne(x => x.UsageQuantity, uq =>
        {
            uq.Property(u => u.Amount  ).HasColumnName("usage_amount").HasPrecision(18, 8).IsRequired();
            uq.Property(u => u.Currency).HasColumnName("usage_currency").HasMaxLength(3).IsRequired();
        });

        b.Property(x => x.UsageUnit    ).HasColumnName("usage_unit").HasMaxLength(64).IsRequired();
        b.Property(x => x.PeriodStart  ).HasColumnName("period_start").IsRequired();
        b.Property(x => x.PeriodEnd    ).HasColumnName("period_end").IsRequired();
        b.Property(x => x.Status       ).HasColumnName("status").HasConversion<string>().HasMaxLength(32).IsRequired();
        b.Property(x => x.RawPayload   ).HasColumnName("raw_payload").HasColumnType("jsonb").IsRequired();
        b.Property(x => x.CorrelationId).HasColumnName("correlation_id").HasMaxLength(64);
        b.Property(x => x.FailureReason).HasColumnName("failure_reason").HasMaxLength(1024);
        b.Property(x => x.CreatedAt    ).HasColumnName("created_at").IsRequired();
        b.Property(x => x.UpdatedAt    ).HasColumnName("updated_at");

        b.Property(x => x.Tags)
         .HasColumnName("tags")
         .HasColumnType("jsonb")
         .HasConversion(
             v => JsonSerializer.Serialize(v, JsonOpts),
             v => JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOpts) ?? new());

        // Indexes for common query patterns
        b.HasIndex(x => x.AccountId   ).HasDatabaseName("ix_br_account_id");
        b.HasIndex(x => x.Provider    ).HasDatabaseName("ix_br_provider");
        b.HasIndex(x => x.Status      ).HasDatabaseName("ix_br_status");
        b.HasIndex(x => x.CorrelationId).HasDatabaseName("ix_br_correlation_id");
        b.HasIndex(x => new { x.PeriodStart, x.PeriodEnd }).HasDatabaseName("ix_br_period");
        b.HasIndex(x => new { x.AccountId, x.PeriodStart, x.PeriodEnd }).HasDatabaseName("ix_br_account_period");
    }
}
