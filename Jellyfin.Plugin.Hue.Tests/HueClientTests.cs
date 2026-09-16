using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hue.Configuration;
using Jellyfin.Plugin.Hue.Hue;
using Jellyfin.Plugin.Hue.Tests.Mocks;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Hue.Tests;

public class HueClientTests
{
    [Fact]
    public async Task PairAsync_WhenButtonPressed_ReturnsSuccessAndKeys()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            var jsonResponse = "[{\"success\":{\"username\":\"test-app-key-abc\",\"clientkey\":\"test-client-key-xyz\"}}]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse),
            };
        });

        var client = new HueClient(new HttpClient(handler), NullLogger<HueClient>.Instance);
        var result = await client.PairAsync("192.168.1.100");

        Assert.True(result.Success);
        Assert.Equal("test-app-key-abc", result.Username);
        Assert.Equal("test-client-key-xyz", result.ClientKey);
        Assert.Null(result.ErrorMessage);
    }

    [Fact]
    public async Task PairAsync_WhenButtonNotPressed_ReturnsFalseWithError()
    {
        var handler = new MockHttpMessageHandler(req =>
        {
            var jsonResponse = "[{\"error\":{\"type\":101,\"address\":\"\",\"description\":\"link button not pressed\"}}]";
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(jsonResponse),
            };
        });

        var client = new HueClient(new HttpClient(handler), NullLogger<HueClient>.Instance);
        var result = await client.PairAsync("192.168.1.100");

        Assert.False(result.Success);
        Assert.Null(result.Username);
        Assert.Equal("link button not pressed", result.ErrorMessage);
    }

    [Fact]
    public async Task ApplyActionAsync_Play_DispatchesGroupAndLightRequests()
    {
        var handler = new MockHttpMessageHandler(req => new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent("{\"data\":[]}"),
        });

        var config = new PluginConfiguration
        {
            BridgeIp = "192.168.1.100",
            BridgeUsername = "valid-user",
        };
        PluginTestHelper.CreateMockPlugin(config);

        var client = new HueClient(new HttpClient(handler), NullLogger<HueClient>.Instance);
        var profile = new HueProfile
        {
            TargetGroupIds = new[] { "group-1" },
            TargetLightIds = new[] { "light-1" },
            PlayBrightness = 0,
            PlayTransitionDurationMs = 3000,
            RestoreStateOnStop = false,
        };

        await client.ApplyActionAsync(profile, "Play");

        // Should have sent PUT requests for grouped_light/group-1 and light/light-1
        Assert.Equal(2, handler.Requests.Count);
        Assert.Contains("grouped_light/group-1", handler.Requests[0].RequestUri?.ToString());
        Assert.Contains("light/light-1", handler.Requests[1].RequestUri?.ToString());
        Assert.Equal("valid-user", handler.Requests[0].Headers.GetValues("hue-application-key").AsContract());
    }

    [Fact]
    public async Task ApplyActionAsync_Resume_WhenResumeSameAsPlay_UsesPlaySettings()
    {
        string capturedBody = string.Empty;
        var handler = new MockHttpMessageHandler(req =>
        {
            if (req.Content != null)
            {
                capturedBody = req.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            }

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent("{\"data\":[]}"),
            };
        });

        var config = new PluginConfiguration
        {
            BridgeIp = "192.168.1.100",
            BridgeUsername = "valid-user",
        };
        PluginTestHelper.CreateMockPlugin(config);

        var client = new HueClient(new HttpClient(handler), NullLogger<HueClient>.Instance);
        var profile = new HueProfile
        {
            TargetGroupIds = new[] { "group-1" },
            PlayBrightness = 15,
            PlayTransitionDurationMs = 2500,
            ResumeSameAsPlay = true,
            ResumeBrightness = 80,
        };

        await client.ApplyActionAsync(profile, "Resume");

        Assert.Single(handler.Requests);
        Assert.Contains("grouped_light/group-1", handler.Requests[0].RequestUri?.ToString());
        Assert.Contains("\"brightness\":15", capturedBody);
        Assert.Contains("\"duration\":2500", capturedBody);
    }
}

internal static class HeaderTestExtensions
{
    public static string? AsContract(this System.Collections.Generic.IEnumerable<string> values)
    {
        using var enumerator = values.GetEnumerator();
        return enumerator.MoveNext() ? enumerator.Current : null;
    }
}
