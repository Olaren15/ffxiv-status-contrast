using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

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
        "_FocusTargetInfo",
        "_PartyList"
    ];

    private BackgroundNodePool? _backgroundNodePool;
    private readonly ConfigurationRepository _configurationRepository;

    private readonly IFramework _framework;
    private readonly NodeIdProvider _idProvider;
    private readonly IPluginLog _log;

    private AddonNamePlate* _namePlate;

    public BackgroundsManager(IFramework framework, IGameGui gameGui, IAddonLifecycle addonLifecycle, IPluginLog log,
        ConfigurationRepository configurationRepository)
    {
        _framework = framework;
        _addonLifecycle = addonLifecycle;
        _log = log;
        _configurationRepository = configurationRepository;

        _idProvider = new NodeIdProvider(69000); // Nice

        // Try to get addons if plugin is loaded when the ui is loaded
        _namePlate = (AddonNamePlate*)gameGui.GetAddonByName(NamePlateAddonName);

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
            _addonLifecycle.RegisterListener(AddonEvent.PreDraw, addonName,
                (_, args) => AssociateBackgroundsWithStatuses((AtkUnitBase*)args.Addon));
        }

        _configurationRepository.ConfigurationUpdated += UpdateConfiguration;
    }

    public void Dispose()
    {
        _configurationRepository.ConfigurationUpdated -= UpdateConfiguration;

        _framework.RunOnFrameworkThread(DestroyBackgrounds).Wait();
        foreach (string addonName in _addonNamesToFollow)
        {
            _addonLifecycle.UnregisterListener(AddonEvent.PreDraw, addonName);
        }

        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, NamePlateAddonName);
        _addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, NamePlateAddonName);
    }

    private void AssociateBackgroundsWithStatuses(AtkUnitBase* addon)
    {
        if (addon->NameString == "_PartyList")
        {
            PartyListStatusNodeFinder.ForEachNode((AddonPartyList*)addon,
                statusNode => _backgroundNodePool?.AssociateNextNode(statusNode));
        }
        else
        {
            GenericStatusNodeFinder.ForEachNode(addon->RootNode,
                statusNode => _backgroundNodePool?.AssociateNextNode(statusNode));
        }
    }

    public void Update()
    {
        if (_namePlate is null)
        {
            return;
        }

        if (_backgroundNodePool is null)
        {
            _log.Debug("Creating backgrounds pool");
            _backgroundNodePool = new BackgroundNodePool(
                _log,
                _namePlate->RootNode,
                _configurationRepository.GetConfiguration(),
                _idProvider
            );
        }

        _backgroundNodePool.PrepareForNextFrame();
        _namePlate->UldManager.UpdateDrawNodeList();
    }

    private void DestroyBackgrounds()
    {
        if (_backgroundNodePool == null)
        {
            _log.Debug("Requested to destroy backgrounds, but was already destroyed");
            return;
        }

        _log.Debug("Destroying backgrounds pool");
        _backgroundNodePool.Dispose();
        _backgroundNodePool = null;
        _namePlate->UldManager.UpdateDrawNodeList();
    }

    private void UpdateConfiguration(object? sender, Configuration configuration)
    {
        _backgroundNodePool?.SetConfiguration(configuration);
    }
}
