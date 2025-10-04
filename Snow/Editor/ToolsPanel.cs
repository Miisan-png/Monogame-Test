using ImGuiNET;
using System;
using System.IO;
using System.Numerics;
using System.Runtime.InteropServices;

namespace Snow.Editor
{
    public class ToolsPanel
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

        private TilemapEditorOverlay _tilemapOverlay;
        private ActorEditor _actorEditor;
        
        private int _newMapWidth = 40;
        private int _newMapHeight = 23;
        private int _newMapTileSize = 16;

        public bool IsOpen { get; set; } = true;

        public ToolsPanel(TilemapEditorOverlay tilemapOverlay, ActorEditor actorEditor)
        {
            _tilemapOverlay = tilemapOverlay;
            _actorEditor = actorEditor;
        }

        public void Render()
        {
            if (!IsOpen) return;

            bool isOpen = IsOpen;
            ImGui.Begin("Tools", ref isOpen);
            IsOpen = isOpen;

            if (ImGui.CollapsingHeader("Tilemap Tools", ImGuiTreeNodeFlags.DefaultOpen))
            {
                RenderTilemapTools();
            }

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Actor Tools"))
            {
                RenderActorTools();
            }

            ImGui.End();
            
            RenderNewMapPopup();
        }

        private void RenderTilemapTools()
        {
            if (ImGui.Button("New Map"))
            {
                ImGui.OpenPopup("NewMapPopup");
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Load Map"))
            {
                string path = ShowOpenFileDialog("tilemap");
                if (!string.IsNullOrEmpty(path))
                {
                    _tilemapOverlay.LoadMap(path);
                }
            }
            
            ImGui.SameLine();
            
            if (ImGui.Button("Save Map"))
            {
                string path = ShowSaveFileDialog("tilemap");
                if (!string.IsNullOrEmpty(path))
                {
                    _tilemapOverlay.SaveMap(path);
                }
            }

            ImGui.Separator();

            if (ImGui.Button("Load Tileset"))
            {
                string path = ShowOpenFileDialog("png");
                if (!string.IsNullOrEmpty(path))
                {
                    _tilemapOverlay.LoadTileset(path);
                }
            }

            ImGui.Separator();

            bool collisionMode = _tilemapOverlay.CollisionMode;
            bool spikeMode = _tilemapOverlay.SpikeMode;

            if (ImGui.RadioButton("Draw Tiles", !collisionMode && !spikeMode))
            {
                _tilemapOverlay.CollisionMode = false;
                _tilemapOverlay.SpikeMode = false;
            }
            
            if (ImGui.RadioButton("Edit Collision", collisionMode))
            {
                _tilemapOverlay.CollisionMode = true;
                _tilemapOverlay.SpikeMode = false;
            }

            if (ImGui.RadioButton("Edit Spikes", spikeMode))
            {
                _tilemapOverlay.CollisionMode = false;
                _tilemapOverlay.SpikeMode = true;
            }

            ImGui.Separator();

            bool showGrid = _tilemapOverlay.ShowGrid;
            if (ImGui.Checkbox("Show Grid", ref showGrid))
            {
                _tilemapOverlay.ShowGrid = showGrid;
            }

            bool showCollision = _tilemapOverlay.ShowCollisionOverlay;
            if (ImGui.Checkbox("Show Collision", ref showCollision))
            {
                _tilemapOverlay.ShowCollisionOverlay = showCollision;
            }

            bool showSpikes = _tilemapOverlay.ShowSpikeOverlay;
            if (ImGui.Checkbox("Show Spikes", ref showSpikes))
            {
                _tilemapOverlay.ShowSpikeOverlay = showSpikes;
            }

            ImGui.Separator();

            ImGui.Text($"Map: {_tilemapOverlay.GridWidth}x{_tilemapOverlay.GridHeight}");
            ImGui.Text($"Tile Size: {_tilemapOverlay.TileSize}px");
            ImGui.Text($"Selected: {_tilemapOverlay.SelectedTiles.Count}");

            ImGui.Separator();

            if (_tilemapOverlay.HasUnsavedChanges)
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(1, 1, 0, 1));
                ImGui.Text($"Autosave in {_tilemapOverlay.AutoSaveTimer:F1}s");
                ImGui.PopStyleColor();
            }
            else
            {
                ImGui.PushStyleColor(ImGuiCol.Text, new Vector4(0, 1, 0, 1));
                ImGui.Text("Saved");
                ImGui.PopStyleColor();
            }
        }

        private void RenderNewMapPopup()
        {
            if (ImGui.BeginPopupModal("NewMapPopup", ImGuiWindowFlags.AlwaysAutoResize))
            {
                ImGui.Text("Create New Tilemap");
                ImGui.Separator();
                
                ImGui.InputInt("Width", ref _newMapWidth);
                ImGui.InputInt("Height", ref _newMapHeight);
                ImGui.InputInt("Tile Size", ref _newMapTileSize);
                
                ImGui.Separator();
                
                if (ImGui.Button("Create"))
                {
                    _tilemapOverlay.NewMap(_newMapTileSize, _newMapWidth, _newMapHeight);
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

        private void RenderActorTools()
        {
            ImGui.Text("Actor editing tools");
            ImGui.TextWrapped("Coming soon...");
        }

        private string ShowOpenFileDialog(string extension)
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = Marshal.SizeOf(ofn);
            
            string filter = extension.ToLower() == "png" ? "PNG Files\0*.png\0All Files\0*.*\0\0" : 
                           extension.ToLower() == "tilemap" ? "Tilemap Files\0*.tilemap\0All Files\0*.*\0\0" :
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
            
            string filter = extension.ToLower() == "tilemap" ? "Tilemap Files\0*.tilemap\0All Files\0*.*\0\0" : 
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
