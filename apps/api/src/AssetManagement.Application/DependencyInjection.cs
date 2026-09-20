using System.Reflection;
using AssetManagement.Application.Approvals;
using AssetManagement.Application.Common.Behaviors;
using AssetManagement.Application.Common.Interfaces;
using AssetManagement.Application.ImportExport;
using AssetManagement.Application.Notifications;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace AssetManagement.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        var assembly = Assembly.GetExecutingAssembly();

        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(assembly));
        services.AddValidatorsFromAssembly(assembly);

        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(AuthorizationBehavior<,>));
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        // After Validation: only requests that already passed authorization/validation reach the audit
        // trail — see AuditBehavior's own remarks and the F8 plan.
        services.AddTransient(typeof(MediatR.IPipelineBehavior<,>), typeof(AuditBehavior<,>));

        // Needs nothing beyond IApplicationDbContext/IClock, so it lives entirely in Application —
        // no Infrastructure-specific concern to justify moving it there (see F4 plan).
        services.AddScoped<IApprovalCoordinator, ApprovalCoordinator>();
        // Same reasoning — only needs IEmailSender (Infrastructure's own thin port), see F8 plan.
        services.AddScoped<INotificationSender, NotificationSender>();
        // Invoked by ImportBatchBackgroundService (Infrastructure), never through ISender — see the F9 plan.
        services.AddScoped<ImportBatchProcessor>();

        return services;
    }
}
