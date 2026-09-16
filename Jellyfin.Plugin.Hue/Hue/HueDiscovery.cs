using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Net.Sockets;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hue.Hue.Models;
using Microsoft.Extensions.Logging;

namespace Jellyfin.Plugin.Hue.Hue;

/// <summary>
/// Service responsible for discovering Philips Hue bridges on the local network via mDNS and cloud discovery.
/// </summary>
public class HueDiscovery
{
    private const string CloudDiscoveryEndpoint = "https://discovery.meethue.com/";
    private readonly HttpClient _httpClient;
    private readonly ILogger<HueDiscovery> _logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="HueDiscovery"/> class.
    /// </summary>
    /// <param name="httpClient">The HTTP client instance.</param>
    /// <param name="logger">The logger instance.</param>
    public HueDiscovery(HttpClient httpClient, ILogger<HueDiscovery> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    /// <summary>
    /// Discovers Hue bridges on the network using local mDNS first, then falling back to cloud discovery.
    /// </summary>
    /// <param name="cancellationToken">Cancellation token.</param>
    /// <returns>A collection of discovered bridge representations.</returns>
    public async Task<Collection<HueDiscoveredBridge>> DiscoverBridgesAsync(CancellationToken cancellationToken = default)
    {
        var discovered = new Dictionary<string, HueDiscoveredBridge>(StringComparer.OrdinalIgnoreCase);

        // 1. Try local mDNS discovery on LAN (instant and unaffected by cloud rate-limits)
        try
        {
            var localBridges = await DiscoverViaMdnsAsync(cancellationToken).ConfigureAwait(false);
            foreach (var b in localBridges)
            {
                discovered[b.InternalIpAddress] = b;
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Local mDNS discovery returned an error");
        }

        // 2. If no local bridges found, fallback to cloud discovery
        if (discovered.Count == 0)
        {
            try
            {
                _logger.LogInformation("Attempting Hue Bridge discovery via {Endpoint}", CloudDiscoveryEndpoint);
                var bridges = await _httpClient.GetFromJsonAsync<List<HueDiscoveredBridge>>(CloudDiscoveryEndpoint, cancellationToken).ConfigureAwait(false);
                if (bridges != null)
                {
                    foreach (var b in bridges)
                    {
                        discovered[b.InternalIpAddress] = b;
                    }
                }
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogWarning(ex, "Cloud discovery failed or was rate-limited");
            }
        }

        return new Collection<HueDiscoveredBridge>(new List<HueDiscoveredBridge>(discovered.Values));
    }

    private async Task<List<HueDiscoveredBridge>> DiscoverViaMdnsAsync(CancellationToken cancellationToken)
    {
        var result = new List<HueDiscoveredBridge>();
        var foundIps = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        using var udpClient = new UdpClient();
        udpClient.Client.Bind(new IPEndPoint(IPAddress.Any, 0));
        udpClient.EnableBroadcast = true;

        var multicastEndpoint = new IPEndPoint(IPAddress.Parse("224.0.0.251"), 5353);

        // Standard DNS query asking for PTR record of _hue._tcp.local
        // Header: ID=0, Flags=0, QDCOUNT=1
        // Question: \x04_hue\x04_tcp\x05local\x00, TYPE=PTR(12), CLASS=IN(1)
        byte[] query =
        [
            0x00, 0x00, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
            0x04, 0x5f, 0x68, 0x75, 0x65, // _hue
            0x04, 0x5f, 0x74, 0x63, 0x70, // _tcp
            0x05, 0x6c, 0x6f, 0x63, 0x61, 0x6c, 0x00, // local
            0x00, 0x0c, // TYPE PTR
            0x00, 0x01 // CLASS IN
        ];

        await udpClient.SendAsync(query, multicastEndpoint, cancellationToken).ConfigureAwait(false);

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromMilliseconds(1500));

        while (!cts.IsCancellationRequested)
        {
            try
            {
                var receiveTask = udpClient.ReceiveAsync(cts.Token).AsTask();
                var completedTask = await Task.WhenAny(receiveTask, Task.Delay(1500, cts.Token)).ConfigureAwait(false);
                if (completedTask != receiveTask)
                {
                    break;
                }

                var recv = await receiveTask.ConfigureAwait(false);
                var ip = recv.RemoteEndPoint.Address.ToString();
                if (foundIps.Add(ip))
                {
                    // Query bridge id directly via /api/config
                    var bridgeId = await FetchBridgeIdAsync(ip, cancellationToken).ConfigureAwait(false);
                    result.Add(new HueDiscoveredBridge
                    {
                        Id = bridgeId ?? ip,
                        InternalIpAddress = ip,
                        Port = 443,
                    });
                }
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                _logger.LogDebug(ex, "Error reading mDNS packet");
                break;
            }
        }

        return result;
    }

    private async Task<string?> FetchBridgeIdAsync(string ip, CancellationToken cancellationToken)
    {
        try
        {
            using var req = new HttpRequestMessage(HttpMethod.Get, $"https://{ip}/api/config");
            using var res = await _httpClient.SendAsync(req, cancellationToken).ConfigureAwait(false);
            if (res.IsSuccessStatusCode)
            {
                var json = await res.Content.ReadAsStringAsync(cancellationToken).ConfigureAwait(false);
                using var doc = JsonDocument.Parse(json);
                if (doc.RootElement.TryGetProperty("bridgeid", out var idProp))
                {
                    return idProp.GetString();
                }
            }
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogDebug(ex, "Could not fetch bridgeid from {Ip}", ip);
        }

        return null;
    }
}
