using CombinationGenerator.Application.Sessions;
using CombinationGenerator.Infrastructure.Sessions;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;

namespace CombinationGenerator.Infrastructure;

/// <summary>רישום של כל מה ש-Infrastructure מספק, בשורה אחת מ-Program.cs.</summary>
public static class DependencyInjection
{
    /// <summary>
    /// רושם את המגירה בזיכרון כמימוש של ISessionStore, את ההגדרות שלה ואת המנקה ברקע.
    /// </summary>
    public static IServiceCollection AddInMemorySessionStore(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<SessionStoreOptions>()
            .Bind(configuration.GetSection(SessionStoreOptions.SectionName))
            .Validate(options => options.IsValid, "SessionStore options must all be positive, and CleanupIntervalSeconds at most 86400.")
            .ValidateOnStart();

        services.TryAddSingleton(TimeProvider.System);

        // Singleton: מגירה אחת לכל השרת. אם הייתה נוצרת מגירה חדשה לכל בקשה, הסימניות היו נעלמות
        services.AddSingleton<InMemorySessionStore>();
        services.AddSingleton<ISessionStore>(provider => provider.GetRequiredService<InMemorySessionStore>());

        services.AddHostedService<SessionCleanupService>();

        return services;
    }
}
