using System;
using System.Collections.Generic;
using Dalamud.Game.Addon.Lifecycle;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

public sealed unsafe class BackgroundsManager : IDisposable
{
    private const string TargetAddonName = "CastBarEnemy";
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

    private AtkUnitBase* _targetAddon;

    public BackgroundsManager(IFramework framework, IGameGui gameGui, IAddonLifecycle addonLifecycle, IPluginLog log,
        ConfigurationRepository configurationRepository)
    {
        _framework = framework;
        _addonLifecycle = addonLifecycle;
        _log = log;
        _configurationRepository = configurationRepository;

        _idProvider = new NodeIdProvider(69000); // Nice

        // Try to get addons if plugin is loaded when the ui is loaded
        _targetAddon = (AtkUnitBase*)gameGui.GetAddonByName(TargetAddonName).Address;

        // Otherwise wait until ui is available
        _addonLifecycle.RegisterListener(AddonEvent.PostSetup, TargetAddonName,
            (_, args) => _targetAddon = (AtkUnitBase*)args.Addon.Address);
        _addonLifecycle.RegisterListener(AddonEvent.PreFinalize, TargetAddonName, (_, _) =>
        {
            _framework.RunOnFrameworkThread(DestroyBackgrounds).Wait();
            _targetAddon = null;
        });

        foreach (string addonName in _addonNamesToFollow)
        {
            _addonLifecycle.RegisterListener(AddonEvent.PreDraw, addonName,
                (_, args) => AssociateBackgroundsWithStatuses((AtkUnitBase*)args.Addon.Address));
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

        _addonLifecycle.UnregisterListener(AddonEvent.PostSetup, TargetAddonName);
        _addonLifecycle.UnregisterListener(AddonEvent.PreFinalize, TargetAddonName);
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
        if (_targetAddon is null)
        {
            return;
        }

        if (_backgroundNodePool is null)
        {
            _log.Debug("Creating backgrounds pool");
            _backgroundNodePool = new BackgroundNodePool(
                _log,
                _targetAddon->RootNode,
                _configurationRepository.GetConfiguration(),
                _idProvider
            );
        }

        _backgroundNodePool.PrepareForNextFrame();
        _targetAddon->UldManager.UpdateDrawNodeList();
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
        _targetAddon->UldManager.UpdateDrawNodeList();
    }

    private void UpdateConfiguration(object? sender, Configuration configuration)
    {
        _backgroundNodePool?.SetConfiguration(configuration);
    }
}
