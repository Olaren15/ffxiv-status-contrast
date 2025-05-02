using System.Numerics;
using Dalamud.Interface.Utility.Raii;
using Dalamud.Interface.Windowing;
using ImGuiNET;

namespace StatusContrast;

public class SettingsWindow : Window
{
    private readonly ConfigurationRepository _configurationRepository;
    private Vector4 _color;
    private bool _fixGaps;
    private bool _preview;

    public SettingsWindow(ConfigurationRepository configurationRepository) : base(
        "StatusContrast Settings###StatusContrast settings")
    {
        _configurationRepository = configurationRepository;

        SizeConstraints = new WindowSizeConstraints
        {
            MinimumSize = new Vector2(200, 100), MaximumSize = new Vector2(float.MaxValue, float.MaxValue)
        };

        Configuration config = _configurationRepository.GetConfiguration();

        _preview = config.Preview;
        _fixGaps = config.FixGaps;
        _color = config.Color;
    }

    public void Show()
    {
        IsOpen = true;
    }

    public override void Draw()
    {
        ImGui.Checkbox("Preview", ref _preview);

        ImGui.Checkbox("Fix Gaps", ref _fixGaps);
        ImGui.SameLine();
        ImGui.TextDisabled("(?)");
        if (ImGui.IsItemHovered())
        {
            using (ImRaii.Tooltip())
            {
                ImGui.TextUnformatted("For some reason, the game has inconsistent spacing between status icons.\n" +
                                      "You probably want this setting on, but the toggle is here if you want to disable it");
            }
        }

        ImGui.ColorEdit4("Background", ref _color,
            ImGuiColorEditFlags.InputRGB | ImGuiColorEditFlags.PickerHueBar | ImGuiColorEditFlags.AlphaBar |
            ImGuiColorEditFlags.Float | ImGuiColorEditFlags.DisplayRGB);

        _configurationRepository.UpdateConfiguration(new Configuration
        {
            Version = 1, Preview = _preview, FixGaps = _fixGaps, Color = _color
        });
    }
}
