using System;
using System.Collections.Generic;

namespace StatusContrast;

public static class ListExtension
{
    public static void AddIfNotNull(this List<IntPtr> list, IntPtr item)
    {
        if (item != IntPtr.Zero)
        {
            list.Add(item);
        }
    }
}
