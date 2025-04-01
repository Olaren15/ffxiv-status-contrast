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
    private BackgroundNodeGroup? _buffsBackgrounds;

    private AtkUnitBase* _debuffs;
    private BackgroundNodeGroup? _debuffsBackgrounds;

    private AtkUnitBase* _jobStatuses;
    private BackgroundNodeGroup? _jobStatusBackground;

    private AtkUnitBase* _otherStatuses;
    private BackgroundNodeGroup? _otherStatusesBackgrounds;

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

        _buffsBackgrounds?.Update();
        _debuffsBackgrounds?.Update();
        _otherStatusesBackgrounds?.Update();
        _jobStatusBackground?.Update();
    }

    private void CreateBackgroundsIfNeeded()
    {
        bool addedNode = false;

        if (_buffs is not null && _buffsBackgrounds is null)
        {
            Log.Debug("creating buffs background");
            _buffsBackgrounds = CreateBackground(_buffs->RootNode, _namePlate->RootNode);
            addedNode = true;
        }

        if (_debuffs is not null && _debuffsBackgrounds is null)
        {
            Log.Debug("creating debuff background");
            _debuffsBackgrounds = CreateBackground(_debuffs->RootNode, _namePlate->RootNode);
            addedNode = true;
        }

        if (_otherStatuses is not null && _otherStatusesBackgrounds is null)
        {
            Log.Debug("creating other statuses background");
            _otherStatusesBackgrounds = CreateBackground(_otherStatuses->RootNode, _namePlate->RootNode);
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

        if (_buffsBackgrounds is not null)
        {
            Log.Debug("destroying buffs background");
            Framework.RunOnFrameworkThread(() =>
            {
                DestroyBackground(_buffsBackgrounds);
                _buffsBackgrounds = null;
            });

            removedNode = true;
        }

        if (_debuffsBackgrounds is not null)
        {
            Log.Debug("destroying debuffs background");
            DestroyBackground(_debuffsBackgrounds);
            _debuffsBackgrounds = null;

            removedNode = true;
        }

        if (_otherStatusesBackgrounds is not null)
        {
            Log.Debug("destroying other statuses background");
            DestroyBackground(_otherStatusesBackgrounds);
            _otherStatusesBackgrounds = null;

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

    private static BackgroundNodeGroup CreateBackground(AtkResNode* statusNode, AtkResNode* attachTarget)
    {
        BackgroundNodeGroup group = new(statusNode);
        NodeLinker.AttachToNode(group.RootNode, attachTarget);

        return group;
    }

    private static void DestroyBackground(BackgroundNodeGroup? group)
    {
        if (group is null)
        {
            return;
        }

        NodeLinker.DetachNode(group.Value.RootNode);
        group.Value.Dispose();
    }
}
