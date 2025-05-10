using System.Collections.Generic;
using FFXIVClientStructs.Interop;

namespace StatusContrast;

public static class ListExtension
{
    public static void AddIfNotNull<T>(this List<Pointer<T>> list, Pointer<T> item) where T : unmanaged
    {
        if (item != null)
        {
            list.Add(item);
        }
    }
}
