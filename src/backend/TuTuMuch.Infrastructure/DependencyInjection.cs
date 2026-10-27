using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using TuTuMuch.Application.Abstractions;
using TuTuMuch.Application.Catalog;
using TuTuMuch.Application.Launch;
using TuTuMuch.Application.Listings;
using TuTuMuch.Application.Notifications;
using TuTuMuch.Infrastructure.Accounts;
using TuTuMuch.Infrastructure.Carriers;
using TuTuMuch.Infrastructure.Email;
using TuTuMuch.Infrastructure.Payments;
using TuTuMuch.Infrastructure.Payouts;
using TuTuMuch.Infrastructure.Persistence;
using TuTuMuch.Infrastructure.Photos;

namespace TuTuMuch.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers TuTuMuch database and the adapters behind the application ports.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddDbContext<TuTuMuchDbContext>(options =>
            options.UseSqlServer(configuration.GetConnectionString("TuTuMuch")));
        services.AddScoped<ITuTuMuchDbContext>(sp => sp.GetRequiredService<TuTuMuchDbContext>());
        services.AddSingleton(TimeProvider.System);
        services.AddSingleton<IPasswordHasher, IdentityPasswordHasher>();
        services.AddSingleton<IBreachedPasswords, BundledBreachedPasswords>();
        services.AddSingleton<IEmailSender, LoggingEmailSender>();
        services.AddScoped<ISellerPayoutAccountReader, PayoutAccountReader>();
        services.Configure<AppOptions>(configuration.GetSection("App"));
        services.Configure<NotificationOptions>(configuration.GetSection("Notifications"));
        services.Configure<SellingOptions>(configuration.GetSection("Selling"));
        services.AddOptions<LaunchOptions>().Bind(configuration.GetSection(LaunchOptions.Section)).ValidateOnStart();
        services.AddSingleton<IValidateOptions<LaunchOptions>, LaunchOptionsValidator>();
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
