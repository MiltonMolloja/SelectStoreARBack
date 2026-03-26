using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using SelectStoreAR.Application.Behaviors;

namespace SelectStoreAR.Application.DependencyInjection;

public static class ApplicationServiceExtensions
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        System.Reflection.Assembly assembly = typeof(ApplicationServiceExtensions).Assembly;

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(assembly);
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        });

        services.AddValidatorsFromAssembly(assembly);

        return services;
    }
}
