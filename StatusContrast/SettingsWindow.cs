using System.Numerics;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace StatusContrast;

public class SettingsWindow : Window
{
    private bool _preview;
    private Vector4 _color;
    private readonly ConfigurationRepository _configurationRepository;

    public SettingsWindow(ConfigurationRepository configurationRepository) : base(
        "StatusContrast Settings###StatusContrast settings")
    {
        _configurationRepository = configurationRepository;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 100), MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        _preview = _configurationRepository.GetConfiguration().Preview;
        _color = configurationRepository.GetConfiguration().Color.ToVector4();
    }

    public void Show()
    {
        IsOpen = true;
    }

    public override void Draw()
    {
        ImGui.Checkbox("Preview", ref _preview);

        ImGui.ColorEdit4("Background", ref _color,
            ImGuiColorEditFlags.InputRGB | ImGuiColorEditFlags.PickerHueBar | ImGuiColorEditFlags.AlphaBar |
            ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB);

        _configurationRepository.UpdateConfiguration(new Configuration
        {
            Version = 1, Preview = _preview, Color = new Color(_color)
        });
    }
}
