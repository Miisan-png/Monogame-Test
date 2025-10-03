using ImGuiNET;

namespace Snow.Editor
{
    public class EditorMain
    {
        private EditorViewport _viewport;
        private TilemapEditor _tilemapEditor;
        private bool _showTilemapEditor = true;

        public EditorMain(GameRenderer gameRenderer, Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _viewport = new EditorViewport(gameRenderer);
            _tilemapEditor = new TilemapEditor(gameRenderer, graphicsDevice, imGuiRenderer);
        }

        public void SetGameTexturePtr(System.IntPtr texturePtr)
        {
            _viewport.SetGameTexturePtr(texturePtr);
        }

        public void Render()
        {
            ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Windows"))
                {
                    bool viewportOpen = _viewport.IsOpen;
                    if (ImGui.MenuItem("Game Viewport", null, viewportOpen))
                    {
                        _viewport.IsOpen = !_viewport.IsOpen;
                    }
                    
                    if (ImGui.MenuItem("Tilemap Editor", null, _showTilemapEditor))
                    {
                        _showTilemapEditor = !_showTilemapEditor;
                    }
                    
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }

            _viewport.Render();

            if (_showTilemapEditor)
            {
                _tilemapEditor.Render();
            }
        }
    }
}
