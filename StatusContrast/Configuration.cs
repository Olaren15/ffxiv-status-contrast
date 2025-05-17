using System;
using System.Numerics;
using Dalamud.Configuration;
using Lumina.Data.Parsing;

namespace StatusContrast;

[Serializable]
public record Configuration : IPluginConfiguration
{
    public int Version { get; set; } = 1;
    public bool Preview { get; set; } = false;
    public bool FixGaps { get; set; } = true;
    public Vector4 Color { get; set; } = new(0.0f, 0.0f, 0.0f, 0.5f);
}
