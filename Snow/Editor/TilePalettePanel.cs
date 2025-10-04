using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.Numerics;

namespace Snow.Editor
{
    public class TilePalettePanel
    {
        private TilemapEditorOverlay _overlay;
        private float _paletteZoom = 2.0f;
        private Vector2 _selectionStart = Vector2.Zero;
        private bool _isSelecting = false;

        public bool IsOpen { get; set; } = true;

        public TilePalettePanel(TilemapEditorOverlay overlay)
        {
            _overlay = overlay;
        }

        public void Render()
        {
            if (!IsOpen) return;

            bool isOpen = IsOpen;
            ImGui.Begin("Tile Palette", ref isOpen);
            IsOpen = isOpen;

            if (_overlay.TilesetTexture == null)
            {
                ImGui.Text("No tileset loaded");
                ImGui.TextWrapped("Load a tileset from Tools -> Load Tileset");
                ImGui.End();
                return;
            }

            ImGui.Text("Tileset Palette");
            ImGui.SliderFloat("Zoom", ref _paletteZoom, 1.0f, 4.0f);
            ImGui.Separator();

            RenderPalette();

            ImGui.End();
        }

        private void RenderPalette()
        {
            ImGui.BeginChild("PaletteContent", new Vector2(0, 0), ImGuiChildFlags.Border);
            
            var drawList = ImGui.GetWindowDrawList();
            var canvasPos = ImGui.GetCursorScreenPos();
            var mousePos = ImGui.GetMousePos();
            
            int displayTileSize = (int)(_overlay.TileSize * _paletteZoom);
            
            for (int ty = 0; ty < _overlay.TilesPerColumn; ty++)
            {
                for (int tx = 0; tx < _overlay.TilesPerRow; tx++)
                {
                    Vector2 tilePos = new Vector2(
                        canvasPos.X + tx * displayTileSize,
                        canvasPos.Y + ty * displayTileSize
                    );
                    
                    Vector2 uv0 = new Vector2(
                        (float)(tx * _overlay.TileSize) / _overlay.TilesetTexture.Width,
                        (float)(ty * _overlay.TileSize) / _overlay.TilesetTexture.Height
                    );
                    
                    Vector2 uv1 = new Vector2(
                        (float)((tx + 1) * _overlay.TileSize) / _overlay.TilesetTexture.Width,
                        (float)((ty + 1) * _overlay.TileSize) / _overlay.TilesetTexture.Height
                    );
                    
                    drawList.AddImage(
                        _overlay.TilesetTexturePtr, 
                        tilePos, 
                        tilePos + new Vector2(displayTileSize, displayTileSize), 
                        uv0, 
                        uv1
                    );
                    
                    bool isSelected = _overlay.SelectedTiles.Contains(new Vector2(tx, ty));
                    
                    if (isSelected)
                    {
                        drawList.AddRect(
                            tilePos, 
                            tilePos + new Vector2(displayTileSize, displayTileSize), 
                            ImGui.GetColorU32(new Vector4(0.3f, 0.8f, 1.0f, 1.0f)),
                            0f,
                            ImDrawFlags.None,
                            3f
                        );
                    }
                    else
                    {
                        drawList.AddRect(
                            tilePos, 
                            tilePos + new Vector2(displayTileSize, displayTileSize), 
                            ImGui.GetColorU32(new Vector4(0.3f, 0.3f, 0.3f, 0.5f))
                        );
                    }
                }
            }
            
            if (ImGui.IsWindowHovered())
            {
                HandlePaletteInput(canvasPos, mousePos, displayTileSize);
            }
            
            ImGui.Dummy(new Vector2(_overlay.TilesPerRow * displayTileSize, _overlay.TilesPerColumn * displayTileSize));
            
            ImGui.EndChild();
        }

        private void HandlePaletteInput(Vector2 canvasPos, Vector2 mousePos, int displayTileSize)
        {
            int tileX = (int)((mousePos.X - canvasPos.X) / displayTileSize);
            int tileY = (int)((mousePos.Y - canvasPos.Y) / displayTileSize);
            
            if (tileX < 0 || tileX >= _overlay.TilesPerRow || tileY < 0 || tileY >= _overlay.TilesPerColumn)
                return;
            
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    _overlay.SelectedTiles.Clear();
                }
                _selectionStart = new Vector2(tileX, tileY);
                _isSelecting = true;
            }
            
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && _isSelecting)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    _overlay.SelectedTiles.Clear();
                }
                
                int minX = (int)Math.Min(_selectionStart.X, tileX);
                int maxX = (int)Math.Max(_selectionStart.X, tileX);
                int minY = (int)Math.Min(_selectionStart.Y, tileY);
                int maxY = (int)Math.Max(_selectionStart.Y, tileY);
                
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        var tile = new Vector2(x, y);
                        if (!_overlay.SelectedTiles.Contains(tile))
                        {
                            _overlay.SelectedTiles.Add(tile);
                        }
                    }
                }
            }
            
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                _isSelecting = false;
            }
        }
    }
}