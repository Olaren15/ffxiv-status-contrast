using System;
using FFXIVClientStructs.FFXIV.Client.System.Memory;
using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

public readonly unsafe struct BackgroundNodeGroup : IDisposable
{
    private readonly AtkResNode* _rootNodeToFollow;

    private readonly BackgroundNode* _backgroundNodes;
    private readonly ulong _backgroundNodeCount;

    public AtkResNode* RootNode { get; }

    public BackgroundNodeGroup(AtkResNode* rootNodeToFollow)
    {
        _rootNodeToFollow = rootNodeToFollow;

        RootNode = (AtkResNode*)IMemorySpace.GetUISpace()->Malloc<AtkResNode>();
        if (RootNode is null)
        {
            throw new Exception("Failed to allocate root background node");
        }

        IMemorySpace.Memset(RootNode, 0, (ulong)sizeof(AtkResNode));
        RootNode->Ctor();
        RootNode->Type = NodeType.Res;
        RootNode->NodeFlags = rootNodeToFollow->NodeFlags;
        RootNode->DrawFlags = 0;

        ulong allocSize = (ulong)sizeof(BackgroundNode) * rootNodeToFollow->ChildCount;
        _backgroundNodes = (BackgroundNode*)IMemorySpace.GetUISpace()->Malloc(allocSize, 8);
        if (_backgroundNodes is null)
        {
            throw new Exception("Failed to allocate background nodes");
        }

        IMemorySpace.Memset(_backgroundNodes, 0, allocSize);
        _backgroundNodeCount = rootNodeToFollow->ChildCount;

        AtkResNode* current = rootNodeToFollow->ChildNode;
        for (ulong i = 0; i < _backgroundNodeCount; i++)
        {
            _backgroundNodes[i].Init(current);

            // We only want nodes with type 1001
            if ((ushort)current->Type == 1001)
            {
                NodeLinker.AttachToNode((AtkResNode*)_backgroundNodes[i].ImageNode, RootNode);
            }

            current = current->PrevSiblingNode;
        }

        Update();
    }

    public void Dispose()
    {
        for (ulong i = 0; i < _backgroundNodeCount; i++)
        {
            NodeLinker.DetachNode((AtkResNode*)_backgroundNodes[i].ImageNode);
            _backgroundNodes[i].Destroy();
        }

        IMemorySpace.Free(_backgroundNodes);
        IMemorySpace.Free(RootNode);
    }

    public void Update()
    {
        RootNode->SetXFloat(_rootNodeToFollow->X);
        RootNode->SetYFloat(_rootNodeToFollow->Y);
        RootNode->SetWidth(_rootNodeToFollow->Width);
        RootNode->SetHeight(_rootNodeToFollow->Height);
        RootNode->SetScale(_rootNodeToFollow->ScaleX, _rootNodeToFollow->ScaleY);

        for (ulong i = 0; i < _backgroundNodeCount; i++)
        {
            _backgroundNodes[i].Update();
        }
    }
}
