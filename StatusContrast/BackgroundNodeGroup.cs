using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

public unsafe class BackgroundNodeGroup : IDisposable
{
    private readonly List<IntPtr> _backgrounds = [];
    private readonly NodeIdProvider _idProvider;

    public BackgroundNodeGroup(AtkResNode* addonRootNode, AtkResNode* attachTarget, Configuration configuration, NodeIdProvider idProvider)
    {
        _idProvider = idProvider;

        AtkResNode* current = addonRootNode;
        // Travers node tree to find status nodes
        while (current != null)
        {
            if (current->ChildNode != null)
            {
                current = current->ChildNode;
                continue;
            }

            // Statuses are of type 1001
            if ((ushort)current->Type == 1001)
            {
                CreateBackground(current, addonRootNode, attachTarget, configuration);
            }

            if (current->PrevSiblingNode != null)
            {
                current = current->PrevSiblingNode;
                continue;
            }

            AtkResNode* nextParent = current->ParentNode;
            while (true)
            {
                if (nextParent == addonRootNode)
                {
                    current = null;
                    break;
                }

                if (nextParent->PrevSiblingNode != null)
                {
                    current = nextParent->PrevSiblingNode;
                    break;
                }

                nextParent = nextParent->ParentNode;
            }
        }

        Update();
    }

    public void Dispose()
    {
        foreach (IntPtr background in _backgrounds)
        {
            BackgroundNode* backgroundNode = (BackgroundNode*)background;
            NodeLinker.DetachNode((AtkResNode*)backgroundNode->ImageNode);

            backgroundNode->Destroy();
            IMemorySpace.Free(backgroundNode);
        }

        _backgrounds.Clear();

        GC.SuppressFinalize(this);
    }

    private void CreateBackground(AtkResNode* current, AtkResNode* addonRootNode, AtkResNode* attachTarget,
        Configuration configuration)
    {
        BackgroundNode* backgroundNode = (BackgroundNode*)IMemorySpace.GetUISpace()->Malloc<BackgroundNode>();
        if (backgroundNode is null)
        {
            throw new Exception("Failed to allocate background node");
        }

        IMemorySpace.Memset(backgroundNode, 0, (ulong)sizeof(BackgroundNode));
        backgroundNode->Init(current, addonRootNode, configuration, _idProvider);

        _backgrounds.Add((IntPtr)backgroundNode);
        NodeLinker.AttachToNode((AtkResNode*)backgroundNode->ImageNode, attachTarget);
    }

    public void SetConfiguration(Configuration configuration)
    {
        foreach (IntPtr backgroundPtr in _backgrounds)
        {
            BackgroundNode* background = (BackgroundNode*)backgroundPtr;
            background->PreviewEnabled = configuration.Preview;
            background->FixGapsEnabled = configuration.FixGaps;
            background->Color = configuration.Color;
        }
    }

    public void Update()
    {
        foreach (IntPtr backgroundPtr in _backgrounds)
        {
            BackgroundNode* background = (BackgroundNode*)backgroundPtr;
            background->Update();
        }
    }
}
