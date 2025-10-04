using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Numerics;

namespace Snow.Editor
{
    public class LevelEditorView
    {
        private TilemapEditorOverlay _tilemapOverlay;
        private GraphicsDevice _graphicsDevice;
        private MonoGame.ImGuiNet.ImGuiRenderer _imGuiRenderer;
        
        private Vector2 _cameraPosition = Vector2.Zero;
        private float _zoom = 2.0f;
        private bool _isPanning = false;
        private Vector2 _panStart;

        public bool IsOpen { get; set; } = true;

        public LevelEditorView(TilemapEditorOverlay tilemapOverlay, GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _tilemapOverlay = tilemapOverlay;
            _graphicsDevice = graphicsDevice;
            _imGuiRenderer = imGuiRenderer;
        }

        public void Render()
        {
            if (!IsOpen) return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, Vector2.Zero);
            
            bool isOpen = IsOpen;
            ImGui.Begin("Level Editor", ref isOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            IsOpen = isOpen;
            
            ImGui.PopStyleVar();

            RenderToolbar();

            var contentRegion = ImGui.GetContentRegionAvail();
            var canvasPos = ImGui.GetCursorScreenPos();
            var mousePos = ImGui.GetMousePos();

            if (contentRegion.X > 0 && contentRegion.Y > 0)
            {
                var drawList = ImGui.GetWindowDrawList();
                
                drawList.AddRectFilled(canvasPos, 
                    new Vector2(canvasPos.X + contentRegion.X, canvasPos.Y + contentRegion.Y),
                    ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.12f, 1.0f)));

                RenderTilemap(drawList, canvasPos, contentRegion, mousePos);
                
                HandleInput(canvasPos, contentRegion);

                ImGui.Dummy(contentRegion);
            }

            ImGui.End();
        }

        private void RenderToolbar()
        {
            ImGui.Text($"Zoom: {_zoom:F1}x");
            
            ImGui.SameLine();
            if (ImGui.Button("Reset View"))
            {
                _cameraPosition = Vector2.Zero;
                _zoom = 2.0f;
            }

            ImGui.SameLine();
            if (ImGui.Button("Fit Map"))
            {
                _cameraPosition = Vector2.Zero;
                _zoom = 2.0f;
            }

            ImGui.SameLine();
            ImGui.Text($"| Selected Tiles: {_tilemapOverlay.SelectedTiles.Count}");

            ImGui.SameLine();
            ImGui.Text($"| Mode: {(_tilemapOverlay.CollisionMode ? "Collision" : "Draw")}");

            ImGui.Separator();
        }

        private void RenderTilemap(ImDrawListPtr drawList, Vector2 canvasPos, Vector2 canvasSize, Vector2 mousePos)
        {
            if (_tilemapOverlay.TilesetTexture == null) 
            {
                ImGui.SetCursorScreenPos(new Vector2(canvasPos.X + canvasSize.X / 2 - 100, canvasPos.Y + canvasSize.Y / 2));
                ImGui.Text("Load a tileset from Tools panel");
                return;
            }

            float tileSize = _tilemapOverlay.TileSize * _zoom;
            Vector2 viewOffset = canvasPos + canvasSize / 2 - _cameraPosition;

            for (int y = 0; y < _tilemapOverlay.GridHeight; y++)
            {
                for (int x = 0; x < _tilemapOverlay.GridWidth; x++)
                {
                    Vector2 tileScreenPos = new Vector2(
                        viewOffset.X + x * tileSize,
                        viewOffset.Y + y * tileSize
                    );

                    if (tileScreenPos.X + tileSize < canvasPos.X || tileScreenPos.X > canvasPos.X + canvasSize.X ||
                        tileScreenPos.Y + tileSize < canvasPos.Y || tileScreenPos.Y > canvasPos.Y + canvasSize.Y)
                        continue;

                    Vector2 tileSizeVec = new Vector2(tileSize, tileSize);

                    int tileId = _tilemapOverlay.GetTileAt(x, y);
                    if (tileId > 0)
                    {
                        int tileIndex = tileId - 1;
                        int tx = tileIndex % _tilemapOverlay.TilesPerRow;
                        int ty = tileIndex / _tilemapOverlay.TilesPerRow;

                        Vector2 uv0 = new Vector2(
                            (float)(tx * _tilemapOverlay.TileSize) / _tilemapOverlay.TilesetTexture.Width,
                            (float)(ty * _tilemapOverlay.TileSize) / _tilemapOverlay.TilesetTexture.Height
                        );

                        Vector2 uv1 = new Vector2(
                            (float)((tx + 1) * _tilemapOverlay.TileSize) / _tilemapOverlay.TilesetTexture.Width,
                            (float)((ty + 1) * _tilemapOverlay.TileSize) / _tilemapOverlay.TilesetTexture.Height
                        );

                        drawList.AddImage(
                            _tilemapOverlay.TilesetTexturePtr,
                            tileScreenPos,
                            tileScreenPos + tileSizeVec,
                            uv0,
                            uv1
                        );
                    }

                    if (_tilemapOverlay.ShowGrid)
                    {
                        drawList.AddRect(tileScreenPos, tileScreenPos + tileSizeVec,
                            ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f)), 0f, ImDrawFlags.None, 1f);
                    }

                    if (_tilemapOverlay.ShowCollisionOverlay && _tilemapOverlay.GetCollisionAt(x, y))
                    {
                        drawList.AddRectFilled(tileScreenPos, tileScreenPos + tileSizeVec,
                            ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 0.3f)));
                    }
                }
            }

            // Check if mouse is within the canvas bounds
            bool mouseInCanvas = mousePos.X >= canvasPos.X && mousePos.X <= canvasPos.X + canvasSize.X &&
                                mousePos.Y >= canvasPos.Y && mousePos.Y <= canvasPos.Y + canvasSize.Y;
            
            if (mouseInCanvas && ImGui.IsWindowFocused())
            {
                HandleTileInput(viewOffset, mousePos, tileSize);
            }
        }

        private void HandleInput(Vector2 canvasPos, Vector2 canvasSize)
        {
            if (!ImGui.IsWindowHovered()) return;

            if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
            {
                if (!_isPanning)
                {
                    _isPanning = true;
                    _panStart = _cameraPosition;
                }
                var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Middle);
                _cameraPosition = _panStart - new Vector2(delta.X, delta.Y);
            }
            else
            {
                _isPanning = false;
            }

            float wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                _zoom += wheel * 0.2f;
                _zoom = Math.Clamp(_zoom, 0.5f, 8.0f);
            }
        }

        private void HandleTileInput(Vector2 viewOffset, Vector2 mousePos, float tileSize)
        {
            int gridX = (int)((mousePos.X - viewOffset.X) / tileSize);
            int gridY = (int)((mousePos.Y - viewOffset.Y) / tileSize);

            if (gridX < 0 || gridX >= _tilemapOverlay.GridWidth || gridY < 0 || gridY >= _tilemapOverlay.GridHeight)
                return;

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                System.Console.WriteLine($"[LevelEditor] Mouse clicked at grid ({gridX}, {gridY})");
                System.Console.WriteLine($"[LevelEditor] Selected tiles: {_tilemapOverlay.SelectedTiles.Count}");
                System.Console.WriteLine($"[LevelEditor] Collision mode: {_tilemapOverlay.CollisionMode}");
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (_tilemapOverlay.CollisionMode)
                {
                    System.Console.WriteLine($"[LevelEditor] Setting collision at ({gridX}, {gridY})");
                    _tilemapOverlay.SetCollisionAt(gridX, gridY, true);
                }
                else
                {
                    System.Console.WriteLine($"[LevelEditor] Placing tiles at ({gridX}, {gridY})");
                    PlaceTiles(gridX, gridY, false);
                }
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) || ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                if (_tilemapOverlay.CollisionMode)
                {
                    _tilemapOverlay.SetCollisionAt(gridX, gridY, false);
                }
                else
                {
                    PlaceTiles(gridX, gridY, true);
                }
            }
        }

        private void PlaceTiles(int startX, int startY, bool erase)
        {
            var selectedTiles = _tilemapOverlay.SelectedTiles;
            
            System.Console.WriteLine($"[LevelEditor] PlaceTiles called: startX={startX}, startY={startY}, erase={erase}, selectedCount={selectedTiles.Count}");

            if (selectedTiles.Count == 0 && !erase)
            {
                System.Console.WriteLine($"[LevelEditor] No tiles selected, cannot place!");
                return;
            }

            if (erase)
            {
                if (_tilemapOverlay.GetTileAt(startX, startY) != 0)
                {
                    System.Console.WriteLine($"[LevelEditor] Erasing tile at ({startX}, {startY})");
                    _tilemapOverlay.SetTileAt(startX, startY, 0);
                }
                return;
            }

            int minTileX = int.MaxValue;
            int minTileY = int.MaxValue;
            int maxTileX = int.MinValue;
            int maxTileY = int.MinValue;

            foreach (var tile in selectedTiles)
            {
                minTileX = Math.Min(minTileX, (int)tile.X);
                minTileY = Math.Min(minTileY, (int)tile.Y);
                maxTileX = Math.Max(maxTileX, (int)tile.X);
                maxTileY = Math.Max(maxTileY, (int)tile.Y);
            }

            int selectionWidth = maxTileX - minTileX + 1;
            int selectionHeight = maxTileY - minTileY + 1;

            System.Console.WriteLine($"[LevelEditor] Selection bounds: ({minTileX},{minTileY}) to ({maxTileX},{maxTileY})");

            for (int dy = 0; dy < selectionHeight; dy++)
            {
                for (int dx = 0; dx < selectionWidth; dx++)
                {
                    int mapX = startX + dx;
                    int mapY = startY + dy;

                    if (mapX >= 0 && mapX < _tilemapOverlay.GridWidth && mapY >= 0 && mapY < _tilemapOverlay.GridHeight)
                    {
                        int paletteX = minTileX + dx;
                        int paletteY = minTileY + dy;

                        var paletteTile = new Vector2(paletteX, paletteY);

                        if (selectedTiles.Contains(paletteTile))
                        {
                            int tileId = paletteY * _tilemapOverlay.TilesPerRow + paletteX + 1;
                            System.Console.WriteLine($"[LevelEditor] Setting tile at ({mapX},{mapY}) to ID {tileId}");
                            _tilemapOverlay.SetTileAt(mapX, mapY, tileId);
                        }
                    }
                }
            }
        }
    }
}
