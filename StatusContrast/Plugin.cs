using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

// ReSharper disable once UnusedType.Global
public sealed unsafe class Plugin : IDalamudPlugin
{
    private const string NamePlateAddonName = "NamePlate";

    private AddonNamePlate* _namePlate;
    private readonly List<IntPtr> _followTargetsBuffer = [];
    private readonly Dictionary<string, BackgroundNodeGroup> _backgrounds = new();

    public Plugin()
    {
        List<string> addonNamesToFollow =
        [
            "_StatusCustom0",
            "_StatusCustom1",
            "_StatusCustom2",
            "_StatusCustom3",
            "_TargetInfoBuffDebuff"
        ];

        // Try to get addons if plugin is loaded when the ui is loaded
        _namePlate = (AddonNamePlate*)GameGui.GetAddonByName(NamePlateAddonName);

        // Otherwise wait until ui is available
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, NamePlateAddonName,
            (_, args) => _namePlate = (AddonNamePlate*)args.Addon);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, NamePlateAddonName, (_, _) =>
        {
            Framework.RunOnFrameworkThread(DestroyBackgrounds).Wait();
            _namePlate = null;
        });

        foreach (string addonName in addonNamesToFollow)
        {
            // Try to get addons if plugin is loaded when the ui is loaded
            _followTargetsBuffer.AddIfNotNull(GameGui.GetAddonByName(addonName));

            // Otherwise wait until ui is available
            AddonLifecycle.RegisterListener(AddonEvent.PostSetup, addonName,
                (_, args) => _followTargetsBuffer.AddIfNotNull(args.Addon));
            AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, addonName,
                (_, _) => Framework.RunOnFrameworkThread(() => RemoveBackground(addonName)).Wait());
        }

        Framework.Update += Update;
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
    [PluginService] public static IPluginLog Log { get; set; } = null!;

    public void Dispose()
    {
        Framework.RunOnFrameworkThread(DestroyBackgrounds).Wait();
        Framework.Update -= Update;
    }

    private void Update(IFramework framework)
    {
        if (_namePlate is null)
        {
            return;
        }

        CreateBackgroundsIfNeeded();

        foreach (BackgroundNodeGroup background in _backgrounds.Values)
        {
            background.Update();
        }
    }

    private void RemoveBackground(string name)
    {
        bool valuePresent = _backgrounds.TryGetValue(name, out BackgroundNodeGroup background);
        if (!valuePresent)
        {
            return;
        }

        Log.Debug("Removing background for {addonName}", name);
        background.Dispose();
        _backgrounds.Remove(name);
    }

    private void CreateBackgroundsIfNeeded()
    {
        bool addedNode = false;

        foreach (IntPtr followTarget in _followTargetsBuffer)
        {
            AtkUnitBase* followTargetAtk = (AtkUnitBase*)followTarget;
            string addonName = followTargetAtk->NameString;

            if (_backgrounds.ContainsKey(addonName))
            {
                continue;
            }

            Log.Debug("Creating background for {addonName}", addonName);
            _backgrounds.Add(addonName, new BackgroundNodeGroup(followTargetAtk->RootNode, _namePlate->RootNode));
            addedNode = true;
        }

        _followTargetsBuffer.Clear();

        if (addedNode)
        {
            _namePlate->UldManager.UpdateDrawNodeList();
        }
    }

    private void DestroyBackgrounds()
    {
        bool removedNode = false;

        Log.Debug("Destroying all backgrounds");
        foreach (BackgroundNodeGroup background in _backgrounds.Values)
        {
            background.Dispose();
            removedNode = true;
        }

        if (removedNode)
        {
            _namePlate->UldManager.UpdateDrawNodeList();
        }
    }
}
