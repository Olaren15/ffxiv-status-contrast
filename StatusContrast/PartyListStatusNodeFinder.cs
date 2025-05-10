using System;
using FFXIVClientStructs.FFXIV.Client.UI;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace StatusContrast;

// Broken af
public unsafe class PartyListStatusNodeFinder(AddonPartyList* partyList) : IStatusNodeFinder
{
    public void ForEachNode(Action<Pointer<AtkResNode>, Pointer<AtkResNode>> action)
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

    private void ForEachStatusNode(AddonPartyList.PartyListMemberStruct partyMember,
        Action<Pointer<AtkResNode>, Pointer<AtkResNode>> action)
    {
        foreach (Pointer<AtkComponentIconText> statusNode in partyMember.StatusIcons)
        {
            if (statusNode.Value != null)
            {
                action(statusNode.Value->AtkResNode->ParentNode, null);
            }
        }
    }
}
