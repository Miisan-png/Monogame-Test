using ImGuiNET;
using Snow.Engine;
using System;
using System.IO;

namespace Snow.Editor
{
    public class EditorMain : IDisposable
    {
        private SceneView _sceneView;
        private LevelEditorView _levelEditorView;
        private ActorEditor _actorEditor;
        private TilemapEditorOverlay _tilemapOverlay;
        private TilePalettePanel _tilePalette;
        private ToolsPanel _toolsPanel;
        
        private string _currentScenePath;
        private SceneData _currentScene;
        private MultiFileWatcher _fileWatcher;
        private GameRenderer _gameRenderer;
        

        public EditorMain(GameRenderer gameRenderer, Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _gameRenderer = gameRenderer;
            _tilemapOverlay = new TilemapEditorOverlay(graphicsDevice, imGuiRenderer);
            _tilemapOverlay.SetMapChangedCallback(OnTilemapChanged);
            
            _sceneView = new SceneView(gameRenderer, _tilemapOverlay);
            _levelEditorView = new LevelEditorView(_tilemapOverlay, graphicsDevice, imGuiRenderer);
            _actorEditor = new ActorEditor(graphicsDevice, imGuiRenderer);
            _actorEditor.SetSceneChangedCallback(OnActorEditorSceneChanged);
            _tilePalette = new TilePalettePanel(_tilemapOverlay);
            _toolsPanel = new ToolsPanel(_tilemapOverlay, _actorEditor);
            
            _fileWatcher = new MultiFileWatcher();
            
            EnsureDefaultMapExists();
            AutoLoadDefaultScene();
        }

        private void EnsureDefaultMapExists()
        {
            string defaultMapPath = "levels/lvl.tilemap";
            
            if (!File.Exists(defaultMapPath))
            {
                System.Console.WriteLine($"[Editor] Default map not found, creating: {defaultMapPath}");
                
                Directory.CreateDirectory("levels");
                
                var defaultMap = TilemapFormat.CreateNew(16, 40, 23);
                
                for (int y = 20; y < 23; y++)
                {
                    for (int x = 0; x < 40; x++)
                    {
                        defaultMap.Tiles[y][x] = 1;
                        defaultMap.Collision[y][x] = true;
                    }
                }
                
                TilemapFormat.Save(defaultMapPath, defaultMap);
                System.Console.WriteLine($"[Editor] Created default tilemap with ground at bottom");
            }
        }

        private void AutoLoadDefaultScene()
        {
            string defaultScenePath = "scenes/forest_1.scene";
            if (File.Exists(defaultScenePath))
            {
                LoadSceneFile(defaultScenePath);
                System.Console.WriteLine($"[Editor] Auto-loaded default scene: {defaultScenePath}");
            }
            else
            {
                System.Console.WriteLine($"[Editor] Default scene not found: {defaultScenePath}");
                System.Console.WriteLine($"[Editor] You can still edit tilemaps using Tools panel");
            }
        }

        private void OnTilemapChanged(string mapPath)
        {
            System.Console.WriteLine($"[Editor] Tilemap changed, reloading game: {mapPath}");
            _gameRenderer.ReloadLevel();
        }

        private void OnActorEditorSceneChanged(string scenePath)
        {
            System.Console.WriteLine($"[Editor] Actor editor saved scene, reloading game: {scenePath}");
            _gameRenderer.ReloadLevel();
        }

        public void SetGameTexturePtr(System.IntPtr texturePtr)
        {
            _sceneView.SetGameTexturePtr(texturePtr);
        }

        public void Update(float deltaTime)
        {
            _actorEditor.Update(deltaTime);
            _tilemapOverlay.Update(deltaTime);
        }

        private void LoadSceneFile(string scenePath)
        {
            try
            {
                _currentScenePath = scenePath;
                _currentScene = SceneParser.ParseScene(scenePath);
                
                if (!string.IsNullOrEmpty(_currentScene.Tileset) && File.Exists(_currentScene.Tileset))
                {
                    _tilemapOverlay.LoadTileset(_currentScene.Tileset);
                    System.Console.WriteLine($"[Editor] Tileset loaded: {_currentScene.Tileset}");
                }
                
                if (!string.IsNullOrEmpty(_currentScene.Tilemap))
                {
                    if (File.Exists(_currentScene.Tilemap))
                    {
                        _tilemapOverlay.LoadMap(_currentScene.Tilemap);
                        System.Console.WriteLine($"[Editor] Map loaded: {_currentScene.Tilemap}");
                    }
                    else
                    {
                        System.Console.WriteLine($"[Editor] Map file not found: {_currentScene.Tilemap}");
                        System.Console.WriteLine($"[Editor] Create a new map using Tools -> New Map");
                    }
                }
                
                _actorEditor.LoadSceneData(_currentScene, scenePath);
                
                SetupFileWatchers();
                
                System.Console.WriteLine($"[Editor] Scene loaded: {scenePath}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[Editor] Failed to load scene: {ex.Message}");
            }
        }

        private void SetupFileWatchers()
        {
            _fileWatcher.ClearWatchers();
            
            if (!string.IsNullOrEmpty(_currentScenePath))
            {
                _fileWatcher.WatchFile(_currentScenePath, OnSceneFileChanged);
            }
            
            if (_currentScene != null && !string.IsNullOrEmpty(_currentScene.Tilemap))
            {
                _fileWatcher.WatchFile(_currentScene.Tilemap, OnTilemapFileChanged);
            }
        }

        private void OnSceneFileChanged(string filePath)
        {
            System.Console.WriteLine($"[Editor] Scene file changed, reloading: {filePath}");
            LoadSceneFile(filePath);
            _gameRenderer.ReloadLevel();
        }

        private void OnTilemapFileChanged(string filePath)
        {
            System.Console.WriteLine($"[Editor] Tilemap file changed, reloading: {filePath}");
            
            if (File.Exists(filePath))
            {
                _tilemapOverlay.LoadMap(filePath);
            }
            
            _gameRenderer.ReloadLevel();
        }

        public void Render()
{
    ImGui.DockSpaceOverViewport(ImGui.GetMainViewport());

    if (ImGui.BeginMainMenuBar())
    {
        if (ImGui.BeginMenu("File"))
        {
            if (ImGui.MenuItem("Open Scene"))
            {
                string path = ShowOpenFileDialog("scene");
                if (!string.IsNullOrEmpty(path))
                {
                    LoadSceneFile(path);
                }
            }

            if (ImGui.MenuItem("Save Scene", null, false, _currentScene != null))
            {
                if (!string.IsNullOrEmpty(_currentScenePath))
                {
                    _actorEditor.SaveSceneData();
                    System.Console.WriteLine($"[Editor] Scene saved: {_currentScenePath}");
                }
            }

            ImGui.Separator();

            if (ImGui.MenuItem("Reload Scene", "Ctrl+R", false, _currentScene != null))
            {
                if (!string.IsNullOrEmpty(_currentScenePath))
                {
                    LoadSceneFile(_currentScenePath);
                    _gameRenderer.ReloadLevel();
                }
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Windows"))
        {
            bool sceneViewOpen = _sceneView.IsOpen;
            if (ImGui.MenuItem("Scene View", null, sceneViewOpen))
            {
                _sceneView.IsOpen = !_sceneView.IsOpen;
            }

            bool levelEditorOpen = _levelEditorView.IsOpen;
            if (ImGui.MenuItem("Level Editor", null, levelEditorOpen))
            {
                _levelEditorView.IsOpen = !_levelEditorView.IsOpen;
            }

            bool actorEditorOpen = _actorEditor.IsOpen;
            if (ImGui.MenuItem("Actor Editor", null, actorEditorOpen))
            {
                _actorEditor.IsOpen = !_actorEditor.IsOpen;
            }

            bool tilePaletteOpen = _tilePalette.IsOpen;
            if (ImGui.MenuItem("Tile Palette", null, tilePaletteOpen))
            {
                _tilePalette.IsOpen = !_tilePalette.IsOpen;
            }

            bool toolsPanelOpen = _toolsPanel.IsOpen;
            if (ImGui.MenuItem("Tools", null, toolsPanelOpen))
            {
                _toolsPanel.IsOpen = !_toolsPanel.IsOpen;
            }

            ImGui.EndMenu();
        }

        if (ImGui.BeginMenu("Themes"))
        {
            if (ImGui.MenuItem("Dark", null, ThemeManager.CurrentTheme == ThemeManager.Theme.Dark))
                ThemeManager.ApplyTheme(ThemeManager.Theme.Dark);

            if (ImGui.MenuItem("Light", null, ThemeManager.CurrentTheme == ThemeManager.Theme.Light))
                ThemeManager.ApplyTheme(ThemeManager.Theme.Light);

            if (ImGui.MenuItem("Classic", null, ThemeManager.CurrentTheme == ThemeManager.Theme.Classic))
                ThemeManager.ApplyTheme(ThemeManager.Theme.Classic);

            if (ImGui.MenuItem("Custom Blue", null, ThemeManager.CurrentTheme == ThemeManager.Theme.CustomBlue))
                ThemeManager.ApplyTheme(ThemeManager.Theme.CustomBlue);

            ImGui.EndMenu();
        }
        
        ImGui.EndMainMenuBar();
    }

    _sceneView.Render();
    _levelEditorView.Render();
    _actorEditor.Render();
    _tilePalette.Render();
    _toolsPanel.Render();
}

        private string ShowOpenFileDialog(string extension)
        {
            var ofn = new OpenFileName();
            ofn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);
            
            string filter = extension.ToLower() == "scene" ? "Scene Files\0*.scene\0All Files\0*.*\0\0" : 
                           "All Files\0*.*\0\0";
            
            ofn.filter = filter;
            ofn.file = new string(new char[256]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = System.IO.Directory.GetCurrentDirectory();
            ofn.title = "Open File";
            ofn.defExt = extension;
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;

            if (GetOpenFileName(ofn))
            {
                return ofn.file;
            }
            
            return null;
        }

        [System.Runtime.InteropServices.DllImport("comdlg32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool GetOpenFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName ofn);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private class OpenFileName
        {
            public int structSize = 0;
            public System.IntPtr dlgOwner = System.IntPtr.Zero;
            public System.IntPtr instance = System.IntPtr.Zero;
            public string filter = null;
            public string customFilter = null;
            public int maxCustFilter = 0;
            public int filterIndex = 0;
            public string file = null;
            public int maxFile = 0;
            public string fileTitle = null;
            public int maxFileTitle = 0;
            public string initialDir = null;
            public string title = null;
            public int flags = 0;
            public short fileOffset = 0;
            public short fileExtension = 0;
            public string defExt = null;
            public System.IntPtr custData = System.IntPtr.Zero;
            public System.IntPtr hook = System.IntPtr.Zero;
            public string templateName = null;
            public System.IntPtr reservedPtr = System.IntPtr.Zero;
            public int reserved = 0;
            public int flagsEx = 0;
        }

        public void Dispose()
        {
            _fileWatcher?.Dispose();
            _actorEditor?.Dispose();
        }
    }
}