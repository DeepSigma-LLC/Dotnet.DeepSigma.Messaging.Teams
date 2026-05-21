using DeepSigma.Messaging.Teams.Authentication;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DeepSigma.Messaging.Teams.DependencyInjection;

public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers <see cref="ITeamsClient"/> as a singleton. If an <see cref="ILoggerFactory"/>
    /// is registered in the container, it will be wired into the client automatically.
    /// </summary>
    public static IServiceCollection AddTeamsClient(
        this IServiceCollection services,
        Action<TeamsClientBuilder> configure)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configure);

        services.AddSingleton<ITeamsClient>(sp =>
        {
            var builder = new TeamsClientBuilder();
            var loggerFactory = sp.GetService<ILoggerFactory>();
            if (loggerFactory is not null)
            {
                builder.WithLoggerFactory(loggerFactory);
            }
            configure(builder);
            return builder.Build();
        });

        return services;
    }

    public static IServiceCollection AddTeamsClient(
        this IServiceCollection services,
        ITeamsCredential credential,
        Action<TeamsClientOptions>? configureOptions = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(credential);

        return services.AddTeamsClient(builder =>
        {
            builder.WithCredential(credential);
            if (configureOptions is not null)
            {
                builder.WithOptions(configureOptions);
            }
        });
    }
}
