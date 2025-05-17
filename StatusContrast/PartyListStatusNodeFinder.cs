using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace StatusContrast;

public static unsafe class PartyListStatusNodeFinder
{
    public static void ForEachNode(AddonPartyList* partyList, Action<Pointer<AtkResNode>> action)
    {
        foreach (AddonPartyList.PartyListMemberStruct partyMember in partyList->PartyMembers)
        {
            ForEachStatusNode(partyMember, action);
        }

        foreach (AddonPartyList.PartyListMemberStruct trustMember in partyList->TrustMembers)
        {
            ForEachStatusNode(trustMember, action);
        }

        ForEachStatusNode(partyList->Chocobo, action);
        ForEachStatusNode(partyList->Pet, action);
    }

    private static void ForEachStatusNode(AddonPartyList.PartyListMemberStruct partyMember,
        Action<Pointer<AtkResNode>> action)
    {
        foreach (Pointer<AtkComponentIconText> statusNode in partyMember.StatusIcons)
        {
            if (statusNode.Value != null)
            {
                action(statusNode.Value->AtkResNode->ParentNode);
            }
        }
    }
}
