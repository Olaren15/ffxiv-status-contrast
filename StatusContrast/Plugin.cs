using Dalamud.Game.Addon.Lifecycle;
using Dalamud.IoC;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.Graphics;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

// ReSharper disable once UnusedType.Global
public sealed unsafe class Plugin : IDalamudPlugin
{
    private AtkUnitBase* _buffs;
    private AtkImageNode* _buffsBackground = null;

    private AtkUnitBase* _debuffs;
    private AtkImageNode* _debuffsBackground = null;

    private AtkImageNode* _jobStatusBackground = null;
    private AtkUnitBase* _jobStatuses;

    private AddonNamePlate* _namePlate;

    private AtkUnitBase* _otherStatuses;
    private AtkImageNode* _otherStatusesBackground = null;

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
            DestroyBackgrounds();
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
        DestroyBackgrounds();
        Framework.Update -= Update;
    }

    private void Update(IFramework framework)
    {
        if (_namePlate is null)
        {
            Log.Info("nameplate not ready");
            return;
        }

        if (_buffs is not null && _buffsBackground is null)
        {
            Log.Info("creating buffs background");
            _buffsBackground = CreateBackground(_buffs);
            NodeLinker.AttachToNode((AtkResNode*)_buffsBackground, _namePlate->RootNode);
            _namePlate->UldManager.UpdateDrawNodeList();
        }

        if (_debuffs is not null && _debuffsBackground is null)
        {
            Log.Info("creating debuff background");
            _debuffsBackground = CreateBackground(_debuffs);
            NodeLinker.AttachToNode((AtkResNode*)_debuffsBackground, _namePlate->RootNode);
            _namePlate->UldManager.UpdateDrawNodeList();
        }

        if (_otherStatuses is not null && _otherStatusesBackground is null)
        {
            Log.Info("creating other statuses background");
            _otherStatusesBackground = CreateBackground(_otherStatuses);
            NodeLinker.AttachToNode((AtkResNode*)_otherStatusesBackground, _namePlate->RootNode);
            _namePlate->UldManager.UpdateDrawNodeList();
        }

        if (_jobStatuses is not null && _jobStatusBackground is null)
        {
            Log.Info("creating job statuses background");
            _jobStatusBackground = CreateBackground(_jobStatuses);
            NodeLinker.AttachToNode((AtkResNode*)_jobStatusBackground, _namePlate->RootNode);
            _namePlate->UldManager.UpdateDrawNodeList();
        }
    }

    private void DestroyBackgrounds()
    {
        if (_buffsBackground is not null)
        {
            Log.Info("destroying buffs background");
            Framework.RunOnFrameworkThread(() =>
            {
                NodeLinker.DetachNode((AtkResNode*)_buffsBackground);
                DestroyBackground(_buffsBackground);
                _buffsBackground = null;

                _namePlate->UldManager.UpdateDrawNodeList();
            });
        }

        if (_debuffsBackground is not null)
        {
            Log.Info("destroying debuffs background");
            Framework.RunOnFrameworkThread(() =>
            {
                NodeLinker.DetachNode((AtkResNode*)_debuffsBackground);
                DestroyBackground(_debuffsBackground);
                _debuffsBackground = null;

                _namePlate->UldManager.UpdateDrawNodeList();
            });
        }

        if (_otherStatusesBackground is not null)
        {
            Log.Info("destroying other statuses background");
            Framework.RunOnFrameworkThread(() =>
            {
                NodeLinker.DetachNode((AtkResNode*)_otherStatusesBackground);
                DestroyBackground(_otherStatusesBackground);
                _otherStatusesBackground = null;

                _namePlate->UldManager.UpdateDrawNodeList();
            });
        }

        if (_jobStatusBackground is not null)
        {
            Log.Info("destroying job statuses background");
            Framework.RunOnFrameworkThread(() =>
            {
                NodeLinker.DetachNode((AtkResNode*)_jobStatusBackground);
                DestroyBackground(_jobStatusBackground);
                _jobStatusBackground = null;

                _namePlate->UldManager.UpdateDrawNodeList();
            });
        }
    }

    private static AtkImageNode* CreateBackground(AtkUnitBase* statusesBar)
    {
        AtkUldAsset* asset = (AtkUldAsset*)IMemorySpace.GetUISpace()->Malloc<AtkUldAsset>();
        if (asset is null)
        {
            return null;
        }

        IMemorySpace.Memset(asset, 0, (ulong)sizeof(AtkUldAsset));
        asset->AtkTexture.Ctor();

        AtkUldPart* part = (AtkUldPart*)IMemorySpace.GetUISpace()->Malloc<AtkUldPart>();
        if (part is null)
        {
            IMemorySpace.Free(asset);
            return null;
        }

        IMemorySpace.Memset(part, 0, (ulong)sizeof(AtkUldPart));
        part->UldAsset = asset;

        AtkUldPartsList* parts = (AtkUldPartsList*)IMemorySpace.GetUISpace()->Malloc<AtkUldPartsList>();
        if (parts is null)
        {
            IMemorySpace.Free(asset);
            IMemorySpace.Free(part);
            return null;
        }

        parts->Parts = part;
        parts->PartCount = 1;
        parts->Id = 0;

        AtkImageNode* node = IMemorySpace.GetUISpace()->Create<AtkImageNode>();
        if (parts is null)
        {
            IMemorySpace.Free(asset);
            IMemorySpace.Free(parts);
            IMemorySpace.Free(part);
            return null;
        }

        node->Type = NodeType.Image;
        node->Flags = (byte)ImageNodeFlags.AutoFit;
        node->WrapMode = 0x1;
        node->NodeFlags = NodeFlags.Visible;
        node->Color = new ByteColor { R = 0, G = 0, B = 0, A = 128 };
        node->SetXFloat(statusesBar->RootNode->X);
        node->SetYFloat(statusesBar->RootNode->Y);
        node->SetWidth(statusesBar->RootNode->Width);
        node->SetHeight(statusesBar->RootNode->Height);
        node->SetScale(statusesBar->RootNode->ScaleX, statusesBar->RootNode->ScaleY);

        node->PartsList = parts;

        return node;
    }

    private static void DestroyBackground(AtkImageNode* node)
    {
        IMemorySpace.Free(node->PartsList->Parts->UldAsset);
        IMemorySpace.Free(node->PartsList->Parts);
        IMemorySpace.Free(node->PartsList);

        node->Destroy(false);
        IMemorySpace.Free(node);
    }
}
