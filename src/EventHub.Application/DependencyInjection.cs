using EventHub.Application.Interfaces.Admin;
using EventHub.Application.Interfaces.Events;
using EventHub.Application.Interfaces.Identity;
using EventHub.Application.Interfaces.Profiles;
using EventHub.Application.Interfaces.Ticketing;
using EventHub.Application.Mappings;
using EventHub.Application.Services.Admin;
using EventHub.Application.Services.Events;
using EventHub.Application.Services.Identity;
using EventHub.Application.Services.Profiles;
using EventHub.Application.Services.Ticketing;
using EventHub.Application.Validations.Identity;
using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace EventHub.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddAutoMapper(cfg => cfg.AddProfile<MappingProfile>());

        services.AddValidatorsFromAssemblyContaining<RegisterDtoValidator>();

        services.AddScoped<IPermissionService, PermissionService>();
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IEventService, EventService>();
        services.AddScoped<ITicketTypeService, TicketTypeService>();
        services.AddScoped<ITicketService, TicketService>();
        services.AddScoped<IPaymentService, PaymentService>();
        services.AddScoped<IOrganizerService, OrganizerService>();
        services.AddScoped<IAdminService, AdminService>();
        services.AddScoped<IWaitlistService, WaitlistService>();
        services.AddScoped<IWalletService, WalletService>();
        services.AddScoped<IGateStaffService, GateStaffService>();
        services.AddScoped<IDocumentService, DocumentService>();

        return services;
    }
}
