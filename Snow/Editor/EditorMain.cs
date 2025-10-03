using ImGuiNET;

namespace Snow.Editor
{
    public class EditorMain
    {
        private EditorViewport _viewport;
        private TilemapEditor _tilemapEditor;
        private ActorEditor _actorEditor;
        private bool _showTilemapEditor = true;
        private bool _showActorEditor = false;

        public EditorMain(GameRenderer gameRenderer, Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _viewport = new EditorViewport(gameRenderer);
            _tilemapEditor = new TilemapEditor(gameRenderer, graphicsDevice, imGuiRenderer);
            _actorEditor = new ActorEditor(graphicsDevice, imGuiRenderer);
        }

        public void SetGameTexturePtr(System.IntPtr texturePtr)
        {
            _viewport.SetGameTexturePtr(texturePtr);
        }

        public void Update(float deltaTime)
        {
            _actorEditor.Update(deltaTime);
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
                    
                    if (ImGui.MenuItem("Actor Editor", null, _showActorEditor))
                    {
                        _showActorEditor = !_showActorEditor;
                        _actorEditor.IsOpen = _showActorEditor;
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

            if (_showActorEditor)
            {
                _actorEditor.Render();
                _showActorEditor = _actorEditor.IsOpen;
            }
        }
    }
}
