using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;

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
                ImGui.Image(_gameTexturePtr, new System.Numerics.Vector2(displayWidth, displayHeight), 
                    System.Numerics.Vector2.Zero, System.Numerics.Vector2.One);

                if (TilemapEditMode)
                {
                    _tilemapOverlay.RenderOverlay(imagePos, new System.Numerics.Vector2(displayWidth, displayHeight), _zoom);
                }

                HandleInput(canvasPos, contentRegion);
            }

            ImGui.End();
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