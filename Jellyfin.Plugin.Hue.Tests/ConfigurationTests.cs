using System;
using System.IO;
using System.Text;
using System.Xml.Serialization;
using Jellyfin.Plugin.Hue.Configuration;
using Xunit;

namespace Jellyfin.Plugin.Hue.Tests;

public class ConfigurationTests
{
    [Fact]
    public void PluginConfiguration_SerializesAndDeserializesCorrectly()
    {
        var config = new PluginConfiguration
        {
            BridgeIp = "192.168.1.150",
            BridgeUsername = "test-app-key-12345",
            BridgeClientKey = "test-client-key-67890",
            Profiles = new[]
            {
                new HueProfile
                {
                    Id = "profile-living-room",
                    Name = "Living Room Setup",
                    Enabled = true,
                    TargetUserIds = new[] { Guid.NewGuid().ToString("N") },
                    TargetDeviceIds = new[] { "AppleTV_Salon" },
                    TargetGroupIds = new[] { "room-1" },
                    TargetLightIds = new[] { "light-1", "light-2" },
                    PlayBrightness = 5,
                    PlayTransitionDurationMs = 5000,
                    PauseBrightness = 30,
                    PauseColorHex = "#FFB366",
                    PauseTransitionDurationMs = 1500,
                    RestoreStateOnStop = true,
                },
            },
        };

        var serializer = new XmlSerializer(typeof(PluginConfiguration));
        string xml;
        using (var writer = new StringWriter())
        {
            serializer.Serialize(writer, config);
            xml = writer.ToString();
        }

        Assert.Contains("192.168.1.150", xml, StringComparison.Ordinal);
        Assert.Contains("Living Room Setup", xml, StringComparison.Ordinal);
        Assert.Contains("AppleTV_Salon", xml, StringComparison.Ordinal);

        PluginConfiguration deserialized;
        using (var reader = new StringReader(xml))
        {
            deserialized = (PluginConfiguration)serializer.Deserialize(reader)!;
        }

        Assert.Equal(config.BridgeIp, deserialized.BridgeIp);
        Assert.Equal(config.BridgeUsername, deserialized.BridgeUsername);
        Assert.Single(deserialized.Profiles);
        Assert.Equal("Living Room Setup", deserialized.Profiles[0].Name);
        Assert.Equal(5, deserialized.Profiles[0].PlayBrightness);
        Assert.True(deserialized.Profiles[0].RestoreStateOnStop);
    }
}
