using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using Snow.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Snow.Editor
{
    public class TilemapEditorOverlay
    {
        private GraphicsDevice _graphicsDevice;
        private MonoGame.ImGuiNet.ImGuiRenderer _imGuiRenderer;
        
        private TilemapData _data;
        
        private Texture2D _tilesetTexture;
        private IntPtr _tilesetTexturePtr = IntPtr.Zero;
        private int _tilesPerRow;
        private int _tilesPerColumn;
        
        private List<Vector2> _selectedTiles = new List<Vector2>();
        private bool _collisionMode = false;
        private bool _spikeMode = false;
        
        private string _currentMapPath = "";
        private bool _hasUnsavedChanges = false;
        private float _autoSaveTimer = 0f;
        private const float AUTO_SAVE_DELAY = 1.0f;
        private Action<string> _onMapChanged;
        
        private bool _showGrid = true;
        private bool _showCollisionOverlay = true;
        private bool _showSpikeOverlay = true;

        public TilemapEditorOverlay(GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _graphicsDevice = graphicsDevice;
            _imGuiRenderer = imGuiRenderer;
            _data = TilemapFormat.CreateNew(16, 40, 23);
        }

        public void SetMapChangedCallback(Action<string> callback)
        {
            _onMapChanged = callback;
        }

        public void Update(float deltaTime)
        {
            if (_hasUnsavedChanges && _autoSaveTimer > 0)
            {
                _autoSaveTimer -= deltaTime;
                if (_autoSaveTimer <= 0 && !string.IsNullOrEmpty(_currentMapPath))
                {
                    SaveMap(_currentMapPath);
                }
            }
        }

        public void NewMap(int tileSize, int gridWidth, int gridHeight)
        {
            _data = TilemapFormat.CreateNew(tileSize, gridWidth, gridHeight);
            
            if (_tilesetTexture != null)
            {
                _tilesPerRow = _tilesetTexture.Width / _data.TileSize;
                _tilesPerColumn = _tilesetTexture.Height / _data.TileSize;
            }
            
            _hasUnsavedChanges = true;
            _autoSaveTimer = AUTO_SAVE_DELAY;
            
            System.Console.WriteLine($"[TilemapEditor] New map created: {gridWidth}x{gridHeight}");
        }

        public void LoadMap(string path)
        {
            try
            {
                var loadedData = TilemapFormat.Load(path);
                if (loadedData != null)
                {
                    _data = loadedData;
                    _currentMapPath = path;
                    _hasUnsavedChanges = false;
                    _autoSaveTimer = 0f;

                    if (_tilesetTexture != null)
                    {
                        _tilesPerRow = _tilesetTexture.Width / _data.TileSize;
                        _tilesPerColumn = _tilesetTexture.Height / _data.TileSize;
                    }

                    System.Console.WriteLine($"[TilemapEditor] Map loaded: {Path.GetFileName(path)}");
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[TilemapEditor] Error loading map: {ex.Message}");
            }
        }

        public void SaveMap(string path)
        {
            try
            {
                TilemapFormat.Save(path, _data);

                _currentMapPath = path;
                _hasUnsavedChanges = false;
                _autoSaveTimer = 0f;

                _onMapChanged?.Invoke(path);

                System.Console.WriteLine($"[TilemapEditor] Map saved: {Path.GetFileName(path)}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[TilemapEditor] Failed to save map: {ex.Message}");
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
                        _tilesetTexturePtr = IntPtr.Zero;
                    }
                }
                
                using (FileStream stream = new FileStream(path, FileMode.Open))
                {
                    Texture2D tempTexture = Texture2D.FromStream(_graphicsDevice, stream);
                    
                    _tilesetTexture = new Texture2D(
                        _graphicsDevice,
                        tempTexture.Width,
                        tempTexture.Height,
                        false,
                        SurfaceFormat.Color
                    );
                    
                    var data = new Microsoft.Xna.Framework.Color[tempTexture.Width * tempTexture.Height];
                    tempTexture.GetData(data);
                    _tilesetTexture.SetData(data);
                    
                    tempTexture.Dispose();
                }
                
                _tilesPerRow = _tilesetTexture.Width / _data.TileSize;
                _tilesPerColumn = _tilesetTexture.Height / _data.TileSize;
                
                _tilesetTexturePtr = _imGuiRenderer.BindTexture(_tilesetTexture);
                
                System.Console.WriteLine($"[TilemapEditor] Tileset loaded: {Path.GetFileName(path)}");
                System.Console.WriteLine($"[TilemapEditor] Tiles: {_tilesPerRow}x{_tilesPerColumn}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[TilemapEditor] Failed to load tileset: {ex.Message}");
            }
        }

        public void RenderOverlay(Vector2 scenePos, Vector2 sceneSize, float sceneZoom)
        {
            if (_tilesetTexture == null) return;

            var drawList = ImGui.GetWindowDrawList();
            var mousePos = ImGui.GetMousePos();
            
            float gameWidth = 320f;
            float gameHeight = 180f;
            float scaleX = sceneSize.X / gameWidth;
            float scaleY = sceneSize.Y / gameHeight;
            
            float displayTileSize = _data.TileSize * scaleX;
            
            for (int y = 0; y < _data.GridHeight; y++)
            {
                for (int x = 0; x < _data.GridWidth; x++)
                {
                    Vector2 tileScreenPos = new Vector2(
                        scenePos.X + x * displayTileSize,
                        scenePos.Y + y * displayTileSize
                    );
                    
                    Vector2 tileSizeVec = new Vector2(displayTileSize, displayTileSize);
                    
                    if (_showGrid)
                    {
                        drawList.AddRect(tileScreenPos, tileScreenPos + tileSizeVec, 
                            ImGui.GetColorU32(new Vector4(0f, 1f, 1f, 0.3f)), 0f, ImDrawFlags.None, 1f);
                    }
                    
                    if (_showCollisionOverlay && _data.Collision[y][x])
                    {
                        drawList.AddRectFilled(tileScreenPos, tileScreenPos + tileSizeVec, 
                            ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 0.4f)));
                    }

                    if (_showSpikeOverlay && _data.Spikes[y][x])
                    {
                        drawList.AddRectFilled(tileScreenPos, tileScreenPos + tileSizeVec, 
                            ImGui.GetColorU32(new Vector4(1.0f, 0.5f, 0.0f, 0.5f)));
                    }
                }
            }
            
            if (ImGui.IsWindowHovered() && !ImGui.GetIO().WantCaptureMouse)
            {
                HandleTileInput(scenePos, mousePos, displayTileSize);
            }
        }

        private void HandleTileInput(Vector2 scenePos, Vector2 mousePos, float displayTileSize)
        {
            int gridX = (int)((mousePos.X - scenePos.X) / displayTileSize);
            int gridY = (int)((mousePos.Y - scenePos.Y) / displayTileSize);
            
            if (gridX < 0 || gridX >= _data.GridWidth || gridY < 0 || gridY >= _data.GridHeight)
                return;
            
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (_spikeMode)
                {
                    SetSpikeTileInternal(gridX, gridY, true);
                }
                else if (_collisionMode)
                {
                    SetCollisionTileInternal(gridX, gridY, true);
                }
                else
                {
                    PlaceTilesInternal(gridX, gridY, false);
                }
            }
            
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) || ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                if (_spikeMode)
                {
                    SetSpikeTileInternal(gridX, gridY, false);
                }
                else if (_collisionMode)
                {
                    SetCollisionTileInternal(gridX, gridY, false);
                }
                else
                {
                    PlaceTilesInternal(gridX, gridY, true);
                }
            }
        }

        internal int GetTileAt(int x, int y)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
                return _data.Tiles[y][x];
            return 0;
        }

        internal void SetTileAt(int x, int y, int tileId)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
            {
                if (_data.Tiles[y][x] != tileId)
                {
                    _data.Tiles[y][x] = tileId;
                    MarkAsChanged();
                }
            }
        }

        internal bool GetCollisionAt(int x, int y)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
                return _data.Collision[y][x];
            return false;
        }

        internal void SetCollisionAt(int x, int y, bool value)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
            {
                if (_data.Collision[y][x] != value)
                {
                    _data.Collision[y][x] = value;
                    MarkAsChanged();
                }
            }
        }

        internal bool GetSpikeAt(int x, int y)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
                return _data.Spikes[y][x];
            return false;
        }

        internal void SetSpikeAt(int x, int y, bool value)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
            {
                if (_data.Spikes[y][x] != value)
                {
                    _data.Spikes[y][x] = value;
                    MarkAsChanged();
                }
            }
        }

        private void PlaceTilesInternal(int startX, int startY, bool erase)
        {
            if (_selectedTiles.Count == 0 && !erase)
                return;
            
            if (erase)
            {
                if (_data.Tiles[startY][startX] != 0)
                {
                    _data.Tiles[startY][startX] = 0;
                    MarkAsChanged();
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
                    
                    if (mapX >= 0 && mapX < _data.GridWidth && mapY >= 0 && mapY < _data.GridHeight)
                    {
                        int paletteX = minTileX + dx;
                        int paletteY = minTileY + dy;
                        
                        var paletteTile = new Vector2(paletteX, paletteY);
                        
                        if (_selectedTiles.Contains(paletteTile))
                        {
                            int tileId = paletteY * _tilesPerRow + paletteX + 1;
                            int oldTileId = _data.Tiles[mapY][mapX];
                            
                            if (tileId != oldTileId)
                            {
                                _data.Tiles[mapY][mapX] = tileId;
                                MarkAsChanged();
                            }
                        }
                    }
                }
            }
        }

        private void SetCollisionTileInternal(int x, int y, bool value)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
            {
                if (_data.Collision[y][x] != value)
                {
                    _data.Collision[y][x] = value;
                    MarkAsChanged();
                }
            }
        }

        private void SetSpikeTileInternal(int x, int y, bool value)
        {
            if (x >= 0 && x < _data.GridWidth && y >= 0 && y < _data.GridHeight)
            {
                if (_data.Spikes[y][x] != value)
                {
                    _data.Spikes[y][x] = value;
                    MarkAsChanged();
                }
            }
        }

        private void MarkAsChanged()
        {
            _hasUnsavedChanges = true;
            _autoSaveTimer = AUTO_SAVE_DELAY;
        }

        public bool CollisionMode { get => _collisionMode; set => _collisionMode = value; }
        public bool SpikeMode { get => _spikeMode; set => _spikeMode = value; }
        public bool ShowGrid { get => _showGrid; set => _showGrid = value; }
        public bool ShowCollisionOverlay { get => _showCollisionOverlay; set => _showCollisionOverlay = value; }
        public bool ShowSpikeOverlay { get => _showSpikeOverlay; set => _showSpikeOverlay = value; }
        public List<Vector2> SelectedTiles => _selectedTiles;
        public Texture2D TilesetTexture => _tilesetTexture;
        public IntPtr TilesetTexturePtr => _tilesetTexturePtr;
        public int TilesPerRow => _tilesPerRow;
        public int TilesPerColumn => _tilesPerColumn;
        public int TileSize => _data?.TileSize ?? 16;
        public int GridWidth => _data?.GridWidth ?? 40;
        public int GridHeight => _data?.GridHeight ?? 23;
        public bool HasUnsavedChanges => _hasUnsavedChanges;
        public float AutoSaveTimer => _autoSaveTimer;
    }
}
