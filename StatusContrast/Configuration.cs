using System;
using Dalamud.Configuration;

namespace StatusContrast;

[Serializable]
public record Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
}
