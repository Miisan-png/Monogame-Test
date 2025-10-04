using ImGuiNET;
using System.Numerics;

namespace Snow.Editor
{
    public class ColorModulatePanel
    {
        public bool IsOpen { get; set; } = true;
        private Vector3 _colorRGB = new Vector3(0.77f, 0.0f, 0.66f);
        
        public Microsoft.Xna.Framework.Color CurrentColor { get; private set; }

        public ColorModulatePanel()
        {
            UpdateColor();
        }

        public void Render()
        {
            if (!IsOpen) return;

            bool isOpen = IsOpen;
            ImGui.Begin("Canvas Modulate", ref isOpen);
            IsOpen = isOpen;

            ImGui.TextColored(new Vector4(1f, 0.8f, 0.4f, 1f), "Canvas Color Modulation");
            ImGui.Separator();

            if (ImGui.ColorPicker3("Canvas Color", ref _colorRGB, ImGuiColorEditFlags.PickerHueWheel))
            {
                UpdateColor();
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text("Presets:");

            Vector3[] presets = new Vector3[]
            {
                new Vector3(0.77f, 0.0f, 0.66f),
                new Vector3(0.5f, 0.3f, 0.8f),
                new Vector3(0.8f, 0.4f, 0.5f),
                new Vector3(0.3f, 0.6f, 0.9f),
                new Vector3(0.9f, 0.7f, 0.3f),
                new Vector3(0.4f, 0.8f, 0.5f),
                new Vector3(1.0f, 1.0f, 1.0f),
                new Vector3(0.0f, 0.0f, 0.0f)
            };

            string[] presetNames = new string[]
            {
                "Purple Dream",
                "Lavender Haze",
                "Rose Tint",
                "Ocean Blue",
                "Golden Hour",
                "Forest Mist",
                "Pure White",
                "Deep Black"
            };

            for (int i = 0; i < presets.Length; i++)
            {
                if (i % 2 == 1) ImGui.SameLine();

                var preset = presets[i];
                var color = new Vector4(preset.X, preset.Y, preset.Z, 1f);
                
                ImGui.PushID(i);
                if (ImGui.ColorButton(presetNames[i], color, ImGuiColorEditFlags.None, new Vector2(60, 30)))
                {
                    _colorRGB = preset;
                    UpdateColor();
                }
                ImGui.PopID();
                
                if (ImGui.IsItemHovered())
                {
                    ImGui.SetTooltip(presetNames[i]);
                }
            }

            ImGui.Spacing();
            ImGui.Separator();
            ImGui.Text($"RGB: ({(int)(_colorRGB.X * 255)}, {(int)(_colorRGB.Y * 255)}, {(int)(_colorRGB.Z * 255)})");
            ImGui.Text($"Hex: #{(int)(_colorRGB.X * 255):X2}{(int)(_colorRGB.Y * 255):X2}{(int)(_colorRGB.Z * 255):X2}");

            ImGui.End();
        }

        private void UpdateColor()
        {
            CurrentColor = new Microsoft.Xna.Framework.Color(
                (byte)(_colorRGB.X * 255),
                (byte)(_colorRGB.Y * 255),
                (byte)(_colorRGB.Z * 255)
            );
        }
    }
}