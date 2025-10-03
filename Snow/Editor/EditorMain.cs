using ImGuiNET;
using Snow.Engine;
using System;
using System.IO;

namespace Snow.Editor
{
    public class EditorMain : IDisposable
    {
        private EditorViewport _viewport;
        private TilemapEditor _tilemapEditor;
        private ActorEditor _actorEditor;
        private bool _showTilemapEditor = true;
        private bool _showActorEditor = false;
        
        private string _currentScenePath;
        private SceneData _currentScene;
        private MultiFileWatcher _fileWatcher;
        private GameRenderer _gameRenderer;

        public EditorMain(GameRenderer gameRenderer, Microsoft.Xna.Framework.Graphics.GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _gameRenderer = gameRenderer;
            _viewport = new EditorViewport(gameRenderer);
            _tilemapEditor = new TilemapEditor(gameRenderer, graphicsDevice, imGuiRenderer);
            _actorEditor = new ActorEditor(graphicsDevice, imGuiRenderer);
            _fileWatcher = new MultiFileWatcher();
        }

        public void SetGameTexturePtr(System.IntPtr texturePtr)
        {
            _viewport.SetGameTexturePtr(texturePtr);
        }

        public void Update(float deltaTime)
        {
            _actorEditor.Update(deltaTime);
        }

        private void LoadSceneFile(string scenePath)
        {
            try
            {
                _currentScenePath = scenePath;
                _currentScene = SceneParser.ParseScene(scenePath);
                
                // Load scene into Actor Editor
                _actorEditor.LoadSceneData(_currentScene, scenePath);
                
                // Load tilemap into Tilemap Editor if specified
                if (!string.IsNullOrEmpty(_currentScene.Tilemap) && File.Exists(_currentScene.Tilemap))
                {
                    _tilemapEditor.LoadMap(_currentScene.Tilemap);
                    
                    // Load tileset if specified
                    if (!string.IsNullOrEmpty(_currentScene.Tileset) && File.Exists(_currentScene.Tileset))
                    {
                        _tilemapEditor.LoadTileset(_currentScene.Tileset);
                    }
                }
                
                // Setup file watchers for hot reload
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
            
            // Watch the scene file
            if (!string.IsNullOrEmpty(_currentScenePath))
            {
                _fileWatcher.WatchFile(_currentScenePath, OnSceneFileChanged);
            }
            
            // Watch the tilemap file
            if (_currentScene != null && !string.IsNullOrEmpty(_currentScene.Tilemap))
            {
                _fileWatcher.WatchFile(_currentScene.Tilemap, OnTilemapFileChanged);
            }
        }

        private void OnSceneFileChanged(string filePath)
        {
            System.Console.WriteLine($"[Editor] Scene file changed, reloading: {filePath}");
            LoadSceneFile(filePath);
            
            // Reload the game viewport
            _gameRenderer.ReloadLevel();
        }

        private void OnTilemapFileChanged(string filePath)
        {
            System.Console.WriteLine($"[Editor] Tilemap file changed, reloading: {filePath}");
            
            // Reload tilemap in editor
            if (File.Exists(filePath))
            {
                _tilemapEditor.LoadMap(filePath);
            }
            
            // Reload the game viewport
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
            ofn.initialDir = Directory.GetCurrentDirectory();
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
