using FluentValidation;
using MyApp.Application.Commands.IngestBillingRecord;
using MyApp.Domain.Enums;

namespace MyApp.Application.Validators;

public sealed class IngestBillingRecordCommandValidator
    : AbstractValidator<IngestBillingRecordCommand>
{
    private static readonly HashSet<string> ValidProviders =
        Enum.GetNames<CloudProvider>()
            .Where(n => n != nameof(CloudProvider.Unknown))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public IngestBillingRecordCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .Must(p => ValidProviders.Contains(p))
            .WithMessage($"Provider must be one of: {string.Join(", ", ValidProviders)}.");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.RawPayload)
            .NotEmpty()
            .WithMessage("RawPayload must be a non-empty JSON string.");

        RuleFor(x => x.CorrelationId)
            .MaximumLength(64)
            .When(x => x.CorrelationId is not null);
    }
}
