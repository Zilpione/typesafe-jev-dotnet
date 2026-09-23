using TypeSafe.Jev;

namespace Microsoft.Extensions.DependencyInjection;

public static class JevServiceCollectionExtensions
{
    public static IHttpClientBuilder AddJevService(this IServiceCollection services, string apiKey)
    {
        ArgumentNullException.ThrowIfNull(services);
        if (string.IsNullOrWhiteSpace(apiKey)) throw new ArgumentException("TypeSafe API key is required.", nameof(apiKey));
        return services.AddHttpClient(nameof(IJevService))
            .AddTypedClient<IJevService>(http => new JevService(apiKey, http));
    }
}
