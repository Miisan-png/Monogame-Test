// Snow/Editor/TilemapEditor.cs
using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace Snow.Editor
{
    public class TilemapEditor
    {
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
        
        private List<Vector2> _selectedTiles = new List<Vector2>();
        private Vector2 _selectionStart = Vector2.Zero;
        private bool _isSelecting = false;
        
        private bool _collisionMode = false;
        
        private Vector2 _viewportScroll = Vector2.Zero;
        private float _zoom = 2.0f;
        private float _paletteZoom = 2.0f;
        
        private Stack<EditorAction> _undoStack = new Stack<EditorAction>();
        private Stack<EditorAction> _redoStack = new Stack<EditorAction>();
        
        private string _statusMessage = "";
        private float _statusMessageTimer = 0f;
        private bool _statusMessageIsError = false;
        
        private IntPtr _tilesetTexturePtr = IntPtr.Zero;
        private MonoGame.ImGuiNet.ImGuiRenderer _imGuiRenderer;
        
        private TilemapEditorUI _ui;
        
        private string _currentMapPath = "";
        private bool _hasUnsavedChanges = false;
        private float _autoSaveTimer = 0f;
        private const float AUTO_SAVE_DELAY = 1.0f;
        private Action<string> _onMapChanged;

        public TilemapEditor(GameRenderer gameRenderer, GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _gameRenderer = gameRenderer;
            _graphicsDevice = graphicsDevice;
            _imGuiRenderer = imGuiRenderer;
            
            InitializeGridData();
            
            _ui = new TilemapEditorUI(this);
        }

        public void SetMapChangedCallback(Action<string> callback)
        {
            _onMapChanged = callback;
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
            
            _hasUnsavedChanges = true;
            _autoSaveTimer = AUTO_SAVE_DELAY;
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
                
                _currentMapPath = path;
                _hasUnsavedChanges = false;
                _autoSaveTimer = 0f;
                
                SetStatusMessage($"Map saved: {Path.GetFileName(path)}", false);
                
                _onMapChanged?.Invoke(path);
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
                
                _currentMapPath = path;
                _hasUnsavedChanges = false;
                _autoSaveTimer = 0f;
                
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
            
            if (_hasUnsavedChanges && _autoSaveTimer > 0)
            {
                _autoSaveTimer -= 1f / 60f;
                if (_autoSaveTimer <= 0 && !string.IsNullOrEmpty(_currentMapPath))
                {
                    SaveMap(_currentMapPath);
                }
            }
            
            _ui.Render();
        }

        internal void PlaceTiles(int startX, int startY, bool erase)
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
                    
                    if (mapX >= 0 && mapX < _gridWidth && mapY >= 0 && mapY < _gridHeight)
                    {
                        int paletteX = minTileX + dx;
                        int paletteY = minTileY + dy;
                        
                        var paletteTile = new Vector2(paletteX, paletteY);
                        
                        if (_selectedTiles.Contains(paletteTile))
                        {
                            int tileId = paletteY * _tilesPerRow + paletteX + 1;
                            int oldTileId = _worldData[mapY][mapX];
                            
                            if (tileId != oldTileId)
                            {
                                _worldData[mapY][mapX] = tileId;
                                MarkAsChanged();
                            }
                        }
                    }
                }
            }
        }

        internal void SetCollisionTile(int x, int y, bool value)
        {
            if (x >= 0 && x < _gridWidth && y >= 0 && y < _gridHeight)
            {
                if (_collisionData[y][x] != value)
                {
                    _collisionData[y][x] = value;
                    MarkAsChanged();
                }
            }
        }

        private void MarkAsChanged()
        {
            _hasUnsavedChanges = true;
            _autoSaveTimer = AUTO_SAVE_DELAY;
        }

        internal void Undo()
        {
            if (_undoStack.Count == 0) return;
            
            var action = _undoStack.Pop();
            
            _worldData[action.Y][action.X] = action.OldTileId;
            _collisionData[action.Y][action.X] = action.OldCollision;
            
            _redoStack.Push(action);
            MarkAsChanged();
        }

        internal void Redo()
        {
            if (_redoStack.Count == 0) return;
            
            var action = _redoStack.Pop();
            
            _worldData[action.Y][action.X] = action.NewTileId;
            _collisionData[action.Y][action.X] = action.NewCollision;
            
            _undoStack.Push(action);
            MarkAsChanged();
        }

        internal void ClearMap()
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
            MarkAsChanged();
        }

        internal void SetStatusMessage(string message, bool isError)
        {
            _statusMessage = message;
            _statusMessageTimer = 3.0f;
            _statusMessageIsError = isError;
        }

        internal int GridWidth => _gridWidth;
        internal int GridHeight => _gridHeight;
        internal int TileSize => _tileSize;
        internal int[][] WorldData => _worldData;
        internal bool[][] CollisionData => _collisionData;
        internal Texture2D TilesetTexture => _tilesetTexture;
        internal int TilesPerRow => _tilesPerRow;
        internal int TilesPerColumn => _tilesPerColumn;
        internal List<Vector2> SelectedTiles => _selectedTiles;
        internal Vector2 SelectionStart { get => _selectionStart; set => _selectionStart = value; }
        internal bool IsSelecting { get => _isSelecting; set => _isSelecting = value; }
        internal bool CollisionMode { get => _collisionMode; set => _collisionMode = value; }
        internal Vector2 ViewportScroll { get => _viewportScroll; set => _viewportScroll = value; }
        internal float Zoom { get => _zoom; set => _zoom = value; }
        internal float PaletteZoom { get => _paletteZoom; set => _paletteZoom = value; }
        internal Stack<EditorAction> UndoStack => _undoStack;
        internal Stack<EditorAction> RedoStack => _redoStack;
        internal string StatusMessage => _statusMessage;
        internal float StatusMessageTimer => _statusMessageTimer;
        internal bool StatusMessageIsError => _statusMessageIsError;
        internal IntPtr TilesetTexturePtr => _tilesetTexturePtr;
        internal bool HasUnsavedChanges => _hasUnsavedChanges;
        internal float AutoSaveTimer => _autoSaveTimer;

        internal class EditorAction
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