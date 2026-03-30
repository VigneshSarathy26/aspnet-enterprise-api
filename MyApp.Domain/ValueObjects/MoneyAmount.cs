namespace MyApp.Domain.ValueObjects;

/// <summary>Immutable monetary amount with ISO 4217 currency code.</summary>
public sealed class MoneyAmount
{
    public decimal Amount   { get; private set; }
    public string  Currency { get; private set; } = default!;   // ISO 4217

    // Required by EF Core owned entity
    private MoneyAmount() { }

    public MoneyAmount(decimal amount, string currency)
    {
        if (amount < 0)
            throw new ArgumentException("Amount cannot be negative.", nameof(amount));
        if (string.IsNullOrWhiteSpace(currency) || currency.Length != 3)
            throw new ArgumentException("Currency must be a valid 3-letter ISO 4217 code.", nameof(currency));

        Amount   = Math.Round(amount, 8, MidpointRounding.AwayFromZero);
        Currency = currency.ToUpperInvariant();
    }

    public static MoneyAmount Zero(string currency) => new(0m, currency);

    public MoneyAmount Add(MoneyAmount other)
    {
        if (!Currency.Equals(other.Currency, StringComparison.Ordinal))
            throw new InvalidOperationException(
                $"Cannot add amounts in different currencies: {Currency} vs {other.Currency}");
        return new MoneyAmount(Amount + other.Amount, Currency);
    }

    public override string ToString() => $"{Amount:F8} {Currency}";
}
