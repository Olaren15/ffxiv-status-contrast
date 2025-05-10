using System;
using FFXIVClientStructs.FFXIV.Component.GUI;
using FFXIVClientStructs.Interop;

namespace StatusContrast;

public interface IStatusNodeFinder
{
    public void ForEachNode(Action<Pointer<AtkResNode>, Pointer<AtkResNode>> action);
}
