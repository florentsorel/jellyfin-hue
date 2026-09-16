using System;
using System.IO;
using System.Reflection;
using Jellyfin.Plugin.Hue.Configuration;
using MediaBrowser.Common.Configuration;
using MediaBrowser.Model.Serialization;
using Moq;

namespace Jellyfin.Plugin.Hue.Tests.Mocks;

public static class PluginTestHelper
{
    public static Plugin CreateMockPlugin(PluginConfiguration? configuration = null)
    {
        var appPathsMock = new Mock<IApplicationPaths>();
        var xmlSerializerMock = new Mock<IXmlSerializer>();

        var tempPath = Path.GetTempPath();
        appPathsMock.SetReturnsDefault<string>(tempPath);
        appPathsMock.Setup(a => a.PluginConfigurationsPath).Returns(tempPath);
        appPathsMock.Setup(a => a.PluginsPath).Returns(tempPath);
        appPathsMock.Setup(a => a.DataPath).Returns(tempPath);
        appPathsMock.Setup(a => a.ConfigurationDirectoryPath).Returns(tempPath);

        var plugin = new Plugin(appPathsMock.Object, xmlSerializerMock.Object);

        if (configuration != null)
        {
            var configProp = typeof(Plugin).GetProperty("Configuration");
            if (configProp != null && configProp.CanWrite)
            {
                configProp.SetValue(plugin, configuration);
            }
            else
            {
                var field = typeof(Plugin).GetField("<Configuration>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic)
                            ?? typeof(Plugin).BaseType?.GetField("<Configuration>k__BackingField", BindingFlags.Instance | BindingFlags.NonPublic);
                field?.SetValue(plugin, configuration);
            }
        }

        return plugin;
    }
}
