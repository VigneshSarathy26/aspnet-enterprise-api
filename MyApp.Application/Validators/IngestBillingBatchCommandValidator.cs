using FluentValidation;
using MyApp.Application.Commands.IngestBillingBatch;
using MyApp.Domain.Enums;

namespace MyApp.Application.Validators;

public sealed class IngestBillingBatchCommandValidator
    : AbstractValidator<IngestBillingBatchCommand>
{
    private static readonly HashSet<string> ValidProviders =
        Enum.GetNames<CloudProvider>()
            .Where(n => n != nameof(CloudProvider.Unknown))
            .ToHashSet(StringComparer.OrdinalIgnoreCase);

    public IngestBillingBatchCommandValidator()
    {
        RuleFor(x => x.Provider)
            .NotEmpty()
            .Must(p => ValidProviders.Contains(p))
            .WithMessage($"Provider must be one of: {string.Join(", ", ValidProviders)}.");

        RuleFor(x => x.AccountId)
            .NotEmpty()
            .MaximumLength(128);

        RuleFor(x => x.RawPayloads)
            .NotEmpty()
            .WithMessage("RawPayloads cannot be empty.")
            .Must(p => p.Count() <= 1000)
            .WithMessage("Batch size cannot exceed 1000 records.");

        RuleForEach(x => x.RawPayloads)
            .NotEmpty()
            .WithMessage("Each payload must be a non-empty JSON string.");
    }
}
