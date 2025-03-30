using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using Dalamud.Plugin;
using Dalamud.Plugin.Services;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using KamiToolKit;
using KamiToolKit.Classes;
using KamiToolKit.Nodes;
using KamiToolKit.Nodes.NodeStyles;

namespace StatusContrast;

public unsafe class BackgroundNodes(
    AtkUnitBase*[] statusNodes,
    IAddonLifecycle addonLifecycle,
    IFramework framework,
    IGameGui gameGui,
    IDalamudPluginInterface dalamudPluginInterface)
    : NativeUiOverlayController(addonLifecycle, framework, gameGui)
{
    private readonly List<BackgroundImageNode> _backgroundNodes = [];
    private readonly NativeController _nativeController = new(dalamudPluginInterface);

    protected override void PreAttach()
    {
    }

    protected override void AttachNodes(AddonNamePlate* addonNamePlate)
    {
        foreach (AtkUnitBase* resNode in statusNodes)
        {
            BackgroundImageNode newNode = new();
            NodeBaseStyle style = new()
            {
                Position = new Vector2(resNode->RootNode->X, resNode->RootNode->Y),
                Size = new Vector2(resNode->RootNode->Width, resNode->RootNode->Height),
                Scale = new Vector2(resNode->RootNode->ScaleX, resNode->RootNode->ScaleY),
                Color = new Vector4(0.0f, 0.0f, 0.0f, 0.5f)
            };
            newNode.SetStyle(style);

            _backgroundNodes.Add(newNode);

            _nativeController.AttachToAddon(newNode, (AtkUnitBase*)addonNamePlate, addonNamePlate->RootNode,
                NodePosition.AsFirstChild);
        }
    }

    protected override void DetachNodes(AddonNamePlate* addonNamePlate)
    {
        foreach (BackgroundImageNode backgroundNode in _backgroundNodes.OfType<BackgroundImageNode>())
        {
            _nativeController.DetachFromAddon(backgroundNode, (AtkUnitBase*)addonNamePlate);
            backgroundNode.Dispose();
        }
    }
}
