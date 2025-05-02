using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;

namespace StatusContrast;

// ReSharper disable once UnusedType.Global
public sealed class Plugin : IDalamudPlugin
{
    private readonly WindowSystem _windowSystem;
    private readonly ConfigurationRepository _configurationRepository;
    private readonly SettingsWindow _settingsWindow;
    private readonly BackgroundsManager _backgroundsManager;

    public Plugin()
    {
        _configurationRepository = new ConfigurationRepository(PluginInterface, Log);
        _backgroundsManager = new BackgroundsManager(Framework, GameGui, AddonLifecycle, Log, _configurationRepository);

        _windowSystem = new WindowSystem("StatusContrast");
        _settingsWindow = new SettingsWindow(_configurationRepository);
        _windowSystem.AddWindow(_settingsWindow);

        PluginInterface.UiBuilder.Draw += _windowSystem.Draw;
        PluginInterface.UiBuilder.OpenConfigUi += _settingsWindow.Show;

        Framework.Update += Update;
    }


    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IAddonLifecycle AddonLifecycle { get; set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IFramework Framework { get; set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IGameGui GameGui { get; set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IPluginLog Log { get; set; } = null!;

    public void Dispose()
    {
        Framework.Update -= Update;
        PluginInterface.UiBuilder.OpenConfigUi -= _settingsWindow.Show;
        PluginInterface.UiBuilder.Draw -= _windowSystem.Draw;

        _backgroundsManager.Dispose();
        _windowSystem.RemoveAllWindows();
    }

    private void Update(IFramework framework)
    {
        _backgroundsManager.Update();
    }
}
