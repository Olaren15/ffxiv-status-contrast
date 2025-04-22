using System;
using Dalamud.Configuration;

namespace StatusContrast;

[Serializable]
public record Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Preview { get; set; } = false;
    public Color Color { get; set; } = new(0, 0, 0, 128);
}
