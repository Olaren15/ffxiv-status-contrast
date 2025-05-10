using FFXIVClientStructs.FFXIV.Component.GUI;

namespace StatusContrast;

/**
 * The following code is extracted from MidoriKami's KamiToolKit. Copyright 2024 MidoriKami.
 * For licensing see /LICENSE/LICENSE-KamiToolKit.txt
 */
public static unsafe class NodeLinker
{
    public static void AttachToNode(AtkResNode* node, AtkResNode* attachTargetNode)
    {
        // If the child list is empty
        if (attachTargetNode->ChildNode is null && attachTargetNode->ChildCount is 0)
        {
            if ((int)attachTargetNode->Type < 1000)
            {
                attachTargetNode->ChildNode = node;
                node->ParentNode = attachTargetNode;
                attachTargetNode->ChildCount++;
            }
            else
            {
                node->ParentNode = attachTargetNode;
            }
        }
        // Else Add to the List as the First Child
        else
        {
            if ((int)attachTargetNode->Type < 1000)
            {
                attachTargetNode->ChildNode->NextSiblingNode = node;
                node->PrevSiblingNode = attachTargetNode->ChildNode;
                attachTargetNode->ChildNode = node;
                node->ParentNode = attachTargetNode;
                attachTargetNode->ChildCount++;
            }
            else
            {
                node->PrevSiblingNode = attachTargetNode->ChildNode;
                node->ParentNode = attachTargetNode;
            }
        }
    }

    public static void DetachNode(AtkResNode* node)
    {
        if (node is null)
        {
            return;
        }

        if (node->ParentNode is null)
        {
            return;
        }

        if (node->ParentNode->ChildNode == node)
        {
            node->ParentNode->ChildNode = node->PrevSiblingNode;
        }

        if (node->PrevSiblingNode != null)
        {
            node->PrevSiblingNode->NextSiblingNode = node->NextSiblingNode;
        }

        if (node->NextSiblingNode != null)
        {
            node->NextSiblingNode->PrevSiblingNode = node->PrevSiblingNode;
        }

        if ((int)node->ParentNode->Type < 1000)
        {
            node->ParentNode->ChildCount--;
        }
    }
}
