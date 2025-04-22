using System;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace StatusContrast;

public class ConfigurationRepository
{
    private readonly IDalamudPluginInterface _pluginInterface;
    private readonly IPluginLog _log;
    private Configuration _cachedConfiguration;

    public event EventHandler<Configuration>? ConfigurationUpdated;

    public ConfigurationRepository(IDalamudPluginInterface pluginInterface, IPluginLog log)
    {
        _pluginInterface = pluginInterface;
        _log = log;

        _cachedConfiguration = (Configuration?)pluginInterface.GetPluginConfig() ?? new Configuration();
    }

    public void UpdateConfiguration(Configuration configuration)
    {
        if (_cachedConfiguration == configuration)
        {
            return;
        }

        _log.Debug("Updated configuration: {Configuration}", configuration);

        _cachedConfiguration = configuration;
        _pluginInterface.SavePluginConfig(configuration);
        ConfigurationUpdated?.Invoke(this, configuration);
    }

    public Configuration GetConfiguration()
    {
        return _cachedConfiguration;
    }
}
