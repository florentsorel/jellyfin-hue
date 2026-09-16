using System.Net.Http;
using Jellyfin.Plugin.Hue.Hue;
using Jellyfin.Plugin.Hue.Services;
using MediaBrowser.Controller;
using MediaBrowser.Controller.Plugins;
using Microsoft.Extensions.DependencyInjection;

namespace Jellyfin.Plugin.Hue;

/// <summary>
/// Registers Philips Hue plugin services into the Jellyfin dependency injection container.
/// </summary>
public class PluginServiceRegistrator : IPluginServiceRegistrator
{
    /// <inheritdoc />
    public void RegisterServices(IServiceCollection serviceCollection, IServerApplicationHost applicationHost)
    {
        // Register HTTP client for local Hue communication (ignoring untrusted self-signed local bridge certificates)
        serviceCollection.AddHttpClient<HueClient>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });

        // Register HTTP client for Hue cloud and local discovery
        serviceCollection.AddHttpClient<HueDiscovery>()
            .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = HttpClientHandler.DangerousAcceptAnyServerCertificateValidator,
            });

        // Register Orchestrator & Hosted Background Service
        serviceCollection.AddSingleton<HueOrchestrator>();
        serviceCollection.AddHostedService<PlaybackListener>();
    }
}
