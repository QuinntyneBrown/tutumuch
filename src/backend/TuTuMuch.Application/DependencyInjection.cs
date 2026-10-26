using Microsoft.Extensions.DependencyInjection;
using TuTuMuch.Application.Accounts;

namespace TuTuMuch.Application;

public static class DependencyInjection
{
    /// <summary>Registers the MediatR handlers and application services.</summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(config => config.RegisterServicesFromAssemblyContaining(typeof(DependencyInjection)));
        services.AddScoped<VerificationEmails>();
        services.AddScoped<Notifications.IEmailOutbox, Notifications.EmailOutbox>();
        services.AddScoped<Notifications.INotificationPreferenceReader, Notifications.NotificationPreferenceReader>();
        services.AddSingleton<Notifications.UnsubscribeTokens>();
        services.AddScoped<Listings.ListingPhotoAttacher>();
        services.AddScoped<Orders.OrderCancellation>();
        return services;
    }
}
