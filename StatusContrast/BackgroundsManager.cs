using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace StatusContrast;

public sealed unsafe class BackgroundsManager : IDisposable
{
    private const string NamePlateAddonName = "NamePlate";
    private readonly IAddonLifecycle _addonLifecycle;

    private readonly List<string> _addonNamesToFollow =
    [
        "_StatusCustom0",
        "_StatusCustom1",
        "_StatusCustom2",
        "_StatusCustom3",
        "_TargetInfoBuffDebuff",
        "_TargetInfo",
        "_FocusTargetInfo"
    ];

    private readonly Dictionary<string, BackgroundNodeGroup> _backgrounds = new();
    private readonly ConfigurationRepository _configurationRepository;
    private readonly List<Pointer<AtkUnitBase>> _followTargetsBuffer = [];

    private readonly IFramework _framework;
    private readonly IGameGui _gameGui;
    private readonly NodeIdProvider _idProvider;
    private readonly IPluginLog _log;

    private AddonNamePlate* _namePlate;

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

        foreach (string addonName in _addonNamesToFollow)
        {
            // Try to get addons if plugin is loaded when the ui is loaded
            _followTargetsBuffer.AddIfNotNull((AtkUnitBase*)_gameGui.GetAddonByName(addonName));

            // Otherwise wait until ui is available
            _addonLifecycle.RegisterListener(AddonEvent.PostSetup, addonName,
                (_, args) => _followTargetsBuffer.AddIfNotNull((AtkUnitBase*)args.Addon));
            _addonLifecycle.RegisterListener(AddonEvent.PreFinalize, addonName,
                (_, _) => _framework.RunOnFrameworkThread(() => RemoveBackground(addonName)).Wait());
        }
    }

    public void Dispose()
    {
        _framework.RunOnFrameworkThread(DestroyBackgrounds).Wait();
        foreach (string addonName in _addonNamesToFollow)
        {
            _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, addonName);
            _addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, addonName);
        }

        _addonLifecycle.UnregisterListener(AddonEvent.PreRequestedUpdate, "_PartyList");

        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, NamePlateAddonName);
        _addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, NamePlateAddonName);

        _configurationRepository.ConfigurationUpdated -= UpdateConfiguration;
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

    private void CreateBackgroundsIfNeeded()
    {
        bool addedNode = false;

        foreach (AtkUnitBase* followTarget in _followTargetsBuffer)
        {
            string addonName = followTarget->NameString;

            if (_backgrounds.ContainsKey(addonName))
            {
                continue;
            }

            _log.Debug("Creating background for {addonName}", addonName);

            _backgrounds.Add(addonName,
                new BackgroundNodeGroup(
                    new GenericStatusNodeFinder(followTarget->RootNode),
                    _namePlate->RootNode,
                    _configurationRepository.GetConfiguration(),
                    _idProvider
                )
            );

            addedNode = true;
        }

        _followTargetsBuffer.Clear();

        if (addedNode)
        {
            _namePlate->UldManager.UpdateDrawNodeList();
        }
    }

    private void RemoveBackground(string name)
    {
        if (!_backgrounds.TryGetValue(name, out BackgroundNodeGroup? background))
        {
            return;
        }

        _log.Debug("Removing background for {AddonName}", name);
        background.Dispose();
        _backgrounds.Remove(name);

        _namePlate->UldManager.UpdateDrawNodeList();
    }

    private void DestroyBackgrounds()
    {
        _log.Debug("Destroying all backgrounds");
        foreach (string name in _backgrounds.Keys)
        {
            RemoveBackground(name);
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
