using Dalamud.Interface.Windowing;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

// ReSharper disable once UnusedType.Global
public sealed unsafe class Plugin : IDalamudPlugin
{
    private readonly BackgroundNodes _backgroundNodes;
    private readonly WindowSystem _windowSystem = new("StatusContrast");

    public Plugin()
    {
        PluginInterface.UiBuilder.Draw += DrawUi;
        AtkUnitBase*[] statusesAtk =
        [
            (AtkUnitBase*)GameGui.GetAddonByName("_StatusCustom0"),
            (AtkUnitBase*)GameGui.GetAddonByName("_StatusCustom1"),
            (AtkUnitBase*)GameGui.GetAddonByName("_StatusCustom2"),
            (AtkUnitBase*)GameGui.GetAddonByName("_StatusCustom3")
        ];

        _backgroundNodes = new BackgroundNodes(statusesAtk, AddonLifecycle, Framework, GameGui, PluginInterface);
        _backgroundNodes.Enable();
    }

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IDalamudPluginInterface PluginInterface { get; set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IAddonLifecycle AddonLifecycle { get; set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IFramework Framework { get; set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IGameGui GameGui { get; set; } = null!;

    // ReSharper disable once AutoPropertyCanBeMadeGetOnly.Local
    [PluginService] private static IPluginLog Log { get; set; } = null!;


    public void Dispose()
    {
        _windowSystem.RemoveAllWindows();
        _backgroundNodes.Dispose();
    }

    private void DrawUi()
    {
        _windowSystem.Draw();
    }
}
