﻿using System;
using System.Collections.Generic;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

public unsafe struct BackgroundNodeGroup : IDisposable
{
    private AtkResNode* _followTarget;

    private AtkResNode* _rootNode;
    private readonly List<BackgroundNodeGroup> _children = [];
    private readonly List<IntPtr> _backgrounds = [];

    public BackgroundNodeGroup(AtkResNode* followTarget, AtkResNode* attachTarget)
    {
        _followTarget = followTarget;

        _rootNode = (AtkResNode*)IMemorySpace.GetUISpace()->Malloc<AtkResNode>();
        if (_rootNode is null)
        {
            throw new Exception("Failed to allocate root background node");
        }

        IMemorySpace.Memset(_rootNode, 0, (ulong)sizeof(AtkResNode));
        _rootNode->Ctor();
        _rootNode->Type = NodeType.Res;
        _rootNode->DrawFlags = 0;

        AtkResNode* current = followTarget->ChildNode;
        while (current != null)
        {
            Plugin.Log.Debug("current: {current}, childCount: {childCount}", (nint)current, followTarget->ChildCount);
            if (current->Type == NodeType.Res && current->ChildNode != null)
            {
                _children.Add(new BackgroundNodeGroup(current, _rootNode));
            }

            // Statuses are of type 1001
            if ((ushort)current->Type == 1001)
            {
                BackgroundNode* backgroundNode = (BackgroundNode*)IMemorySpace.GetUISpace()->Malloc<BackgroundNode>();
                if (backgroundNode is null)
                {
                    throw new Exception("Failed to allocate background node");
                }

                IMemorySpace.Memset(backgroundNode, 0, (ulong)sizeof(BackgroundNode));
                backgroundNode->Init(current);

                _backgrounds.Add((IntPtr)backgroundNode);
                NodeLinker.AttachToNode((AtkResNode*)backgroundNode->ImageNode, _rootNode);
            }

            current = current->PrevSiblingNode;
        }

        Update();
        NodeLinker.AttachToNode(_rootNode, attachTarget);
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

        foreach (BackgroundNodeGroup group in _children)
        {
            group.Dispose();
        }

        _children.Clear();

        NodeLinker.DetachNode(_rootNode);
        IMemorySpace.Free(_rootNode);

        _rootNode = null;
        _followTarget = null;
    }

    public void Update()
    {
        _rootNode->NodeFlags = _followTarget->NodeFlags;
        _rootNode->SetXFloat(_followTarget->X);
        _rootNode->SetYFloat(_followTarget->Y);
        _rootNode->SetWidth(_followTarget->Width);
        _rootNode->SetHeight(_followTarget->Height);
        _rootNode->SetScale(_followTarget->ScaleX, _followTarget->ScaleY);

        foreach (BackgroundNodeGroup child in _children)
        {
            child.Update();
        }

        foreach (IntPtr backgroundPtr in _backgrounds)
        {
            BackgroundNode* background = (BackgroundNode*)backgroundPtr;
            background->Update();
        }
    }
}
