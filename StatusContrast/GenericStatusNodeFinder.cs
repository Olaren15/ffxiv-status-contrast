using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace StatusContrast;

public static unsafe class GenericStatusNodeFinder
{
    public static void ForEachNode(AtkResNode* addonRootNode, Action<Pointer<AtkResNode>> action)
    {
        AtkResNode* current = addonRootNode;
        // Travers node tree to find status nodes
        while (current != null)
        {
            // Statuses are of type 1001
            if ((ushort)current->Type == 1001)
            {
                action(current);
            }
            else if (current->ChildNode != null)
            {
                current = current->ChildNode;
                continue;
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
