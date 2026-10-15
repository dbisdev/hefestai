using System.Reflection;
using FluentValidation;
using Loremaster.Application.Common.Behaviors;
using Loremaster.Application.Common.Interfaces;
using Loremaster.Application.Common.Services;
using Loremaster.Application.Features.EntityGeneration.Services;
using Loremaster.Application.Features.EntityTemplates.Commands.ExtractTemplatesFromManual.Services;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace Loremaster.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplicationServices(this IServiceCollection services)
    {
        services.AddValidatorsFromAssembly(Assembly.GetExecutingAssembly());

        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly());
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
            cfg.AddBehavior(typeof(IPipelineBehavior<,>), typeof(PerformanceBehavior<,>));
        });

        // Entity generation services
        services.AddScoped<IGameSystemAuthorizationService, GameSystemAuthorizationService>();
        services.AddScoped<IPromptBuilderService, PromptBuilderService>();
        services.AddScoped<IImageGenerationService, ImageGenerationService>();
        services.AddScoped<IGenerationTrackingService, GenerationTrackingService>();
        services.AddScoped<ITemplateResolutionService, TemplateResolutionService>();

        // Template extraction services (internal — registered via concrete interface)
        services.AddScoped<IEntityExtractionRagService, EntityExtractionRagService>();
        services.AddScoped<IEntityTypeParser, EntityTypeParser>();
        services.AddScoped<IFieldDefinitionConverter, FieldDefinitionConverter>();

        return services;
    }
}
