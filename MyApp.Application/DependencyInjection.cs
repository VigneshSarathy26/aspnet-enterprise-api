using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using MyApp.Application.Behaviors;
using MyApp.Application.Mappings;
using MyApp.Application.Validators;
using System.Reflection;

namespace MyApp.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        // MediatR — discovers all IRequestHandler<,> implementations
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        // Mapster — register global type mappings
        BillingMappingConfig.RegisterMappings();

        // FluentValidation — auto-discover all AbstractValidator<T> in this assembly
        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
