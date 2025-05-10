using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace StatusContrast;

public unsafe class GenericStatusNodeFinder(AtkResNode* addonRootNode) : IStatusNodeFinder
{
    public void ForEachNode(Action<Pointer<AtkResNode>, Pointer<AtkResNode>> action)
    {
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
                action(current, addonRootNode);
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
    }
}
