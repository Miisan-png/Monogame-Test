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
        
        private bool _showViewportGuide = true;
        private bool _showGrid = true;
        private bool _showPlayerSpawn = true;
        private Vector2 _playerSpawnPosition = new Vector2(160, 90);
        private int _gameViewportWidth = 320;
        private int _gameViewportHeight = 180;
        
        private bool _isDraggingPlayer = false;
        private Vector2 _dragOffset;
        
        private Action<Vector2> _onPlayerSpawnChanged;

        public bool IsOpen { get; set; } = true;

        public LevelEditorView(TilemapEditorOverlay tilemapOverlay, GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _tilemapOverlay = tilemapOverlay;
            _graphicsDevice = graphicsDevice;
            _imGuiRenderer = imGuiRenderer;
        }

        public void SetPlayerSpawnChangedCallback(Action<Vector2> callback)
        {
            _onPlayerSpawnChanged = callback;
        }

        public void LoadSceneData(Vector2 playerSpawn)
        {
            _playerSpawnPosition = playerSpawn;
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
                
                if (_showViewportGuide)
                {
                    RenderViewportGuide(drawList, canvasPos, contentRegion);
                }
                
                if (_showPlayerSpawn)
                {
                    RenderPlayerSpawnIndicator(drawList, canvasPos, contentRegion);
                }
                
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
            if (ImGui.Button("Center on Viewport"))
            {
                CenterOnViewport();
            }

            ImGui.SameLine();
            if (ImGui.Button("Center on Player"))
            {
                CenterOnPlayer();
            }

            ImGui.SameLine();
            ImGui.Checkbox("Show Viewport", ref _showViewportGuide);

            ImGui.SameLine();
            ImGui.Checkbox("Show Player", ref _showPlayerSpawn);

            ImGui.SameLine();
            ImGui.Checkbox("Show Grid", ref _showGrid);

            ImGui.SameLine();
            ImGui.Text($"| Selected: {_tilemapOverlay.SelectedTiles.Count}");

            ImGui.SameLine();
            string modeText = _tilemapOverlay.SpikeMode ? "Spike" : _tilemapOverlay.CollisionMode ? "Collision" : "Draw";
            ImGui.Text($"| Mode: {modeText}");

            ImGui.Separator();
            
            ImGui.Text($"Camera: ({_cameraPosition.X:F0}, {_cameraPosition.Y:F0}) | Viewport: {_gameViewportWidth}x{_gameViewportHeight} | Player: ({_playerSpawnPosition.X:F0}, {_playerSpawnPosition.Y:F0})");
            
            ImGui.Separator();
        }

        private void CenterOnViewport()
        {
            _cameraPosition = new Vector2(
                _gameViewportWidth / 2 - (_gameViewportWidth / 2) / _zoom,
                _gameViewportHeight / 2 - (_gameViewportHeight / 2) / _zoom
            );
        }

        private void CenterOnPlayer()
        {
            _cameraPosition = new Vector2(
                _playerSpawnPosition.X - (_gameViewportWidth / 2) / _zoom,
                _playerSpawnPosition.Y - (_gameViewportHeight / 2) / _zoom
            );
        }

        private void RenderViewportGuide(ImDrawListPtr drawList, Vector2 canvasPos, Vector2 canvasSize)
        {
            float tileSize = _tilemapOverlay.TileSize * _zoom;
            Vector2 viewOffset = canvasPos + canvasSize / 2 - _cameraPosition * _zoom;

            Vector2 viewportTopLeft = viewOffset;
            Vector2 viewportSize = new Vector2(_gameViewportWidth * _zoom, _gameViewportHeight * _zoom);

            uint darkOverlay = ImGui.GetColorU32(new Vector4(0.0f, 0.0f, 0.0f, 0.6f));
            
            if (viewportTopLeft.Y > canvasPos.Y)
            {
                drawList.AddRectFilled(
                    canvasPos,
                    new Vector2(canvasPos.X + canvasSize.X, viewportTopLeft.Y),
                    darkOverlay
                );
            }
            
            if (viewportTopLeft.Y + viewportSize.Y < canvasPos.Y + canvasSize.Y)
            {
                drawList.AddRectFilled(
                    new Vector2(canvasPos.X, viewportTopLeft.Y + viewportSize.Y),
                    new Vector2(canvasPos.X + canvasSize.X, canvasPos.Y + canvasSize.Y),
                    darkOverlay
                );
            }
            
            if (viewportTopLeft.X > canvasPos.X)
            {
                drawList.AddRectFilled(
                    new Vector2(canvasPos.X, Math.Max(viewportTopLeft.Y, canvasPos.Y)),
                    new Vector2(viewportTopLeft.X, Math.Min(viewportTopLeft.Y + viewportSize.Y, canvasPos.Y + canvasSize.Y)),
                    darkOverlay
                );
            }
            
            if (viewportTopLeft.X + viewportSize.X < canvasPos.X + canvasSize.X)
            {
                drawList.AddRectFilled(
                    new Vector2(viewportTopLeft.X + viewportSize.X, Math.Max(viewportTopLeft.Y, canvasPos.Y)),
                    new Vector2(canvasPos.X + canvasSize.X, Math.Min(viewportTopLeft.Y + viewportSize.Y, canvasPos.Y + canvasSize.Y)),
                    darkOverlay
                );
            }

            uint viewportColor = ImGui.GetColorU32(new Vector4(0.2f, 0.8f, 1.0f, 1.0f));
            drawList.AddRect(
                viewportTopLeft,
                viewportTopLeft + viewportSize,
                viewportColor,
                0f,
                ImDrawFlags.None,
                4f
            );

            float cornerSize = 20f;
            
            drawList.AddLine(viewportTopLeft, new Vector2(viewportTopLeft.X + cornerSize, viewportTopLeft.Y), viewportColor, 4f);
            drawList.AddLine(viewportTopLeft, new Vector2(viewportTopLeft.X, viewportTopLeft.Y + cornerSize), viewportColor, 4f);
            
            Vector2 topRight = new Vector2(viewportTopLeft.X + viewportSize.X, viewportTopLeft.Y);
            drawList.AddLine(topRight, new Vector2(topRight.X - cornerSize, topRight.Y), viewportColor, 4f);
            drawList.AddLine(topRight, new Vector2(topRight.X, topRight.Y + cornerSize), viewportColor, 4f);
            
            Vector2 bottomLeft = new Vector2(viewportTopLeft.X, viewportTopLeft.Y + viewportSize.Y);
            drawList.AddLine(bottomLeft, new Vector2(bottomLeft.X + cornerSize, bottomLeft.Y), viewportColor, 4f);
            drawList.AddLine(bottomLeft, new Vector2(bottomLeft.X, bottomLeft.Y - cornerSize), viewportColor, 4f);
            
            Vector2 bottomRight = viewportTopLeft + viewportSize;
            drawList.AddLine(bottomRight, new Vector2(bottomRight.X - cornerSize, bottomRight.Y), viewportColor, 4f);
            drawList.AddLine(bottomRight, new Vector2(bottomRight.X, bottomRight.Y - cornerSize), viewportColor, 4f);

            Vector2 labelPos = new Vector2(viewportTopLeft.X + 10, viewportTopLeft.Y + 10);
            drawList.AddText(labelPos, viewportColor, $"CAMERA VIEW [{_gameViewportWidth}x{_gameViewportHeight}]");
        }

        private void RenderPlayerSpawnIndicator(ImDrawListPtr drawList, Vector2 canvasPos, Vector2 canvasSize)
        {
            Vector2 viewOffset = canvasPos + canvasSize / 2 - _cameraPosition * _zoom;

            Vector2 playerScreenPos = new Vector2(
                viewOffset.X + _playerSpawnPosition.X * _zoom,
                viewOffset.Y + _playerSpawnPosition.Y * _zoom
            );

            float playerWidth = 16 * _zoom;
            float playerHeight = 24 * _zoom;
            
            var mousePos = ImGui.GetMousePos();
            bool isHovering = mousePos.X >= playerScreenPos.X && mousePos.X <= playerScreenPos.X + playerWidth &&
                             mousePos.Y >= playerScreenPos.Y && mousePos.Y <= playerScreenPos.Y + playerHeight;

            uint playerColor = isHovering ? 
                ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 0.4f, 1.0f)) : 
                ImGui.GetColorU32(new Vector4(1.0f, 0.7f, 0.2f, 1.0f));
            
            float time = (float)ImGui.GetTime();
            float pulse = (float)Math.Sin(time * 3.0) * 0.3f + 0.7f;
            
            for (int i = 0; i < 3; i++)
            {
                float expand = (i + 1) * 3f;
                float alpha = (1f - (i / 3f)) * 0.4f * pulse;
                uint glowColor = ImGui.GetColorU32(new Vector4(1f, 0.7f, 0.2f, alpha));
                
                drawList.AddRect(
                    new Vector2(playerScreenPos.X - expand, playerScreenPos.Y - expand),
                    new Vector2(playerScreenPos.X + playerWidth + expand, playerScreenPos.Y + playerHeight + expand),
                    glowColor,
                    0f,
                    ImDrawFlags.None,
                    3f
                );
            }
            
            float thickness = isHovering ? 5f : 4f;
            drawList.AddRect(
                playerScreenPos,
                new Vector2(playerScreenPos.X + playerWidth, playerScreenPos.Y + playerHeight),
                playerColor,
                0f,
                ImDrawFlags.None,
                thickness
            );

            if (isHovering || _isDraggingPlayer)
            {
                float handleSize = 8f;
                uint handleColor = ImGui.GetColorU32(new Vector4(0.2f, 1.0f, 1.0f, 1.0f));
                
                drawList.AddRectFilled(
                    new Vector2(playerScreenPos.X - handleSize/2, playerScreenPos.Y - handleSize/2),
                    new Vector2(playerScreenPos.X + handleSize/2, playerScreenPos.Y + handleSize/2),
                    handleColor
                );
                
                drawList.AddRectFilled(
                    new Vector2(playerScreenPos.X + playerWidth - handleSize/2, playerScreenPos.Y - handleSize/2),
                    new Vector2(playerScreenPos.X + playerWidth + handleSize/2, playerScreenPos.Y + handleSize/2),
                    handleColor
                );
                
                drawList.AddRectFilled(
                    new Vector2(playerScreenPos.X - handleSize/2, playerScreenPos.Y + playerHeight - handleSize/2),
                    new Vector2(playerScreenPos.X + handleSize/2, playerScreenPos.Y + playerHeight + handleSize/2),
                    handleColor
                );
                
                drawList.AddRectFilled(
                    new Vector2(playerScreenPos.X + playerWidth - handleSize/2, playerScreenPos.Y + playerHeight - handleSize/2),
                    new Vector2(playerScreenPos.X + playerWidth + handleSize/2, playerScreenPos.Y + playerHeight + handleSize/2),
                    handleColor
                );
            }

            Vector2 labelPos = new Vector2(playerScreenPos.X - 5, playerScreenPos.Y - 20);
            drawList.AddText(labelPos, playerColor, "PLAYER SPAWN");
            
            float crossSize = 8f;
            Vector2 center = new Vector2(playerScreenPos.X + playerWidth / 2, playerScreenPos.Y + playerHeight / 2);
            drawList.AddLine(new Vector2(center.X - crossSize, center.Y), new Vector2(center.X + crossSize, center.Y), playerColor, 2f);
            drawList.AddLine(new Vector2(center.X, center.Y - crossSize), new Vector2(center.X, center.Y + crossSize), playerColor, 2f);
            
            if (ImGui.IsWindowHovered() && isHovering && ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                _isDraggingPlayer = true;
                _dragOffset = new Vector2(mousePos.X - playerScreenPos.X, mousePos.Y - playerScreenPos.Y);
            }

            if (_isDraggingPlayer && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                Vector2 newScreenPos = new Vector2(mousePos.X - _dragOffset.X, mousePos.Y - _dragOffset.Y);
                
                _playerSpawnPosition = new Vector2(
                    (newScreenPos.X - viewOffset.X) / _zoom,
                    (newScreenPos.Y - viewOffset.Y) / _zoom
                );
                
                if (ImGui.GetIO().KeyShift)
                {
                    float snapSize = _tilemapOverlay.TileSize;
                    _playerSpawnPosition.X = (float)Math.Round(_playerSpawnPosition.X / snapSize) * snapSize;
                    _playerSpawnPosition.Y = (float)Math.Round(_playerSpawnPosition.Y / snapSize) * snapSize;
                }
            }

            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                if (_isDraggingPlayer)
                {
                    SavePlayerSpawnToScene();
                }
                _isDraggingPlayer = false;
            }
        }

        private void SavePlayerSpawnToScene()
        {
            _onPlayerSpawnChanged?.Invoke(_playerSpawnPosition);
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
            Vector2 viewOffset = canvasPos + canvasSize / 2 - _cameraPosition * _zoom;

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

                    if (_showGrid)
                    {
                        drawList.AddRect(tileScreenPos, tileScreenPos + tileSizeVec,
                            ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f)), 0f, ImDrawFlags.None, 1f);
                    }

                    if (_tilemapOverlay.ShowCollisionOverlay && _tilemapOverlay.GetCollisionAt(x, y))
                    {
                        drawList.AddRectFilled(tileScreenPos, tileScreenPos + tileSizeVec,
                            ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 0.3f)));
                    }

                    if (_tilemapOverlay.ShowSpikeOverlay && _tilemapOverlay.GetSpikeAt(x, y))
                    {
                        drawList.AddRectFilled(tileScreenPos, tileScreenPos + tileSizeVec,
                            ImGui.GetColorU32(new Vector4(1.0f, 0.5f, 0.0f, 0.5f)));
                    }
                }
            }

            bool mouseInCanvas = mousePos.X >= canvasPos.X && mousePos.X <= canvasPos.X + canvasSize.X &&
                                mousePos.Y >= canvasPos.Y && mousePos.Y <= canvasPos.Y + canvasSize.Y;
            
            if (mouseInCanvas && ImGui.IsWindowFocused() && !_isDraggingPlayer)
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
                _cameraPosition = _panStart - new Vector2(delta.X / _zoom, delta.Y / _zoom);
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

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseDragging(ImGuiMouseButton.Left))
            {
                if (_tilemapOverlay.SpikeMode)
                {
                    _tilemapOverlay.SetSpikeAt(gridX, gridY, true);
                }
                else if (_tilemapOverlay.CollisionMode)
                {
                    _tilemapOverlay.SetCollisionAt(gridX, gridY, true);
                }
                else
                {
                    PlaceTiles(gridX, gridY, false);
                }
            }

            if (ImGui.IsMouseClicked(ImGuiMouseButton.Right) || ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                if (_tilemapOverlay.SpikeMode)
                {
                    _tilemapOverlay.SetSpikeAt(gridX, gridY, false);
                }
                else if (_tilemapOverlay.CollisionMode)
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

            if (selectedTiles.Count == 0 && !erase)
            {
                return;
            }

            if (erase)
            {
                if (_tilemapOverlay.GetTileAt(startX, startY) != 0)
                {
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
                            _tilemapOverlay.SetTileAt(mapX, mapY, tileId);
                        }
                    }
                }
            }
        }
    }
}
