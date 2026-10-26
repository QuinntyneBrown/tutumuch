using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using TutuMuch.Application.Abstractions;
using TutuMuch.Application.Catalog;
using TutuMuch.Application.Listings;
using TutuMuch.Application.Notifications;
using TutuMuch.Infrastructure.Accounts;
using TutuMuch.Infrastructure.Carriers;
using TutuMuch.Infrastructure.Email;
using TutuMuch.Infrastructure.Payments;
using TutuMuch.Infrastructure.Payouts;
using TutuMuch.Infrastructure.Persistence;
using TutuMuch.Infrastructure.Photos;

namespace TutuMuch.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers Tutu Much database and the adapters behind the application ports.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TutuMuchDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TutuMuch")));
        services.AddScoped<ITutuMuchDbContext>(sp => sp.GetRequiredService<TutuMuchDbContext>());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IBreachedPasswords, BundledBreachedPasswords>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddScoped<ISellerPayoutAccountReader, PayoutAccountReader>();
        services.Configure<AppOptions>(configuration.GetSection("App"));
        services.Configure<NotificationOptions>(configuration.GetSection("Notifications"));
        services.Configure<SellingOptions>(configuration.GetSection("Selling"));
        services.Configure<PhotoOptions>(configuration.GetSection("Photos"));
        services.Configure<CatalogOptions>(configuration.GetSection("Catalog"));
        services.Configure<PhotoStorageOptions>(configuration.GetSection("PhotoStorage"));
        services.AddSingleton<IImageInspector, SkiaImageInspector>();
        services.AddSingleton<IListingPhotoProcessor, SkiaPhotoProcessor>();
        services.AddSingleton<IPhotoStorage, FileSystemPhotoStorage>();
        services.AddSingleton<IPaymentProvider, UnconfiguredPaymentProvider>();
        services.AddSingleton<ICarrierTracking, UnconfiguredCarrierTracking>();
        services.AddSingleton<IPayoutProvider, UnconfiguredPayoutProvider>();
        return services;
    }
}
