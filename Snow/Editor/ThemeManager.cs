using ImGuiNET;
using System.Numerics;

namespace Snow.Editor
{
    public static class ThemeManager
    {
        public enum Theme
        {
            Dark,
            Light,
            Classic,
            CustomBlue
        }

        public static Theme CurrentTheme { get; private set; } = Theme.Dark;

        public static void ApplyTheme(Theme theme)
        {
            switch (theme)
            {
                case Theme.Dark:
                    ImGui.StyleColorsDark();
                    break;
                case Theme.Light:
                    ImGui.StyleColorsLight();
                    break;
                case Theme.Classic:
                    ImGui.StyleColorsClassic();
                    break;
                case Theme.CustomBlue:
                    ApplyCustomBlue();
                    break;
            }

            CurrentTheme = theme;
        }

        private static void ApplyCustomBlue()
        {
            var style = ImGui.GetStyle();
            ImGui.StyleColorsDark();

            style.Colors[(int)ImGuiCol.WindowBg]        = new Vector4(0.1f, 0.1f, 0.12f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBg]         = new Vector4(0.05f, 0.05f, 0.1f, 1.0f);
            style.Colors[(int)ImGuiCol.TitleBgActive]   = new Vector4(0.1f, 0.2f, 0.4f, 1.0f);
            style.Colors[(int)ImGuiCol.Button]          = new Vector4(0.2f, 0.3f, 0.6f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonHovered]   = new Vector4(0.3f, 0.4f, 0.8f, 1.0f);
            style.Colors[(int)ImGuiCol.ButtonActive]    = new Vector4(0.1f, 0.5f, 0.7f, 1.0f);
            style.Colors[(int)ImGuiCol.Header]          = new Vector4(0.2f, 0.35f, 0.7f, 1.0f);
            style.Colors[(int)ImGuiCol.HeaderHovered]   = new Vector4(0.3f, 0.45f, 0.9f, 1.0f);
            style.Colors[(int)ImGuiCol.HeaderActive]    = new Vector4(0.25f, 0.5f, 0.9f, 1.0f);
        }
    }
}
