using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace StatusContrast;

public unsafe class BackgroundNodeGroup : IDisposable
{
    private readonly AtkResNode* _attachTarget;
    private readonly List<Pointer<BackgroundNode>> _backgrounds = [];
    private readonly NodeIdProvider _idProvider;

    public BackgroundNodeGroup(IStatusNodeFinder statusNodeFinder, AtkResNode* attachTarget,
        Configuration configuration, NodeIdProvider idProvider)
    {
        _attachTarget = attachTarget;
        _idProvider = idProvider;

        statusNodeFinder.ForEachNode((node, rootNode) => CreateBackground(node, rootNode, configuration));

        Update();
    }

    public void Dispose()
    {
        foreach (BackgroundNode* background in _backgrounds)
        {
            NodeLinker.DetachNode((AtkResNode*)background->ImageNode);

            background->Destroy();
            IMemorySpace.Free(background);
        }

        _backgrounds.Clear();

        GC.SuppressFinalize(this);
    }

    private void CreateBackground(AtkResNode* node, AtkResNode* addonRootNode,
        Configuration configuration)
    {
        BackgroundNode* backgroundNode = (BackgroundNode*)IMemorySpace.GetUISpace()->Malloc<BackgroundNode>();
        if (backgroundNode is null)
        {
            throw new Exception("Failed to allocate background node");
        }

        IMemorySpace.Memset(backgroundNode, 0, (ulong)sizeof(BackgroundNode));
        backgroundNode->Init(node, addonRootNode, configuration, _idProvider);

        _backgrounds.Add(backgroundNode);
        NodeLinker.AttachToNode((AtkResNode*)backgroundNode->ImageNode, _attachTarget);
    }

    public void SetConfiguration(Configuration configuration)
    {
        foreach (BackgroundNode* background in _backgrounds)
        {
            background->PreviewEnabled = configuration.Preview;
            background->FixGapsEnabled = configuration.FixGaps;
            background->Color = configuration.Color;
        }
    }

    public void Update()
    {
        foreach (BackgroundNode* background in _backgrounds)
        {
            background->Update();
        }
    }
}
