using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Jellyfin.Plugin.Hue.Api;
using Jellyfin.Plugin.Hue.Api.Models;
using Jellyfin.Plugin.Hue.Configuration;
using Jellyfin.Plugin.Hue.Hue;
using Jellyfin.Plugin.Hue.Tests.Mocks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace Jellyfin.Plugin.Hue.Tests;

public class HueApiControllerTests
{
    private readonly PluginConfiguration _config;

    public HueApiControllerTests()
    {
        _config = new PluginConfiguration
        {
            BridgeIp = "192.168.1.50",
            BridgeUsername = "valid-appkey",
            Profiles = new[]
            {
                new HueProfile
                {
                    Id = "profile-salon",
                    Name = "Salon",
                    TargetLightIds = new[] { "light-1" },
                },
            },
        };

        PluginTestHelper.CreateMockPlugin(_config);
    }

    private static HueApiController CreateController(Func<HttpRequestMessage, HttpResponseMessage> handlerFunc)
    {
        var handler = new MockHttpMessageHandler(handlerFunc);
        var httpClient = new HttpClient(handler);
        var hueClient = new HueClient(httpClient, NullLogger<HueClient>.Instance);
        var discovery = new HueDiscovery(httpClient, NullLogger<HueDiscovery>.Instance);
        return new HueApiController(hueClient, discovery, NullLogger<HueApiController>.Instance);
    }

    [Fact]
    public async Task PairBridge_MissingIp_ReturnsBadRequest()
    {
        var controller = CreateController(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var result = await controller.PairBridge(new PairRequest { BridgeIp = string.Empty }, default);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task PairBridge_SuccessfulPairing_UpdatesConfigAndReturnsOk()
    {
        var controller = CreateController(req =>
        {
            var json = "[{\"success\":{\"username\":\"new-user-123\",\"clientkey\":\"new-client-key-456\"}}]";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        });

        var result = await controller.PairBridge(new PairRequest { BridgeIp = "192.168.1.100" }, default);
        Assert.IsType<OkObjectResult>(result.Result);

        Assert.Equal("192.168.1.100", _config.BridgeIp);
        Assert.Equal("new-user-123", _config.BridgeUsername);
        Assert.Equal("new-client-key-456", _config.BridgeClientKey);
    }

    [Fact]
    public async Task PairBridge_ButtonNotPressed_ReturnsOkWithSuccessFalse()
    {
        var controller = CreateController(req =>
        {
            var json = "[{\"error\":{\"type\":101,\"address\":\"\",\"description\":\"link button not pressed\"}}]";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        });

        var result = await controller.PairBridge(new PairRequest { BridgeIp = "192.168.1.100" }, default);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task GetResources_ReturnsMappedRoomsZonesAndLights()
    {
        var controller = CreateController(req =>
        {
            var path = req.RequestUri?.AbsolutePath ?? string.Empty;
            string json;
            if (path.EndsWith("/room", StringComparison.Ordinal))
            {
                json = "{\"data\":[{\"id\":\"room-1\",\"metadata\":{\"name\":\"Living Room\",\"archetype\":\"living_room\"},\"services\":[{\"rid\":\"grouped-1\",\"rtype\":\"grouped_light\"}]}]}";
            }
            else if (path.EndsWith("/zone", StringComparison.Ordinal))
            {
                json = "{\"data\":[{\"id\":\"zone-1\",\"metadata\":{\"name\":\"TV Zone\",\"archetype\":\"tv\"},\"services\":[{\"rid\":\"grouped-2\",\"rtype\":\"grouped_light\"}]}]}";
            }
            else if (path.EndsWith("/light", StringComparison.Ordinal))
            {
                json = "{\"data\":[{\"id\":\"light-1\",\"metadata\":{\"name\":\"Hue Lamp\",\"archetype\":\"sultan_bulb\"},\"color\":{\"xy\":{\"x\":0.4,\"y\":0.4}},\"color_temperature\":{\"mirek\":300},\"dimming\":{\"brightness\":80}}]}";
            }
            else
            {
                json = "{\"data\":[]}";
            }

            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(json) };
        });

        var result = await controller.GetResources(default);
        var okResult = Assert.IsType<OkObjectResult>(result.Result);
        var response = Assert.IsType<HueResourcesResponse>(okResult.Value);

        Assert.Single(response.Rooms);
        Assert.Equal("grouped-1", response.Rooms[0].Id);
        Assert.Equal("Living Room", response.Rooms[0].Name);

        Assert.Single(response.Zones);
        Assert.Equal("grouped-2", response.Zones[0].Id);

        Assert.Single(response.Lights);
        Assert.Equal("light-1", response.Lights[0].Id);
        Assert.True(response.Lights[0].SupportsColor);
        Assert.True(response.Lights[0].SupportsColorTemperature);
        Assert.True(response.Lights[0].SupportsDimming);
    }

    [Fact]
    public async Task TestTarget_MissingTargetId_ReturnsBadRequest()
    {
        var controller = CreateController(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var result = await controller.TestTarget(new TestTargetRequest { TargetId = string.Empty }, default);
        Assert.IsType<BadRequestObjectResult>(result.Result);
    }

    [Fact]
    public async Task TestTarget_ValidTarget_ReturnsSuccess()
    {
        var controller = CreateController(_ => new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent("{}") });
        var result = await controller.TestTarget(new TestTargetRequest { TargetId = "light-1", IsGroup = false }, default);
        Assert.IsType<OkObjectResult>(result.Result);
    }

    [Fact]
    public async Task TestProfile_NotFound_ReturnsNotFound()
    {
        var controller = CreateController(_ => new HttpResponseMessage(HttpStatusCode.OK));
        var result = await controller.TestProfile("non-existent", default);
        Assert.IsType<NotFoundObjectResult>(result.Result);
    }

    [Fact]
    public async Task TestProfile_ValidProfile_ReturnsSuccess()
    {
        var controller = CreateController(_ =>
        {
            var lightJson = "{\"data\":[{\"id\":\"light-1\",\"on\":{\"on\":true},\"dimming\":{\"brightness\":50}}]}";
            return new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(lightJson) };
        });

        var result = await controller.TestProfile("profile-salon", default);
        Assert.IsType<OkObjectResult>(result.Result);
    }
}
