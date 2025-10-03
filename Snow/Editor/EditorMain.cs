using ImGuiNET;

namespace Snow.Editor
{
    public class EditorMain
    {
        private EditorViewport _viewport;

        public EditorMain(GameRenderer gameRenderer)
        {
            _viewport = new EditorViewport(gameRenderer);
        }

        public void SetGameTexturePtr(System.IntPtr texturePtr)
        {
            _viewport.SetGameTexturePtr(texturePtr);
        }

        public void Render()
        {
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());

            _viewport.Render();
        }
    }
}
