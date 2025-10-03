using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;
using System.Runtime.InteropServices;

namespace Snow.Editor
{
    public class TilemapEditor
    {
        [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
        private class OpenFileName
        {
            public int structSize = 0;
            public IntPtr dlgOwner = IntPtr.Zero;
            public IntPtr instance = IntPtr.Zero;
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
            public IntPtr custData = IntPtr.Zero;
            public IntPtr hook = IntPtr.Zero;
            public string templateName = null;
            public IntPtr reservedPtr = IntPtr.Zero;
            public int reserved = 0;
            public int flagsEx = 0;
        }

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetOpenFileName([In, Out] OpenFileName ofn);

        [DllImport("comdlg32.dll", SetLastError = true, CharSet = CharSet.Auto)]
        private static extern bool GetSaveFileName([In, Out] OpenFileName ofn);

        private GameRenderer _gameRenderer;
        private GraphicsDevice _graphicsDevice;
        
        private int _gridWidth = 40;
        private int _gridHeight = 23;
        private int _tileSize = 16;
        private int[][] _worldData;
        private bool[][] _collisionData;
        
        private Texture2D _tilesetTexture;
        private string _tilesetPath = "";
        private int _tilesPerRow;
        private int _tilesPerColumn;
        
        private List<System.Numerics.Vector2> _selectedTiles = new List<System.Numerics.Vector2>();
        private System.Numerics.Vector2 _selectionStart = System.Numerics.Vector2.Zero;
        private bool _isSelecting = false;
        
        private bool _collisionMode = false;
        
        private System.Numerics.Vector2 _viewportScroll = System.Numerics.Vector2.Zero;
        private float _zoom = 2.0f;
        private float _paletteZoom = 2.0f;
        
        private Stack<EditorAction> _undoStack = new Stack<EditorAction>();
        private Stack<EditorAction> _redoStack = new Stack<EditorAction>();
        
        private string _statusMessage = "";
        private float _statusMessageTimer = 0f;
        private bool _statusMessageIsError = false;
        
        private IntPtr _tilesetTexturePtr = IntPtr.Zero;
        private MonoGame.ImGuiNet.ImGuiRenderer _imGuiRenderer;

        public TilemapEditor(GameRenderer gameRenderer, GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _gameRenderer = gameRenderer;
            _graphicsDevice = graphicsDevice;
            _imGuiRenderer = imGuiRenderer;
            
            InitializeGridData();
        }

        private void InitializeGridData()
        {
            _worldData = new int[_gridHeight][];
            _collisionData = new bool[_gridHeight][];
            
            for (int y = 0; y < _gridHeight; y++)
            {
                _worldData[y] = new int[_gridWidth];
                _collisionData[y] = new bool[_gridWidth];
                
                for (int x = 0; x < _gridWidth; x++)
                {
                    _worldData[y][x] = 0;
                    _collisionData[y][x] = false;
                }
            }
        }

        public void LoadTileset(string path)
        {
            try
            {
                if (_tilesetTexture != null)
                {
                    _tilesetTexture.Dispose();
                    if (_tilesetTexturePtr != IntPtr.Zero)
                    {
                        _imGuiRenderer.UnbindTexture(_tilesetTexturePtr);
                    }
                }
                
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    _tilesetTexture = Texture2D.FromStream(_graphicsDevice, stream);
                }
                
                _tilesetPath = path;
                _tilesPerRow = _tilesetTexture.Width / _tileSize;
                _tilesPerColumn = _tilesetTexture.Height / _tileSize;
                
                _tilesetTexturePtr = _imGuiRenderer.BindTexture(_tilesetTexture);
                
                SetStatusMessage($"Tileset loaded: {Path.GetFileName(path)}", false);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Failed to load tileset: {ex.Message}", true);
            }
        }

        public void NewMap(int width, int height, int tileSize)
        {
            _gridWidth = width;
            _gridHeight = height;
            _tileSize = tileSize;
            
            InitializeGridData();
            
            if (!string.IsNullOrEmpty(_tilesetPath))
            {
                _tilesPerRow = _tilesetTexture.Width / _tileSize;
                _tilesPerColumn = _tilesetTexture.Height / _tileSize;
            }
        }

        public void SaveMap(string path)
        {
            try
            {
                var mapData = new
                {
                    tile_size = _tileSize,
                    grid_width = _gridWidth,
                    grid_height = _gridHeight,
                    world_data = _worldData,
                    collision_data = _collisionData
                };
                
                var options = new JsonSerializerOptions { WriteIndented = true };
                string json = JsonSerializer.Serialize(mapData, options);
                File.WriteAllText(path, json);
                
                SetStatusMessage($"Map saved: {Path.GetFileName(path)}", false);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Failed to save map: {ex.Message}", true);
            }
        }

        public void LoadMap(string path)
        {
            try
            {
                string json = File.ReadAllText(path);
                using JsonDocument doc = JsonDocument.Parse(json);
                JsonElement root = doc.RootElement;
                
                _tileSize = root.GetProperty("tile_size").GetInt32();
                _gridWidth = root.GetProperty("grid_width").GetInt32();
                _gridHeight = root.GetProperty("grid_height").GetInt32();
                
                _worldData = new int[_gridHeight][];
                _collisionData = new bool[_gridHeight][];
                
                int y = 0;
                foreach (JsonElement row in root.GetProperty("world_data").EnumerateArray())
                {
                    _worldData[y] = new int[_gridWidth];
                    int x = 0;
                    foreach (JsonElement tile in row.EnumerateArray())
                    {
                        _worldData[y][x] = tile.GetInt32();
                        x++;
                    }
                    y++;
                }
                
                y = 0;
                foreach (JsonElement row in root.GetProperty("collision_data").EnumerateArray())
                {
                    _collisionData[y] = new bool[_gridWidth];
                    int x = 0;
                    foreach (JsonElement tile in row.EnumerateArray())
                    {
                        _collisionData[y][x] = tile.GetBoolean();
                        x++;
                    }
                    y++;
                }
                
                if (!string.IsNullOrEmpty(_tilesetPath))
                {
                    _tilesPerRow = _tilesetTexture.Width / _tileSize;
                    _tilesPerColumn = _tilesetTexture.Height / _tileSize;
                }
                
                SetStatusMessage($"Map loaded: {Path.GetFileName(path)}", false);
            }
            catch (Exception ex)
            {
                SetStatusMessage($"Failed to load map: {ex.Message}", true);
            }
        }

        public void Render()
        {
            if (_statusMessageTimer > 0)
            {
                _statusMessageTimer -= 1f / 60f;
            }
            
            ImGui.Begin("Tilemap Editor", ImGuiWindowFlags.MenuBar);
            
            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("File"))
                {
                    if (ImGui.MenuItem("New Map"))
                    {
                        ImGui.OpenPopup("NewMapPopup");
                    }
                    
                    if (ImGui.MenuItem("Load Tileset"))
                    {
                        string path = ShowOpenFileDialog("png");
                        if (!string.IsNullOrEmpty(path))
                        {
                            LoadTileset(path);
                        }
                    }
                    
                    if (ImGui.MenuItem("Save Map"))
                    {
                        string path = ShowSaveFileDialog("json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            SaveMap(path);
                        }
                    }
                    
                    if (ImGui.MenuItem("Load Map"))
                    {
                        string path = ShowOpenFileDialog("json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            LoadMap(path);
                        }
                    }
                    
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Undo", "Ctrl+Z", false, _undoStack.Count > 0))
                    {
                        Undo();
                    }
                    
                    if (ImGui.MenuItem("Redo", "Ctrl+Y", false, _redoStack.Count > 0))
                    {
                        Redo();
                    }
                    
                    if (ImGui.MenuItem("Clear Map"))
                    {
                        ClearMap();
                    }
                    
                    ImGui.EndMenu();
                }
                
                ImGui.EndMenuBar();
            }
            
            if (_statusMessageTimer > 0)
            {
                if (_statusMessageIsError)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(1, 0.3f, 0.3f, 1));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new System.Numerics.Vector4(0.3f, 1, 0.3f, 1));
                }
                
                ImGui.Text(_statusMessage);
                ImGui.PopStyleColor();
                ImGui.Separator();
            }
            
            if (ImGui.BeginPopupModal("NewMapPopup", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.InputInt("Width", ref _gridWidth);
                ImGui.InputInt("Height", ref _gridHeight);
                ImGui.InputInt("Tile Size", ref _tileSize);
                
                if (ImGui.Button("Create"))
                {
                    NewMap(_gridWidth, _gridHeight, _tileSize);
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.EndPopup();
            }
            
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 320);
            
            RenderToolPanel();
            
            ImGui.NextColumn();
            
            RenderTilemapView();
            
            ImGui.Columns(1);
            
            ImGui.End();
        }

        private void RenderToolPanel()
        {
            ImGui.BeginChild("ToolPanel", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border);
            
            ImGui.Text("Tools");
            ImGui.Separator();
            
            if (ImGui.RadioButton("Draw", !_collisionMode))
            {
                _collisionMode = false;
            }
            
            if (ImGui.RadioButton("Collision", _collisionMode))
            {
                _collisionMode = true;
            }
            
            ImGui.Separator();
            
            ImGui.Text($"Grid: {_gridWidth}x{_gridHeight}");
            ImGui.Text($"Tile Size: {_tileSize}px");
            ImGui.Text($"Selected Tiles: {_selectedTiles.Count}");
            
            ImGui.Separator();
            
            ImGui.Text("Tilemap View");
            ImGui.SliderFloat("Zoom##map", ref _zoom, 0.5f, 4.0f);
            
            ImGui.Separator();
            
            if (_tilesetTexture != null)
            {
                ImGui.Text("Tileset Palette");
                ImGui.SliderFloat("Zoom##palette", ref _paletteZoom, 1.0f, 4.0f);
                ImGui.Separator();
                
                RenderTilesetPalette();
            }
            else
            {
                ImGui.Text("No tileset loaded");
                ImGui.TextWrapped("Load a tileset from File -> Load Tileset");
            }
            
            ImGui.EndChild();
        }

        private void RenderTilesetPalette()
        {
            ImGui.BeginChild("TilesetPalette", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border);
            
            if (_tilesetTexture == null)
            {
                ImGui.EndChild();
                return;
            }
            
            var drawList = ImGui.GetWindowDrawList();
            var canvasPos = ImGui.GetCursorScreenPos();
            var mousePos = ImGui.GetMousePos();
            
            int displayTileSize = (int)(_tileSize * _paletteZoom);
            
            for (int ty = 0; ty < _tilesPerColumn; ty++)
            {
                for (int tx = 0; tx < _tilesPerRow; tx++)
                {
                    System.Numerics.Vector2 tilePos = new System.Numerics.Vector2(
                        canvasPos.X + tx * displayTileSize,
                        canvasPos.Y + ty * displayTileSize
                    );
                    
                    System.Numerics.Vector2 uv0 = new System.Numerics.Vector2(
                        (float)(tx * _tileSize) / _tilesetTexture.Width,
                        (float)(ty * _tileSize) / _tilesetTexture.Height
                    );
                    
                    System.Numerics.Vector2 uv1 = new System.Numerics.Vector2(
                        (float)((tx + 1) * _tileSize) / _tilesetTexture.Width,
                        (float)((ty + 1) * _tileSize) / _tilesetTexture.Height
                    );
                    
                    drawList.AddImage(
                        _tilesetTexturePtr, 
                        tilePos, 
                        tilePos + new System.Numerics.Vector2(displayTileSize, displayTileSize), 
                        uv0, 
                        uv1
                    );
                    
                    bool isSelected = _selectedTiles.Contains(new System.Numerics.Vector2(tx, ty));
                    
                    if (isSelected)
                    {
                        drawList.AddRect(
                            tilePos, 
                            tilePos + new System.Numerics.Vector2(displayTileSize, displayTileSize), 
                            ImGui.GetColorU32(new System.Numerics.Vector4(0.3f, 0.8f, 1.0f, 1.0f)),
                            0f,
                            ImDrawFlags.None,
                            3f
                        );
                    }
                    else
                    {
                        drawList.AddRect(
                            tilePos, 
                            tilePos + new System.Numerics.Vector2(displayTileSize, displayTileSize), 
                            ImGui.GetColorU32(new System.Numerics.Vector4(0.3f, 0.3f, 0.3f, 0.5f))
                        );
                    }
                }
            }
            
            if (ImGui.IsWindowHovered())
            {
                HandlePaletteInput(canvasPos, mousePos, displayTileSize);
            }
            
            ImGui.Dummy(new System.Numerics.Vector2(_tilesPerRow * displayTileSize, _tilesPerColumn * displayTileSize));
            
            ImGui.EndChild();
        }

        private void HandlePaletteInput(System.Numerics.Vector2 canvasPos, System.Numerics.Vector2 mousePos, int displayTileSize)
        {
            int tileX = (int)((mousePos.X - canvasPos.X) / displayTileSize);
            int tileY = (int)((mousePos.Y - canvasPos.Y) / displayTileSize);
            
            if (tileX < 0 || tileX >= _tilesPerRow || tileY < 0 || tileY >= _tilesPerColumn)
                return;
            
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    _selectedTiles.Clear();
                }
                _selectionStart = new System.Numerics.Vector2(tileX, tileY);
                _isSelecting = true;
            }
            
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && _isSelecting)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    _selectedTiles.Clear();
                }
                
                int minX = (int)Math.Min(_selectionStart.X, tileX);
                int maxX = (int)Math.Max(_selectionStart.X, tileX);
                int minY = (int)Math.Min(_selectionStart.Y, tileY);
                int maxY = (int)Math.Max(_selectionStart.Y, tileY);
                
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        var tile = new System.Numerics.Vector2(x, y);
                        if (!_selectedTiles.Contains(tile))
                        {
                            _selectedTiles.Add(tile);
                        }
                    }
                }
            }
            
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                _isSelecting = false;
            }
        }

        private void RenderTilemapView()
        {
            ImGui.BeginChild("TilemapView", new System.Numerics.Vector2(0, 0), ImGuiChildFlags.Border, ImGuiWindowFlags.HorizontalScrollbar);
            
            var drawList = ImGui.GetWindowDrawList();
            var canvasPos = ImGui.GetCursorScreenPos();
            var mousePos = ImGui.GetMousePos();
            
            int displayTileSize = (int)(_tileSize * _zoom);
            
            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    System.Numerics.Vector2 tilePos = new System.Numerics.Vector2(
                        canvasPos.X + x * displayTileSize + _viewportScroll.X,
                        canvasPos.Y + y * displayTileSize + _viewportScroll.Y
                    );
                    
                    System.Numerics.Vector2 tileSize = new System.Numerics.Vector2(displayTileSize, displayTileSize);
                    
                    drawList.AddRect(tilePos, tilePos + tileSize, ImGui.GetColorU32(new System.Numerics.Vector4(0.2f, 0.2f, 0.2f, 0.5f)));
                    
                    int tileId = _worldData[y][x];
                    
                    if (tileId > 0 && _tilesetTexture != null)
                    {
                        int tileIndex = tileId - 1;
                        int tx = tileIndex % _tilesPerRow;
                        int ty = tileIndex / _tilesPerRow;
                        
                        System.Numerics.Vector2 uv0 = new System.Numerics.Vector2(
                            (float)(tx * _tileSize) / _tilesetTexture.Width,
                            (float)(ty * _tileSize) / _tilesetTexture.Height
                        );
                        
                        System.Numerics.Vector2 uv1 = new System.Numerics.Vector2(
                            (float)((tx + 1) * _tileSize) / _tilesetTexture.Width,
                            (float)((ty + 1) * _tileSize) / _tilesetTexture.Height
                        );
                        
                        drawList.AddImage(_tilesetTexturePtr, tilePos, tilePos + tileSize, uv0, uv1);
                    }
                    
                    if (_collisionData[y][x])
                    {
                        drawList.AddRectFilled(tilePos, tilePos + tileSize, 
                            ImGui.GetColorU32(new System.Numerics.Vector4(1.0f, 0.0f, 0.0f, 0.3f)));
                    }
                }
            }
            
            if (ImGui.IsWindowHovered())
            {
                HandleMapInput(canvasPos, mousePos, displayTileSize);
            }
            
            ImGui.Dummy(new System.Numerics.Vector2(_gridWidth * displayTileSize, _gridHeight * displayTileSize));
            
            ImGui.EndChild();
        }

        private void HandleMapInput(System.Numerics.Vector2 canvasPos, System.Numerics.Vector2 mousePos, int displayTileSize)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                int gridX = (int)((mousePos.X - canvasPos.X - _viewportScroll.X) / displayTileSize);
                int gridY = (int)((mousePos.Y - canvasPos.Y - _viewportScroll.Y) / displayTileSize);
                
                if (gridX >= 0 && gridX < _gridWidth && gridY >= 0 && gridY < _gridHeight)
                {
                    if (_collisionMode)
                    {
                        bool newValue = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
                        bool oldValue = _collisionData[gridY][gridX];
                        
                        if (newValue != oldValue)
                        {
                            _undoStack.Push(new EditorAction
                            {
                                X = gridX,
                                Y = gridY,
                                OldTileId = _worldData[gridY][gridX],
                                NewTileId = _worldData[gridY][gridX],
                                OldCollision = oldValue,
                                NewCollision = newValue
                            });
                            
                            _collisionData[gridY][gridX] = newValue;
                            _redoStack.Clear();
                        }
                    }
                    else
                    {
                        bool isRightClick = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
                        PlaceTiles(gridX, gridY, isRightClick);
                    }
                }
            }
            
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) || ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                int gridX = (int)((mousePos.X - canvasPos.X - _viewportScroll.X) / displayTileSize);
                int gridY = (int)((mousePos.Y - canvasPos.Y - _viewportScroll.Y) / displayTileSize);
                
                if (gridX >= 0 && gridX < _gridWidth && gridY >= 0 && gridY < _gridHeight)
                {
                    if (_collisionMode)
                    {
                        bool newValue = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
                        _collisionData[gridY][gridX] = newValue;
                    }
                    else
                    {
                        bool isRightClick = ImGui.IsMouseDragging(ImGuiMouseButton.Right);
                        PlaceTiles(gridX, gridY, isRightClick);
                    }
                }
            }
            
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
            {
                var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Middle);
                _viewportScroll.X += delta.X;
                _viewportScroll.Y += delta.Y;
                ImGui.ResetMouseDragDelta(ImGuiMouseButton.Middle);
            }
            
            float wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                _zoom += wheel * 0.1f;
                _zoom = Math.Clamp(_zoom, 0.5f, 4.0f);
            }
        }

        private void PlaceTiles(int startX, int startY, bool erase)
        {
            if (_selectedTiles.Count == 0 && !erase)
                return;
            
            if (erase)
            {
                int oldTileId = _worldData[startY][startX];
                if (oldTileId != 0)
                {
                    _undoStack.Push(new EditorAction
                    {
                        X = startX,
                        Y = startY,
                        OldTileId = oldTileId,
                        NewTileId = 0,
                        OldCollision = _collisionData[startY][startX],
                        NewCollision = _collisionData[startY][startX]
                    });
                    _worldData[startY][startX] = 0;
                }
                return;
            }
            
            int minTileX = int.MaxValue;
            int minTileY = int.MaxValue;
            int maxTileX = int.MinValue;
            int maxTileY = int.MinValue;
            
            foreach (var tile in _selectedTiles)
            {
                minTileX = Math.Min(minTileX, (int)tile.X);
                minTileY = Math.Min(minTileY, (int)tile.Y);
                maxTileX = Math.Max(maxTileX, (int)tile.X);
                maxTileY = Math.Max(maxTileY, (int)tile.Y);
            }
            
            int selectionWidth = maxTileX - minTileX + 1;
            int selectionHeight = maxTileY - minTileY + 1;
            
            for (int dy = 0; dy < selectionHeight; dy++)
            {
                for (int dx = 0; dx < selectionWidth; dx++)
                {
                    int mapX = startX + dx;
                    int mapY = startY + dy;
                    
                    if (mapX >= 0 && mapX < _gridWidth && mapY >= 0 && mapY < _gridHeight)
                    {
                        int paletteX = minTileX + dx;
                        int paletteY = minTileY + dy;
                        
                        var paletteTile = new System.Numerics.Vector2(paletteX, paletteY);
                        
                        if (_selectedTiles.Contains(paletteTile))
                        {
                            int tileId = paletteY * _tilesPerRow + paletteX + 1;
                            int oldTileId = _worldData[mapY][mapX];
                            
                            if (tileId != oldTileId)
                            {
                                _worldData[mapY][mapX] = tileId;
                            }
                        }
                    }
                }
            }
        }

        private void Undo()
        {
            if (_undoStack.Count == 0) return;
            
            var action = _undoStack.Pop();
            
            _worldData[action.Y][action.X] = action.OldTileId;
            _collisionData[action.Y][action.X] = action.OldCollision;
            
            _redoStack.Push(action);
        }

        private void Redo()
        {
            if (_redoStack.Count == 0) return;
            
            var action = _redoStack.Pop();
            
            _worldData[action.Y][action.X] = action.NewTileId;
            _collisionData[action.Y][action.X] = action.NewCollision;
            
            _undoStack.Push(action);
        }

        private void ClearMap()
        {
            for (int y = 0; y < _gridHeight; y++)
            {
                for (int x = 0; x < _gridWidth; x++)
                {
                    _worldData[y][x] = 0;
                    _collisionData[y][x] = false;
                }
            }
            
            _undoStack.Clear();
            _redoStack.Clear();
        }

        private string ShowOpenFileDialog(string extension)
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            
            string filter = extension.ToLower() == "png" ? "PNG Files\0*.png\0All Files\0*.*\0\0" : 
                           extension.ToLower() == "json" ? "JSON Files\0*.json\0All Files\0*.*\0\0" : 
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

        private string ShowSaveFileDialog(string extension)
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            
            string filter = extension.ToLower() == "json" ? "JSON Files\0*.json\0All Files\0*.*\0\0" : 
                           "All Files\0*.*\0\0";
            
            ofn.filter = filter;
            ofn.file = new string(new char[256]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = Directory.GetCurrentDirectory();
            ofn.title = "Save File";
            ofn.defExt = extension;
            ofn.flags = 0x00000002 | 0x00000004;

            if (GetSaveFileName(ofn))
            {
                return ofn.file;
            }
            
            return null;
        }

        private void SetStatusMessage(string message, bool isError)
        {
            _statusMessage = message;
            _statusMessageTimer = 3.0f;
            _statusMessageIsError = isError;
        }

        private class EditorAction
        {
            public int X { get; set; }
            public int Y { get; set; }
            public int OldTileId { get; set; }
            public int NewTileId { get; set; }
            public bool OldCollision { get; set; }
            public bool NewCollision { get; set; }
        }
    }
}
