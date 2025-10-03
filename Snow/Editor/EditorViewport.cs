using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

namespace Snow.Editor
{
    public class EditorViewport
    {
        private GameRenderer _gameRenderer;
        private IntPtr _gameTexturePtr;
        private bool _isPlaying;
        private float _zoom = 1.0f;
        private bool _fitToWindow = true;

        public bool IsOpen { get; set; } = true;

        public EditorViewport(GameRenderer gameRenderer)
        {
            _gameRenderer = gameRenderer;
            _isPlaying = false;
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
            ImGui.Begin("Game Viewport", ref isOpen, ImGuiWindowFlags.NoScrollbar | ImGuiWindowFlags.NoScrollWithMouse);
            IsOpen = isOpen;
            
            ImGui.PopStyleVar();

            if (ImGui.Button(_isPlaying ? "Pause" : "Play"))
            {
                _isPlaying = !_isPlaying;
                _gameRenderer.IsPaused = !_isPlaying;
            }

            ImGui.SameLine();

            if (ImGui.Button("Stop"))
            {
                _isPlaying = false;
                _gameRenderer.IsPaused = true;
            }

            ImGui.SameLine();
            
            if (ImGui.Button("Reload Level"))
            {
                _gameRenderer.ReloadLevel();
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

            var contentRegion = ImGui.GetContentRegionAvail();
            
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

                float offsetX = (contentRegion.X - displayWidth) * 0.5f;
                float offsetY = (contentRegion.Y - displayHeight) * 0.5f;

                var cursorPos = ImGui.GetCursorPos();
                ImGui.SetCursorPos(new System.Numerics.Vector2(cursorPos.X + offsetX, cursorPos.Y + offsetY));

                ImGui.Image(_gameTexturePtr, new System.Numerics.Vector2(displayWidth, displayHeight), 
                    System.Numerics.Vector2.Zero, System.Numerics.Vector2.One);
            }

            ImGui.End();
        }
    }
}
