using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;
namespace Snow.Editor
{
    public class SceneView
    {
        private GameRenderer _gameRenderer;
        private IntPtr _gameTexturePtr;
        private TilemapEditorOverlay _tilemapOverlay;
        
        private float _zoom = 1.0f;
        private bool _fitToWindow = true;
        private System.Numerics.Vector2 _panOffset = System.Numerics.Vector2.Zero;
        private bool _isPanning = false;
        private System.Numerics.Vector2 _panStart;
        
        private int _selectedEntityIndex = -1;
        private Snow.Engine.SceneData _currentScene;

        public bool IsOpen { get; set; } = true;
        public bool TilemapEditMode { get; set; } = false;

        public SceneView(GameRenderer gameRenderer, TilemapEditorOverlay tilemapOverlay)
        {
            _gameRenderer = gameRenderer;
            _tilemapOverlay = tilemapOverlay;
        }

        public void SetGameTexturePtr(IntPtr texturePtr)
        {
            _gameTexturePtr = texturePtr;
        }

        public void SetSelectedEntity(int entityIndex)
        {
            _selectedEntityIndex = entityIndex;
        }

        public void LoadScene(Snow.Engine.SceneData sceneData)
        {
            _currentScene = sceneData;
        }

        public void Render()
        {
            if (!IsOpen) return;

            ImGui.PushStyleVar(ImGuiStyleVar.WindowPadding, System.Numerics.Vector2.Zero);
            
            bool isOpen = IsOpen;
            ImGui.Begin("Scene View", ref isOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            IsOpen = isOpen;
            
            ImGui.PopStyleVar();

            RenderToolbar();

            var contentRegion = ImGui.GetContentRegionAvail();
            var canvasPos = ImGui.GetCursorScreenPos();
            
            if (_gameTexturePtr != IntPtr.Zero && contentRegion.X > 0 && contentRegion.Y > 0)
            {
                float textureWidth = _gameRenderer.GameRenderTarget.Width;
                float textureHeight = _gameRenderer.GameRenderTarget.Height;
                
                float displayWidth;
                float displayHeight;

                if (_fitToWindow)
                {
                    float aspectRatio = textureWidth / textureHeight;
                    displayWidth = contentRegion.X;
                    displayHeight = displayWidth / aspectRatio;
                    
                    if (displayHeight > contentRegion.Y)
                    {
                        displayHeight = contentRegion.Y;
                        displayWidth = displayHeight * aspectRatio;
                    }
                }
                else
                {
                    displayWidth = textureWidth * _zoom;
                    displayHeight = textureHeight * _zoom;
                }

                float offsetX = (contentRegion.X - displayWidth) * 0.5f + _panOffset.X;
                float offsetY = (contentRegion.Y - displayHeight) * 0.5f + _panOffset.Y;

                var cursorPos = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new System.Numerics.Vector2(cursorPos.X + offsetX, cursorPos.Y + offsetY));

                var imagePos = ImGui.GetCursorScreenPos();
                
                // Draw the game viewport
                ImGui.Image(_gameTexturePtr, new System.Numerics.Vector2(displayWidth, displayHeight), 
                    System.Numerics.Vector2.Zero, System.Numerics.Vector2.One);

                // Draw viewport outline
                var drawList = ImGui.GetWindowDrawList();
                drawList.AddRect(
                    imagePos,
                    new System.Numerics.Vector2(imagePos.X + displayWidth, imagePos.Y + displayHeight),
                    ImGui.GetColorU32(new Vector4(0.3f, 0.8f, 1f, 0.8f)),
                    0f,
                    ImDrawFlags.None,
                    2f
                );

                // Draw corner indicators
                float cornerSize = 8f;
                uint cornerColor = ImGui.GetColorU32(new Vector4(0.3f, 0.8f, 1f, 1f));
                
                // Top-left corner
                drawList.AddLine(imagePos, new System.Numerics.Vector2(imagePos.X + cornerSize, imagePos.Y), cornerColor, 3f);
                drawList.AddLine(imagePos, new System.Numerics.Vector2(imagePos.X, imagePos.Y + cornerSize), cornerColor, 3f);
                
                // Top-right corner
                var topRight = new System.Numerics.Vector2(imagePos.X + displayWidth, imagePos.Y);
                drawList.AddLine(topRight, new System.Numerics.Vector2(topRight.X - cornerSize, topRight.Y), cornerColor, 3f);
                drawList.AddLine(topRight, new System.Numerics.Vector2(topRight.X, topRight.Y + cornerSize), cornerColor, 3f);
                
                // Bottom-left corner
                var bottomLeft = new System.Numerics.Vector2(imagePos.X, imagePos.Y + displayHeight);
                drawList.AddLine(bottomLeft, new System.Numerics.Vector2(bottomLeft.X + cornerSize, bottomLeft.Y), cornerColor, 3f);
                drawList.AddLine(bottomLeft, new System.Numerics.Vector2(bottomLeft.X, bottomLeft.Y - cornerSize), cornerColor, 3f);
                
                // Bottom-right corner
                var bottomRight = new System.Numerics.Vector2(imagePos.X + displayWidth, imagePos.Y + displayHeight);
                drawList.AddLine(bottomRight, new System.Numerics.Vector2(bottomRight.X - cornerSize, bottomRight.Y), cornerColor, 3f);
                drawList.AddLine(bottomRight, new System.Numerics.Vector2(bottomRight.X, bottomRight.Y - cornerSize), cornerColor, 3f);

                // Draw selected entity highlight
                if (_selectedEntityIndex >= 0 && _currentScene != null && _selectedEntityIndex < _currentScene.Entities.Count)
                {
                    var entity = _currentScene.Entities[_selectedEntityIndex];
                    DrawEntityHighlight(drawList, entity, imagePos, displayWidth, displayHeight, textureWidth, textureHeight);
                }

                if (TilemapEditMode)
                {
                    _tilemapOverlay.RenderOverlay(imagePos, new System.Numerics.Vector2(displayWidth, displayHeight), _zoom);
                }

                HandleInput(canvasPos, contentRegion);
            }

            ImGui.End();
        }

        private void DrawEntityHighlight(ImDrawListPtr drawList, Snow.Engine.EntityData entity, 
            System.Numerics.Vector2 viewportPos, float viewportWidth, float viewportHeight, 
            float gameWidth, float gameHeight)
        {
            // Convert entity position to viewport space
            float scaleX = viewportWidth / gameWidth;
            float scaleY = viewportHeight / gameHeight;

            float entityScreenX = viewportPos.X + (entity.X * scaleX);
            float entityScreenY = viewportPos.Y + (entity.Y * scaleY);

            float width = 16f;
            float height = 24f;

            if (entity.CollisionShape != null)
            {
                width = entity.CollisionShape.Width;
                height = entity.CollisionShape.Height;
            }

            float entityWidth = width * scaleX;
            float entityHeight = height * scaleY;

            // Draw pulsing highlight box
            float time = (float)ImGui.GetTime();
            float pulse = (float)Math.Sin(time * 3.0) * 0.3f + 0.7f;
            
            var highlightColor = ImGui.GetColorU32(new Vector4(1f, 0.8f, 0.2f, pulse));
            
            // Draw outer glow
            for (int i = 0; i < 3; i++)
            {
                float expand = (i + 1) * 2f;
                float alpha = (1f - (i / 3f)) * 0.3f * pulse;
                var glowColor = ImGui.GetColorU32(new Vector4(1f, 0.8f, 0.2f, alpha));
                
                drawList.AddRect(
                    new System.Numerics.Vector2(entityScreenX - expand, entityScreenY - expand),
                    new System.Numerics.Vector2(entityScreenX + entityWidth + expand, entityScreenY + entityHeight + expand),
                    glowColor,
                    0f,
                    ImDrawFlags.None,
                    2f
                );
            }
            
            // Draw main highlight
            drawList.AddRect(
                new System.Numerics.Vector2(entityScreenX, entityScreenY),
                new System.Numerics.Vector2(entityScreenX + entityWidth, entityScreenY + entityHeight),
                highlightColor,
                0f,
                ImDrawFlags.None,
                3f
            );

            // Draw corner markers
            float cornerSize = 6f;
            
            // Top-left
            drawList.AddLine(
                new System.Numerics.Vector2(entityScreenX, entityScreenY),
                new System.Numerics.Vector2(entityScreenX + cornerSize, entityScreenY),
                highlightColor, 3f);
            drawList.AddLine(
                new System.Numerics.Vector2(entityScreenX, entityScreenY),
                new System.Numerics.Vector2(entityScreenX, entityScreenY + cornerSize),
                highlightColor, 3f);

            // Top-right
            drawList.AddLine(
                new System.Numerics.Vector2(entityScreenX + entityWidth, entityScreenY),
                new System.Numerics.Vector2(entityScreenX + entityWidth - cornerSize, entityScreenY),
                highlightColor, 3f);
            drawList.AddLine(
                new System.Numerics.Vector2(entityScreenX + entityWidth, entityScreenY),
                new System.Numerics.Vector2(entityScreenX + entityWidth, entityScreenY + cornerSize),
                highlightColor, 3f);

            // Bottom-left
            drawList.AddLine(
                new System.Numerics.Vector2(entityScreenX, entityScreenY + entityHeight),
                new System.Numerics.Vector2(entityScreenX + cornerSize, entityScreenY + entityHeight),
                highlightColor, 3f);
            drawList.AddLine(
                new System.Numerics.Vector2(entityScreenX, entityScreenY + entityHeight),
                new System.Numerics.Vector2(entityScreenX, entityScreenY + entityHeight - cornerSize),
                highlightColor, 3f);

            // Bottom-right
            drawList.AddLine(
                new System.Numerics.Vector2(entityScreenX + entityWidth, entityScreenY + entityHeight),
                new System.Numerics.Vector2(entityScreenX + entityWidth - cornerSize, entityScreenY + entityHeight),
                highlightColor, 3f);
            drawList.AddLine(
                new System.Numerics.Vector2(entityScreenX + entityWidth, entityScreenY + entityHeight),
                new System.Numerics.Vector2(entityScreenX + entityWidth, entityScreenY + entityHeight - cornerSize),
                highlightColor, 3f);

            // Draw label above entity
            string label = entity.Type;
            var labelPos = new System.Numerics.Vector2(entityScreenX, entityScreenY - 20);
            drawList.AddRectFilled(
                new System.Numerics.Vector2(labelPos.X - 4, labelPos.Y - 2),
                new System.Numerics.Vector2(labelPos.X + label.Length * 7 + 4, labelPos.Y + 14),
                ImGui.GetColorU32(new Vector4(0f, 0f, 0f, 0.7f))
            );
            drawList.AddText(labelPos, ImGui.GetColorU32(new Vector4(1f, 1f, 1f, 1f)), label);
        }

        private void RenderToolbar()
        {
            if (ImGui.Button(_gameRenderer.IsPaused ? "▶ Play" : "⏸ Pause"))
            {
                _gameRenderer.IsPaused = !_gameRenderer.IsPaused;
            }

            ImGui.SameLine();

            if (ImGui.Button("⏹ Stop"))
            {
                _gameRenderer.IsPaused = true;
            }

            ImGui.SameLine();
            
            if (ImGui.Button("🔄 Reload"))
            {
                _gameRenderer.ReloadLevel();
            }

            ImGui.SameLine();
            ImGui.Separator();
            ImGui.SameLine();

            bool tilemapMode = TilemapEditMode;
            if (ImGui.Checkbox("Tilemap Edit Mode", ref tilemapMode))
            {
                TilemapEditMode = tilemapMode;
            }

            ImGui.SameLine();
            ImGui.Separator();
            ImGui.SameLine();

            ImGui.Checkbox("Fit to Window", ref _fitToWindow);

            if (!_fitToWindow)
            {
                ImGui.SameLine();
                ImGui.SetNextItemWidth(150);
                ImGui.SliderFloat("Zoom", ref _zoom, 0.1f, 10.0f, "%.1fx");

                ImGui.SameLine();
                if (ImGui.Button("1x"))
                    _zoom = 1.0f;
                ImGui.SameLine();
                if (ImGui.Button("2x"))
                    _zoom = 2.0f;
                ImGui.SameLine();
                if (ImGui.Button("4x"))
                    _zoom = 4.0f;
            }

            ImGui.Separator();
        }

        private void HandleInput(System.Numerics.Vector2 canvasPos, System.Numerics.Vector2 canvasSize)
        {
            if (!ImGui.IsWindowHovered()) return;

            if (!TilemapEditMode)
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
                {
                    if (!_isPanning)
                    {
                        _isPanning = true;
                        _panStart = _panOffset;
                    }
                    var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Middle);
                    _panOffset = _panStart + new System.Numerics.Vector2(delta.X, delta.Y);
                }
                else
                {
                    _isPanning = false;
                }

                if (!_fitToWindow)
                {
                    float wheel = ImGui.GetIO().MouseWheel;
                    if (wheel != 0)
                    {
                        _zoom += wheel * 0.2f;
                        _zoom = Math.Clamp(_zoom, 0.1f, 10.0f);
                    }
                }
            }
        }
    }
}
