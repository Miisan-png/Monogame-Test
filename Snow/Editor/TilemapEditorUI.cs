using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Snow.Editor
{
    public class TilemapEditorUI
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

        private TilemapEditor _editor;

        public TilemapEditorUI(TilemapEditor editor)
        {
            _editor = editor;
        }

        public void Render()
        {
            ImGui.Begin("Tilemap Editor", ImGuiWindowFlags.MenuBar);
            
            RenderMenuBar();
            RenderStatusMessage();
            RenderNewMapPopup();
            
            ImGui.Columns(2);
            ImGui.SetColumnWidth(0, 320);
            
            RenderToolPanel();
            
            ImGui.NextColumn();
            
            RenderTilemapView();
            
            ImGui.Columns(1);
            
            ImGui.End();
        }

        private void RenderMenuBar()
        {
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
                            _editor.LoadTileset(path);
                        }
                    }
                    
                    if (ImGui.MenuItem("Save Map"))
                    {
                        string path = ShowSaveFileDialog("json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            _editor.SaveMap(path);
                        }
                    }
                    
                    if (ImGui.MenuItem("Load Map"))
                    {
                        string path = ShowOpenFileDialog("json");
                        if (!string.IsNullOrEmpty(path))
                        {
                            _editor.LoadMap(path);
                        }
                    }
                    
                    ImGui.EndMenu();
                }
                
                if (ImGui.BeginMenu("Edit"))
                {
                    if (ImGui.MenuItem("Undo", "Ctrl+Z", false, _editor.UndoStack.Count > 0))
                    {
                        _editor.Undo();
                    }
                    
                    if (ImGui.MenuItem("Redo", "Ctrl+Y", false, _editor.RedoStack.Count > 0))
                    {
                        _editor.Redo();
                    }
                    
                    if (ImGui.MenuItem("Clear Map"))
                    {
                        _editor.ClearMap();
                    }
                    
                    ImGui.EndMenu();
                }
                
                ImGui.EndMenuBar();
            }
        }

        private void RenderStatusMessage()
        {
            if (_editor.StatusMessageTimer > 0)
            {
                if (_editor.StatusMessageIsError)
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 0.3f, 0.3f, 1));
                }
                else
                {
                    ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0.3f, 1, 0.3f, 1));
                }
                
                ImGui.Text(_editor.StatusMessage);
                ImGui.PopStyleColor();
                ImGui.Separator();
            }
        }

        private void RenderNewMapPopup()
        {
            if (ImGui.BeginPopupModal("NewMapPopup", ImGuiWindowFlags.AlwaysAutoResize))
            {
                int width = _editor.GridWidth;
                int height = _editor.GridHeight;
                int tileSize = _editor.TileSize;
                
                ImGui.InputInt("Width", ref width);
                ImGui.InputInt("Height", ref height);
                ImGui.InputInt("Tile Size", ref tileSize);
                
                if (ImGui.Button("Create"))
                {
                    _editor.NewMap(width, height, tileSize);
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.SameLine();
                
                if (ImGui.Button("Cancel"))
                {
                    ImGui.CloseCurrentPopup();
                }
                
                ImGui.EndPopup();
            }
        }

        private void RenderToolPanel()
        {
            ImGui.BeginChild("ToolPanel", new Vector2(0, 0), ImGuiChildFlags.Border);
            
            ImGui.Text("Tools");
            ImGui.Separator();
            
            bool collisionMode = _editor.CollisionMode;
            
            if (ImGui.RadioButton("Draw", !collisionMode))
            {
                _editor.CollisionMode = false;
            }
            
            if (ImGui.RadioButton("Collision", collisionMode))
            {
                _editor.CollisionMode = true;
            }
            
            ImGui.Separator();
            
            ImGui.Text($"Grid: {_editor.GridWidth}x{_editor.GridHeight}");
            ImGui.Text($"Tile Size: {_editor.TileSize}px");
            ImGui.Text($"Selected Tiles: {_editor.SelectedTiles.Count}");
            
            ImGui.Separator();
            
            ImGui.Text("Tilemap View");
            float zoom = _editor.Zoom;
            if (ImGui.SliderFloat("Zoom##map", ref zoom, 0.5f, 4.0f))
            {
                _editor.Zoom = zoom;
            }
            
            ImGui.Separator();
            
            if (_editor.TilesetTexture != null)
            {
                ImGui.Text("Tileset Palette");
                float paletteZoom = _editor.PaletteZoom;
                if (ImGui.SliderFloat("Zoom##palette", ref paletteZoom, 1.0f, 4.0f))
                {
                    _editor.PaletteZoom = paletteZoom;
                }
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
            ImGui.BeginChild("TilesetPalette", new Vector2(0, 0), ImGuiChildFlags.Border);
            
            if (_editor.TilesetTexture == null)
            {
                ImGui.EndChild();
                return;
            }
            
            var drawList = ImGui.GetWindowDrawList();
            var canvasPos = ImGui.GetCursorScreenPos();
            var mousePos = ImGui.GetMousePos();
            
            int displayTileSize = (int)(_editor.TileSize * _editor.PaletteZoom);
            
            for (int ty = 0; ty < _editor.TilesPerColumn; ty++)
            {
                for (int tx = 0; tx < _editor.TilesPerRow; tx++)
                {
                    Vector2 tilePos = new Vector2(
                        canvasPos.X + tx * displayTileSize,
                        canvasPos.Y + ty * displayTileSize
                    );
                    
                    Vector2 uv0 = new Vector2(
                        (float)(tx * _editor.TileSize) / _editor.TilesetTexture.Width,
                        (float)(ty * _editor.TileSize) / _editor.TilesetTexture.Height
                    );
                    
                    Vector2 uv1 = new Vector2(
                        (float)((tx + 1) * _editor.TileSize) / _editor.TilesetTexture.Width,
                        (float)((ty + 1) * _editor.TileSize) / _editor.TilesetTexture.Height
                    );
                    
                    drawList.AddImage(
                        _editor.TilesetTexturePtr, 
                        tilePos, 
                        tilePos + new Vector2(displayTileSize, displayTileSize), 
                        uv0, 
                        uv1
                    );
                    
                    bool isSelected = _editor.SelectedTiles.Contains(new Vector2(tx, ty));
                    
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
            
            ImGui.Dummy(new Vector2(_editor.TilesPerRow * displayTileSize, _editor.TilesPerColumn * displayTileSize));
            
            ImGui.EndChild();
        }

        private void HandlePaletteInput(Vector2 canvasPos, Vector2 mousePos, int displayTileSize)
        {
            int tileX = (int)((mousePos.X - canvasPos.X) / displayTileSize);
            int tileY = (int)((mousePos.Y - canvasPos.Y) / displayTileSize);
            
            if (tileX < 0 || tileX >= _editor.TilesPerRow || tileY < 0 || tileY >= _editor.TilesPerColumn)
                return;
            
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    _editor.SelectedTiles.Clear();
                }
                _editor.SelectionStart = new Vector2(tileX, tileY);
                _editor.IsSelecting = true;
            }
            
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) && _editor.IsSelecting)
            {
                if (!ImGui.GetIO().KeyShift)
                {
                    _editor.SelectedTiles.Clear();
                }
                
                int minX = (int)Math.Min(_editor.SelectionStart.X, tileX);
                int maxX = (int)Math.Max(_editor.SelectionStart.X, tileX);
                int minY = (int)Math.Min(_editor.SelectionStart.Y, tileY);
                int maxY = (int)Math.Max(_editor.SelectionStart.Y, tileY);
                
                for (int y = minY; y <= maxY; y++)
                {
                    for (int x = minX; x <= maxX; x++)
                    {
                        var tile = new Vector2(x, y);
                        if (!_editor.SelectedTiles.Contains(tile))
                        {
                            _editor.SelectedTiles.Add(tile);
                        }
                    }
                }
            }
            
            if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
            {
                _editor.IsSelecting = false;
            }
        }

        private void RenderTilemapView()
        {
            ImGui.BeginChild("TilemapView", new Vector2(0, 0), ImGuiChildFlags.Border, ImGuiWindowFlags.HorizontalScrollbar);
            
            var drawList = ImGui.GetWindowDrawList();
            var canvasPos = ImGui.GetCursorScreenPos();
            var mousePos = ImGui.GetMousePos();
            
            int displayTileSize = (int)(_editor.TileSize * _editor.Zoom);
            
            for (int y = 0; y < _editor.GridHeight; y++)
            {
                for (int x = 0; x < _editor.GridWidth; x++)
                {
                    Vector2 tilePos = new Vector2(
                        canvasPos.X + x * displayTileSize + _editor.ViewportScroll.X,
                        canvasPos.Y + y * displayTileSize + _editor.ViewportScroll.Y
                    );
                    
                    Vector2 tileSize = new Vector2(displayTileSize, displayTileSize);
                    
                    drawList.AddRect(tilePos, tilePos + tileSize, ImGui.GetColorU32(new Vector4(0.2f, 0.2f, 0.2f, 0.5f)));
                    
                    int tileId = _editor.WorldData[y][x];
                    
                    if (tileId > 0 && _editor.TilesetTexture != null)
                    {
                        int tileIndex = tileId - 1;
                        int tx = tileIndex % _editor.TilesPerRow;
                        int ty = tileIndex / _editor.TilesPerRow;
                        
                        Vector2 uv0 = new Vector2(
                            (float)(tx * _editor.TileSize) / _editor.TilesetTexture.Width,
                            (float)(ty * _editor.TileSize) / _editor.TilesetTexture.Height
                        );
                        
                        Vector2 uv1 = new Vector2(
                            (float)((tx + 1) * _editor.TileSize) / _editor.TilesetTexture.Width,
                            (float)((ty + 1) * _editor.TileSize) / _editor.TilesetTexture.Height
                        );
                        
                        drawList.AddImage(_editor.TilesetTexturePtr, tilePos, tilePos + tileSize, uv0, uv1);
                    }
                    
                    if (_editor.CollisionData[y][x])
                    {
                        drawList.AddRectFilled(tilePos, tilePos + tileSize, 
                            ImGui.GetColorU32(new Vector4(1.0f, 0.0f, 0.0f, 0.3f)));
                    }
                }
            }
            
            if (ImGui.IsWindowHovered())
            {
                HandleMapInput(canvasPos, mousePos, displayTileSize);
            }
            
            ImGui.Dummy(new Vector2(_editor.GridWidth * displayTileSize, _editor.GridHeight * displayTileSize));
            
            ImGui.EndChild();
        }

        private void HandleMapInput(Vector2 canvasPos, Vector2 mousePos, int displayTileSize)
        {
            if (ImGui.IsMouseClicked(ImGuiMouseButton.Left) || ImGui.IsMouseClicked(ImGuiMouseButton.Right))
            {
                int gridX = (int)((mousePos.X - canvasPos.X - _editor.ViewportScroll.X) / displayTileSize);
                int gridY = (int)((mousePos.Y - canvasPos.Y - _editor.ViewportScroll.Y) / displayTileSize);
                
                if (gridX >= 0 && gridX < _editor.GridWidth && gridY >= 0 && gridY < _editor.GridHeight)
                {
                    if (_editor.CollisionMode)
                    {
                        bool newValue = ImGui.IsMouseClicked(ImGuiMouseButton.Left);
                        bool oldValue = _editor.CollisionData[gridY][gridX];
                        
                        if (newValue != oldValue)
                        {
                            _editor.UndoStack.Push(new TilemapEditor.EditorAction
                            {
                                X = gridX,
                                Y = gridY,
                                OldTileId = _editor.WorldData[gridY][gridX],
                                NewTileId = _editor.WorldData[gridY][gridX],
                                OldCollision = oldValue,
                                NewCollision = newValue
                            });
                            
                            _editor.CollisionData[gridY][gridX] = newValue;
                            _editor.RedoStack.Clear();
                        }
                    }
                    else
                    {
                        bool isRightClick = ImGui.IsMouseClicked(ImGuiMouseButton.Right);
                        _editor.PlaceTiles(gridX, gridY, isRightClick);
                    }
                }
            }
            
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Left) || ImGui.IsMouseDragging(ImGuiMouseButton.Right))
            {
                int gridX = (int)((mousePos.X - canvasPos.X - _editor.ViewportScroll.X) / displayTileSize);
                int gridY = (int)((mousePos.Y - canvasPos.Y - _editor.ViewportScroll.Y) / displayTileSize);
                
                if (gridX >= 0 && gridX < _editor.GridWidth && gridY >= 0 && gridY < _editor.GridHeight)
                {
                    if (_editor.CollisionMode)
                    {
                        bool newValue = ImGui.IsMouseDragging(ImGuiMouseButton.Left);
                        _editor.CollisionData[gridY][gridX] = newValue;
                    }
                    else
                    {
                        bool isRightClick = ImGui.IsMouseDragging(ImGuiMouseButton.Right);
                        _editor.PlaceTiles(gridX, gridY, isRightClick);
                    }
                }
            }
            
            if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
            {
                var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Middle);
                _editor.ViewportScroll = new Vector2(
                    _editor.ViewportScroll.X + delta.X,
                    _editor.ViewportScroll.Y + delta.Y
                );
                ImGui.ResetMouseDragDelta(ImGuiMouseButton.Middle);
            }
            
            float wheel = ImGui.GetIO().MouseWheel;
            if (wheel != 0)
            {
                _editor.Zoom += wheel * 0.1f;
                _editor.Zoom = Math.Clamp(_editor.Zoom, 0.5f, 4.0f);
            }
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
    }
}