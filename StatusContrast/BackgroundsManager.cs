using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using Serilog;

namespace StatusContrast;

public sealed unsafe class BackgroundsManager : IDisposable
{
    private const string NamePlateAddonName = "NamePlate";

    private readonly IFramework _framework;
    private readonly IGameGui _gameGui;
    private readonly IAddonLifecycle _addonLifecycle;
    private readonly IPluginLog _log;
    private readonly ConfigurationRepository _configurationRepository;
    private readonly NodeIdProvider _idProvider;

    private AddonNamePlate* _namePlate;
    private readonly List<IntPtr> _followTargetsBuffer = [];
    private readonly Dictionary<string, BackgroundNodeGroup> _backgrounds = new();

    public BackgroundsManager(IFramework framework, IGameGui gameGui, IAddonLifecycle addonLifecycle, IPluginLog log,
        ConfigurationRepository configurationRepository)
    {
        _framework = framework;
        _gameGui = gameGui;
        _addonLifecycle = addonLifecycle;
        _log = log;
        _configurationRepository = configurationRepository;

        _idProvider = new NodeIdProvider(69000); // Nice

        _configurationRepository.ConfigurationUpdated += UpdateConfiguration;

        List<string> addonNamesToFollow =
        [
            "_StatusCustom0",
            "_StatusCustom1",
            "_StatusCustom2",
            "_StatusCustom3",
            "_TargetInfoBuffDebuff",
            "_TargetInfo",
            "_FocusTargetInfo",
            "_PartyList"
        ];

        // Try to get addons if plugin is loaded when the ui is loaded
        _namePlate = (AddonNamePlate*)_gameGui.GetAddonByName(NamePlateAddonName);

        // Otherwise wait until ui is available
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, NamePlateAddonName,
            (_, args) => _namePlate = (AddonNamePlate*)args.Addon);
        _addonLifecycle.RegisterListener(AddonEvent.PreFinalize, NamePlateAddonName, (_, _) =>
        {
            _framework.RunOnFrameworkThread(DestroyBackgrounds).Wait();
            _namePlate = null;
        });

        foreach (string addonName in addonNamesToFollow)
        {
            // Try to get addons if plugin is loaded when the ui is loaded
            _followTargetsBuffer.AddIfNotNull(_gameGui.GetAddonByName(addonName));

            // Otherwise wait until ui is available
            _addonLifecycle.RegisterListener(AddonEvent.PostSetup, addonName,
                (_, args) => _followTargetsBuffer.AddIfNotNull(args.Addon));
            _addonLifecycle.RegisterListener(AddonEvent.PreFinalize, addonName,
                (_, _) => _framework.RunOnFrameworkThread(() => RemoveBackground(addonName)).Wait());
        }
    }

    public void Update()
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

    public void Dispose()
    {
        _framework.RunOnFrameworkThread(DestroyBackgrounds).Wait();
        _configurationRepository.ConfigurationUpdated -= UpdateConfiguration;
    }

    private void RemoveBackground(string name)
    {
        if (!_backgrounds.TryGetValue(name, out BackgroundNodeGroup? background))
        {
            return;
        }

        Log.Debug("Removing background for {AddonName}", name);
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

            _log.Debug("Creating background for {addonName}", addonName);
            _backgrounds.Add(addonName, new BackgroundNodeGroup(followTargetAtk->RootNode, _namePlate->RootNode, _configurationRepository.GetConfiguration(), _idProvider));
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

        _log.Debug("Destroying all backgrounds");
        foreach (BackgroundNodeGroup background in _backgrounds.Values)
        {
            background.Dispose();
            removedNode = true;
        }

        _backgrounds.Clear();

        if (removedNode)
        {
            _namePlate->UldManager.UpdateDrawNodeList();
        }
    }

    private void UpdateConfiguration(object? sender, Configuration configuration)
    {
        foreach (BackgroundNodeGroup backgound in _backgrounds.Values)
        {
            backgound.SetConfiguration(configuration);
        }
    }
}
