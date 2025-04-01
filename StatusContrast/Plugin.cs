using Dalamud.Game.Addon.Lifecycle;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

// ReSharper disable once UnusedType.Global
public sealed unsafe class Plugin : IDalamudPlugin
{
    private AddonNamePlate* _namePlate;

    private AtkUnitBase* _buffs;
    private BackgroundNode* _buffsBackground = null;

    private AtkUnitBase* _debuffs;
    private BackgroundNode* _debuffsBackground = null;

    private AtkUnitBase* _jobStatuses;
    private BackgroundNode* _jobStatusBackground = null;

    private AtkUnitBase* _otherStatuses;
    private BackgroundNode* _otherStatusesBackground = null;

    public Plugin()
    {
        // Try to get addons if plugin is loaded when the ui is loaded
        _namePlate = (AddonNamePlate*)GameGui.GetAddonByName("NamePlate");
        _buffs = (AtkUnitBase*)GameGui.GetAddonByName("_StatusCustom0");
        _debuffs = (AtkUnitBase*)GameGui.GetAddonByName("_StatusCustom1");
        _otherStatuses = (AtkUnitBase*)GameGui.GetAddonByName("_StatusCustom2");
        _jobStatuses = (AtkUnitBase*)GameGui.GetAddonByName("_StatusCustom3");

        // Otherwise wait until ui is available
        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "NamePlate",
            (_, args) => _namePlate = (AddonNamePlate*)args.Addon);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "NamePlate", (_, _) =>
        {
            Framework.RunOnFrameworkThread(DestroyBackgrounds).Wait();
            _namePlate = null;
        });

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "_StatusCustom0",
            (_, args) => _buffs = (AtkUnitBase*)args.Addon);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_StatusCustom0", (_, _) => _buffs = null);

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "_StatusCustom1",
            (_, args) => _debuffs = (AtkUnitBase*)args.Addon);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_StatusCustom1", (_, _) => _debuffs = null);

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "_StatusCustom2",
            (_, args) => _otherStatuses = (AtkUnitBase*)args.Addon);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_StatusCustom2", (_, _) => _otherStatuses = null);

        AddonLifecycle.RegisterListener(AddonEvent.PostSetup, "_StatusCustom3",
            (_, args) => _jobStatuses = (AtkUnitBase*)args.Addon);
        AddonLifecycle.RegisterListener(AddonEvent.PreFinalize, "_StatusCustom3", (_, _) => _jobStatuses = null);

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
    [PluginService] private static IPluginLog Log { get; set; } = null!;

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

        _buffsBackground->Update(_buffs->RootNode);
        _debuffsBackground->Update(_debuffs->RootNode);
        _otherStatusesBackground->Update(_otherStatuses->RootNode);
        _jobStatusBackground->Update(_jobStatuses->RootNode);
    }

    private void CreateBackgroundsIfNeeded()
    {
        bool addedNode = false;

        if (_buffs is not null && _buffsBackground is null)
        {
            Log.Debug("creating buffs background");
            _buffsBackground = CreateBackground(_buffs->RootNode, _namePlate->RootNode);
            addedNode = true;
        }

        if (_debuffs is not null && _debuffsBackground is null)
        {
            Log.Debug("creating debuff background");
            _debuffsBackground = CreateBackground(_debuffs->RootNode, _namePlate->RootNode);
            addedNode = true;
        }

        if (_otherStatuses is not null && _otherStatusesBackground is null)
        {
            Log.Debug("creating other statuses background");
            _otherStatusesBackground = CreateBackground(_otherStatuses->RootNode, _namePlate->RootNode);
            addedNode = true;
        }

        if (_jobStatuses is not null && _jobStatusBackground is null)
        {
            Log.Debug("creating job statuses background");
            _jobStatusBackground = CreateBackground(_jobStatuses->RootNode, _namePlate->RootNode);
            addedNode = true;
        }

        if (addedNode)
        {
            _namePlate->UldManager.UpdateDrawNodeList();
        }
    }

    private void DestroyBackgrounds()
    {
        bool removedNode = false;

        if (_buffsBackground is not null)
        {
            Log.Debug("destroying buffs background");
            Framework.RunOnFrameworkThread(() =>
            {
                DestroyBackground(_buffsBackground);
                _buffsBackground = null;
            });

            removedNode = true;
        }

        if (_debuffsBackground is not null)
        {
            Log.Debug("destroying debuffs background");
            DestroyBackground(_debuffsBackground);
            _debuffsBackground = null;

            removedNode = true;
        }

        if (_otherStatusesBackground is not null)
        {
            Log.Debug("destroying other statuses background");
            DestroyBackground(_otherStatusesBackground);
            _otherStatusesBackground = null;

            removedNode = true;
        }

        if (_jobStatusBackground is not null)
        {
            Log.Debug("destroying job statuses background");
            DestroyBackground(_jobStatusBackground);
            _jobStatusBackground = null;

            removedNode = true;
        }

        if (removedNode)
        {
            _namePlate->UldManager.UpdateDrawNodeList();
        }
    }

    private static BackgroundNode* CreateBackground(AtkResNode* statusNode, AtkResNode* attachTarget)
    {
        BackgroundNode* backgroundNode = (BackgroundNode*)IMemorySpace.GetUISpace()->Malloc<BackgroundNode>();
        if (backgroundNode is null)
        {
            return null;
        }

        IMemorySpace.Memset(backgroundNode, 0, (ulong)sizeof(BackgroundNode));
        backgroundNode->Init(statusNode);

        NodeLinker.AttachToNode((AtkResNode*)backgroundNode->ImageNode, attachTarget);

        return backgroundNode;
    }

    private static void DestroyBackground(BackgroundNode* backgroundNode)
    {
        NodeLinker.DetachNode((AtkResNode*)backgroundNode->ImageNode);
        backgroundNode->Destroy();
        IMemorySpace.Free(backgroundNode);
    }
}
