using ImGuiNET;
using Microsoft.Xna.Framework.Graphics;
using Snow.Engine;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;

namespace Snow.Editor
{
    public class ActorEditor
    {
        private string _currentScenePath;
        private SceneData _currentScene;
        private int _selectedEntityIndex = -1;
        private bool _isOpen = true;
        private GraphicsDevice _graphicsDevice;
        private MonoGame.ImGuiNet.ImGuiRenderer _imGuiRenderer;
        
        private Dictionary<string, Texture2D> _loadedTextures = new Dictionary<string, Texture2D>();
        private Dictionary<string, IntPtr> _texturePointers = new Dictionary<string, IntPtr>();
        
        private bool _showCollisionGizmo = true;
        private bool _isDraggingCollision = false;
        private bool _isResizingCollision = false;
        private Vector2 _dragStartMouse;
        private Vector2 _dragStartOffset;
        private Vector2 _dragStartSize;
        private string _resizeHandle = "";
        
        private float _previewZoom = 4.0f;
        private Vector2 _previewPan = Vector2.Zero;
        private bool _isPanning = false;
        private Vector2 _panStart;
        
        private int _currentAnimationFrame = 0;
        private float _animationTimer = 0f;
        private float _animationSpeed = 0.1f;
        private bool _isPlayingAnimation = false;
        private List<string> _currentAnimationFrames = new List<string>();
        
        // Auto-save functionality
        private bool _hasUnsavedChanges = false;
        private float _autoSaveTimer = 0f;
        private const float AUTO_SAVE_DELAY = 2.0f; // Save 2 seconds after last edit

        public bool IsOpen { get => _isOpen; set => _isOpen = value; }

        public ActorEditor(GraphicsDevice graphicsDevice, MonoGame.ImGuiNet.ImGuiRenderer imGuiRenderer)
        {
            _graphicsDevice = graphicsDevice;
            _imGuiRenderer = imGuiRenderer;
        }

        public void LoadSceneData(SceneData sceneData, string scenePath)
        {
            try
            {
                _currentScenePath = scenePath;
                _currentScene = sceneData;
                _selectedEntityIndex = -1;
                _previewPan = Vector2.Zero;
                _hasUnsavedChanges = false;
                _autoSaveTimer = 0f;
                
                System.Console.WriteLine($"[ActorEditor] Scene data loaded: {scenePath}");
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"[ActorEditor] Failed to load scene data: {ex.Message}");
            }
        }

        public void SaveSceneData()
        {
            if (_currentScene != null && !string.IsNullOrEmpty(_currentScenePath))
            {
                try
                {
                    SceneSerializer.SaveScene(_currentScene, _currentScenePath);
                    _hasUnsavedChanges = false;
                    _autoSaveTimer = 0f;
                    System.Console.WriteLine($"[ActorEditor] Scene saved: {_currentScenePath}");
                }
                catch (Exception ex)
                {
                    System.Console.WriteLine($"[ActorEditor] Failed to save scene: {ex.Message}");
                }
            }
        }

        private void MarkAsChanged()
        {
            _hasUnsavedChanges = true;
            _autoSaveTimer = AUTO_SAVE_DELAY;
        }

        public void Update(float deltaTime)
        {
            // Auto-save logic
            if (_hasUnsavedChanges && _autoSaveTimer > 0)
            {
                _autoSaveTimer -= deltaTime;
                if (_autoSaveTimer <= 0)
                {
                    SaveSceneData();
                }
            }
        
            if (_isPlayingAnimation && _currentAnimationFrames.Count > 0)
            {
                _animationTimer += deltaTime;
                if (_animationTimer >= _animationSpeed)
                {
                    _animationTimer = 0f;
                    _currentAnimationFrame = (_currentAnimationFrame + 1) % _currentAnimationFrames.Count;
                }
            }
        }

        public void Render()
        {
            if (!_isOpen) return;

            ImGui.Begin("Actor Editor", ref _isOpen, ImGuiWindowFlags.MenuBar);

            if (ImGui.BeginMenuBar())
            {
                if (ImGui.BeginMenu("View"))
                {
                    ImGui.MenuItem("Show Collision Gizmo", null, ref _showCollisionGizmo);
                    ImGui.EndMenu();
                }
                
                ImGui.EndMenuBar();
            }

            if (_currentScene == null)
            {
                ImGui.Text("No scene loaded. Open a scene from File menu.");
                ImGui.End();
                return;
            }

            // Show auto-save indicator
            if (_hasUnsavedChanges)
            {
                ImGui.TextColored(new Vector4(1, 1, 0, 1), $"Auto-saving in {_autoSaveTimer:F1}s...");
            }
            else
            {
                ImGui.TextColored(new Vector4(0, 1, 0, 1), "All changes saved");
            }

            ImGui.Columns(3);
            ImGui.SetColumnWidth(0, 200);
            ImGui.SetColumnWidth(1, 400);

            RenderEntityList();

            ImGui.NextColumn();

            RenderPreviewWindow();

            ImGui.NextColumn();

            RenderEntityEditor();

            ImGui.Columns(1);

            ImGui.End();
        }

        private void RenderEntityList()
        {
            ImGui.BeginChild("EntityList", new Vector2(0, 0), ImGuiChildFlags.Border);

            ImGui.Text("Entities");
            ImGui.Separator();

            for (int i = 0; i < _currentScene.Entities.Count; i++)
            {
                var entity = _currentScene.Entities[i];
                bool isSelected = _selectedEntityIndex == i;

                if (ImGui.Selectable($"{entity.Type}##entity_{i}", isSelected))
                {
                    _selectedEntityIndex = i;
                    _previewPan = Vector2.Zero;
                    LoadEntityTextures(entity);
                }

                if (ImGui.IsItemHovered())
                {
                    ImGui.BeginTooltip();
                    ImGui.Text($"ID: {entity.Id}");
                    ImGui.Text($"Position: ({entity.X}, {entity.Y})");
                    ImGui.EndTooltip();
                }
            }

            ImGui.EndChild();
        }

        private void LoadEntityTextures(EntityData entity)
        {
            _currentAnimationFrames.Clear();
            _currentAnimationFrame = 0;
            _isPlayingAnimation = false;

            if (entity.SpriteData != null && !string.IsNullOrEmpty(entity.SpriteData.TexturePath))
            {
                LoadTexture(entity.SpriteData.TexturePath);
            }

            if (entity.Animations != null && entity.Animations.Count > 0)
            {
                foreach (var anim in entity.Animations)
                {
                    foreach (var framePath in anim.Value)
                    {
                        LoadTexture(framePath);
                        if (anim.Key == "idle" || _currentAnimationFrames.Count == 0)
                        {
                            _currentAnimationFrames.Add(framePath);
                        }
                    }
                }
            }
        }

        private void LoadTexture(string path)
        {
            if (string.IsNullOrEmpty(path) || _loadedTextures.ContainsKey(path))
                return;

            try
            {
                if (File.Exists(path))
                {
                    using (FileStream stream = new FileStream(path, FileMode.Open))
                    {
                        Texture2D texture = Texture2D.FromStream(_graphicsDevice, stream);
                        _loadedTextures[path] = texture;
                        _texturePointers[path] = _imGuiRenderer.BindTexture(texture);
                    }
                }
            }
            catch (Exception ex)
            {
                System.Console.WriteLine($"Failed to load texture {path}: {ex.Message}");
            }
        }

        private void RenderPreviewWindow()
        {
            ImGui.BeginChild("PreviewWindow", new Vector2(0, 0), ImGuiChildFlags.Border);

            if (_selectedEntityIndex < 0 || _selectedEntityIndex >= _currentScene.Entities.Count)
            {
                ImGui.Text("Select an entity to preview");
                ImGui.EndChild();
                return;
            }

            var entity = _currentScene.Entities[_selectedEntityIndex];

            ImGui.Text("Preview");
            ImGui.SameLine();
            ImGui.SetNextItemWidth(100);
            ImGui.SliderFloat("Zoom", ref _previewZoom, 1.0f, 10.0f);
            
            ImGui.Separator();

            var drawList = ImGui.GetWindowDrawList();
            var canvasPos = ImGui.GetCursorScreenPos();
            var canvasSize = ImGui.GetContentRegionAvail();
            var mousePos = ImGui.GetMousePos();

            drawList.AddRectFilled(canvasPos, new Vector2(canvasPos.X + canvasSize.X, canvasPos.Y + canvasSize.Y), 
                ImGui.GetColorU32(new Vector4(0.1f, 0.1f, 0.1f, 1.0f)));

            Vector2 centerOffset = new Vector2(canvasSize.X / 2, canvasSize.Y / 2);
            Vector2 spritePos = canvasPos + centerOffset + _previewPan;

            string texturePath = GetCurrentTexturePath(entity);
            if (!string.IsNullOrEmpty(texturePath) && _texturePointers.ContainsKey(texturePath))
            {
                var texturePtr = _texturePointers[texturePath];
                var texture = _loadedTextures[texturePath];

                float displayWidth = texture.Width * _previewZoom;
                float displayHeight = texture.Height * _previewZoom;

                Vector2 spriteTopLeft = new Vector2(spritePos.X - displayWidth / 2, spritePos.Y - displayHeight / 2);

                drawList.AddImage(texturePtr, spriteTopLeft, 
                    new Vector2(spriteTopLeft.X + displayWidth, spriteTopLeft.Y + displayHeight),
                    Vector2.Zero, Vector2.One);

                if (_showCollisionGizmo && entity.CollisionShape != null)
                {
                    RenderCollisionGizmo(drawList, entity, spriteTopLeft, canvasPos, canvasSize, mousePos);
                }
            }

            if (ImGui.IsWindowHovered())
            {
                if (ImGui.IsMouseDragging(ImGuiMouseButton.Middle))
                {
                    if (!_isPanning)
                    {
                        _isPanning = true;
                        _panStart = _previewPan;
                    }
                    var delta = ImGui.GetMouseDragDelta(ImGuiMouseButton.Middle);
                    _previewPan = _panStart + new Vector2(delta.X, delta.Y);
                }
                else
                {
                    _isPanning = false;
                }

                float wheel = ImGui.GetIO().MouseWheel;
                if (wheel != 0)
                {
                    _previewZoom += wheel * 0.5f;
                    _previewZoom = Math.Clamp(_previewZoom, 1.0f, 10.0f);
                }
            }

            ImGui.Dummy(canvasSize);

            ImGui.Separator();

            if (_currentAnimationFrames.Count > 0)
            {
                ImGui.Text($"Animation Frame: {_currentAnimationFrame + 1}/{_currentAnimationFrames.Count}");
                
                if (ImGui.Button(_isPlayingAnimation ? "Stop" : "Play"))
                {
                    _isPlayingAnimation = !_isPlayingAnimation;
                }
                
                ImGui.SameLine();
                if (ImGui.Button("< Prev"))
                {
                    _currentAnimationFrame = (_currentAnimationFrame - 1 + _currentAnimationFrames.Count) % _currentAnimationFrames.Count;
                }
                
                ImGui.SameLine();
                if (ImGui.Button("Next >"))
                {
                    _currentAnimationFrame = (_currentAnimationFrame + 1) % _currentAnimationFrames.Count;
                }
                
                ImGui.SliderFloat("Speed", ref _animationSpeed, 0.05f, 0.5f);
            }

            ImGui.EndChild();
        }

        private string GetCurrentTexturePath(EntityData entity)
        {
            if (_currentAnimationFrames.Count > 0)
            {
                return _currentAnimationFrames[_currentAnimationFrame];
            }

            if (entity.SpriteData != null && !string.IsNullOrEmpty(entity.SpriteData.TexturePath))
            {
                return entity.SpriteData.TexturePath;
            }

            return null;
        }

        private void RenderCollisionGizmo(ImDrawListPtr drawList, EntityData entity, 
            Vector2 spriteTopLeft, Vector2 canvasPos, Vector2 canvasSize, Vector2 mousePos)
        {
            var shape = entity.CollisionShape;
            
            Vector2 collisionPos = new Vector2(
                spriteTopLeft.X + shape.OffsetX * _previewZoom,
                spriteTopLeft.Y + shape.OffsetY * _previewZoom
            );

            if (shape.Type == "box")
            {
                float width = shape.Width * _previewZoom;
                float height = shape.Height * _previewZoom;

                drawList.AddRect(collisionPos, 
                    new Vector2(collisionPos.X + width, collisionPos.Y + height),
                    ImGui.GetColorU32(new Vector4(0.0f, 1.0f, 0.0f, 1.0f)), 0f, ImDrawFlags.None, 2f);

                DrawResizeHandles(drawList, collisionPos, width, height);

                if (ImGui.IsWindowHovered() && !_isDraggingCollision && !_isResizingCollision)
                {
                    if (IsMouseOverRect(mousePos, collisionPos, width, height))
                    {
                        drawList.AddRectFilled(collisionPos, 
                            new Vector2(collisionPos.X + width, collisionPos.Y + height),
                            ImGui.GetColorU32(new Vector4(0.0f, 1.0f, 0.0f, 0.2f)));

                        if (ImGui.IsMouseClicked(ImGuiMouseButton.Left))
                        {
                            string handle = GetResizeHandle(mousePos, collisionPos, width, height);
                            if (!string.IsNullOrEmpty(handle))
                            {
                                _isResizingCollision = true;
                                _resizeHandle = handle;
                                _dragStartMouse = mousePos;
                                _dragStartOffset = new Vector2(shape.OffsetX, shape.OffsetY);
                                _dragStartSize = new Vector2(shape.Width, shape.Height);
                            }
                            else
                            {
                                _isDraggingCollision = true;
                                _dragStartMouse = mousePos;
                                _dragStartOffset = new Vector2(shape.OffsetX, shape.OffsetY);
                            }
                        }
                    }
                }

                if (_isDraggingCollision && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var delta = new Vector2(mousePos.X - _dragStartMouse.X, mousePos.Y - _dragStartMouse.Y);
                    shape.OffsetX = _dragStartOffset.X + delta.X / _previewZoom;
                    shape.OffsetY = _dragStartOffset.Y + delta.Y / _previewZoom;
                    MarkAsChanged();
                }

                if (_isResizingCollision && ImGui.IsMouseDragging(ImGuiMouseButton.Left))
                {
                    var delta = new Vector2(mousePos.X - _dragStartMouse.X, mousePos.Y - _dragStartMouse.Y);
                    ResizeCollisionBox(shape, delta);
                    MarkAsChanged();
                }

                if (ImGui.IsMouseReleased(ImGuiMouseButton.Left))
                {
                    _isDraggingCollision = false;
                    _isResizingCollision = false;
                    _resizeHandle = "";
                }
            }
            else if (shape.Type == "circle")
            {
                float radius = shape.Radius * _previewZoom;
                Vector2 center = new Vector2(collisionPos.X + radius, collisionPos.Y + radius);

                drawList.AddCircle(center, radius, ImGui.GetColorU32(new Vector4(0.0f, 1.0f, 0.0f, 1.0f)), 32, 2f);
            }
        }

        private void DrawResizeHandles(ImDrawListPtr drawList, Vector2 pos, float width, float height)
        {
            float handleSize = 8f;
            uint handleColor = ImGui.GetColorU32(new Vector4(1.0f, 1.0f, 0.0f, 1.0f));

            Vector2[] handles = {
                new Vector2(pos.X, pos.Y),
                new Vector2(pos.X + width / 2, pos.Y),
                new Vector2(pos.X + width, pos.Y),
                new Vector2(pos.X + width, pos.Y + height / 2),
                new Vector2(pos.X + width, pos.Y + height),
                new Vector2(pos.X + width / 2, pos.Y + height),
                new Vector2(pos.X, pos.Y + height),
                new Vector2(pos.X, pos.Y + height / 2)
            };

            foreach (var handle in handles)
            {
                drawList.AddRectFilled(
                    new Vector2(handle.X - handleSize / 2, handle.Y - handleSize / 2),
                    new Vector2(handle.X + handleSize / 2, handle.Y + handleSize / 2),
                    handleColor
                );
            }
        }

        private bool IsMouseOverRect(Vector2 mouse, Vector2 pos, float width, float height)
        {
            return mouse.X >= pos.X && mouse.X <= pos.X + width &&
                   mouse.Y >= pos.Y && mouse.Y <= pos.Y + height;
        }

        private string GetResizeHandle(Vector2 mouse, Vector2 pos, float width, float height)
        {
            float handleSize = 8f;
            
            if (IsMouseOverRect(mouse, new Vector2(pos.X - handleSize, pos.Y - handleSize), handleSize * 2, handleSize * 2))
                return "tl";
            if (IsMouseOverRect(mouse, new Vector2(pos.X + width - handleSize, pos.Y - handleSize), handleSize * 2, handleSize * 2))
                return "tr";
            if (IsMouseOverRect(mouse, new Vector2(pos.X - handleSize, pos.Y + height - handleSize), handleSize * 2, handleSize * 2))
                return "bl";
            if (IsMouseOverRect(mouse, new Vector2(pos.X + width - handleSize, pos.Y + height - handleSize), handleSize * 2, handleSize * 2))
                return "br";
            if (IsMouseOverRect(mouse, new Vector2(pos.X + width / 2 - handleSize, pos.Y - handleSize), handleSize * 2, handleSize * 2))
                return "t";
            if (IsMouseOverRect(mouse, new Vector2(pos.X + width / 2 - handleSize, pos.Y + height - handleSize), handleSize * 2, handleSize * 2))
                return "b";
            if (IsMouseOverRect(mouse, new Vector2(pos.X - handleSize, pos.Y + height / 2 - handleSize), handleSize * 2, handleSize * 2))
                return "l";
            if (IsMouseOverRect(mouse, new Vector2(pos.X + width - handleSize, pos.Y + height / 2 - handleSize), handleSize * 2, handleSize * 2))
                return "r";

            return "";
        }

        private void ResizeCollisionBox(CollisionShape shape, Vector2 delta)
        {
            float deltaX = delta.X / _previewZoom;
            float deltaY = delta.Y / _previewZoom;

            switch (_resizeHandle)
            {
                case "tl":
                    shape.OffsetX = _dragStartOffset.X + deltaX;
                    shape.OffsetY = _dragStartOffset.Y + deltaY;
                    shape.Width = Math.Max(1, _dragStartSize.X - deltaX);
                    shape.Height = Math.Max(1, _dragStartSize.Y - deltaY);
                    break;
                case "tr":
                    shape.OffsetY = _dragStartOffset.Y + deltaY;
                    shape.Width = Math.Max(1, _dragStartSize.X + deltaX);
                    shape.Height = Math.Max(1, _dragStartSize.Y - deltaY);
                    break;
                case "bl":
                    shape.OffsetX = _dragStartOffset.X + deltaX;
                    shape.Width = Math.Max(1, _dragStartSize.X - deltaX);
                    shape.Height = Math.Max(1, _dragStartSize.Y + deltaY);
                    break;
                case "br":
                    shape.Width = Math.Max(1, _dragStartSize.X + deltaX);
                    shape.Height = Math.Max(1, _dragStartSize.Y + deltaY);
                    break;
                case "t":
                    shape.OffsetY = _dragStartOffset.Y + deltaY;
                    shape.Height = Math.Max(1, _dragStartSize.Y - deltaY);
                    break;
                case "b":
                    shape.Height = Math.Max(1, _dragStartSize.Y + deltaY);
                    break;
                case "l":
                    shape.OffsetX = _dragStartOffset.X + deltaX;
                    shape.Width = Math.Max(1, _dragStartSize.X - deltaX);
                    break;
                case "r":
                    shape.Width = Math.Max(1, _dragStartSize.X + deltaX);
                    break;
            }
        }

        private void RenderEntityEditor()
        {
            ImGui.BeginChild("EntityEditor", new Vector2(0, 0), ImGuiChildFlags.Border);

            if (_selectedEntityIndex < 0 || _selectedEntityIndex >= _currentScene.Entities.Count)
            {
                ImGui.Text("Select an entity to edit");
                ImGui.EndChild();
                return;
            }

            var entity = _currentScene.Entities[_selectedEntityIndex];

            ImGui.Text($"Editing: {entity.Type}");
            ImGui.Separator();

            ImGui.Text($"ID: {entity.Id}");

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Transform", ImGuiTreeNodeFlags.DefaultOpen))
            {
                float x = entity.X;
                float y = entity.Y;

                if (ImGui.DragFloat("Position X", ref x, 1f))
                {
                    entity.X = x;
                    MarkAsChanged();
                }

                if (ImGui.DragFloat("Position Y", ref y, 1f))
                {
                    entity.Y = y;
                    MarkAsChanged();
                }
            }

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Collision Shape", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (entity.CollisionShape == null)
                {
                    if (ImGui.Button("Add Collision Shape"))
                    {
                        entity.CollisionShape = new CollisionShape();
                        MarkAsChanged();
                    }
                }
                else
                {
                    string[] types = { "box", "circle" };
                    int currentType = entity.CollisionShape.Type == "box" ? 0 : 1;
                    
                    if (ImGui.Combo("Type", ref currentType, types, types.Length))
                    {
                        entity.CollisionShape.Type = types[currentType];
                        MarkAsChanged();
                    }

                    float offsetX = entity.CollisionShape.OffsetX;
                    float offsetY = entity.CollisionShape.OffsetY;
                    float width = entity.CollisionShape.Width;
                    float height = entity.CollisionShape.Height;
                    float radius = entity.CollisionShape.Radius;

                    if (ImGui.DragFloat("Offset X", ref offsetX, 0.1f))
                    {
                        entity.CollisionShape.OffsetX = offsetX;
                        MarkAsChanged();
                    }

                    if (ImGui.DragFloat("Offset Y", ref offsetY, 0.1f))
                    {
                        entity.CollisionShape.OffsetY = offsetY;
                        MarkAsChanged();
                    }

                    if (entity.CollisionShape.Type == "box")
                    {
                        if (ImGui.DragFloat("Width", ref width, 0.1f, 1f, 200f))
                        {
                            entity.CollisionShape.Width = width;
                            MarkAsChanged();
                        }

                        if (ImGui.DragFloat("Height", ref height, 0.1f, 1f, 200f))
                        {
                            entity.CollisionShape.Height = height;
                            MarkAsChanged();
                        }
                    }
                    else
                    {
                        if (ImGui.DragFloat("Radius", ref radius, 0.1f, 1f, 200f))
                        {
                            entity.CollisionShape.Radius = radius;
                            MarkAsChanged();
                        }
                    }

                    if (ImGui.Button("Remove Collision Shape"))
                    {
                        entity.CollisionShape = null;
                        MarkAsChanged();
                    }
                }
            }

            ImGui.Spacing();

            if (ImGui.CollapsingHeader("Sprite Data", ImGuiTreeNodeFlags.DefaultOpen))
            {
                if (entity.SpriteData == null)
                {
                    if (ImGui.Button("Add Sprite Data"))
                    {
                        entity.SpriteData = new SpriteData();
                        MarkAsChanged();
                    }
                }
                else
                {
                    string texturePath = entity.SpriteData.TexturePath ?? "";
                    if (ImGui.InputText("Texture Path", ref texturePath, 256))
                    {
                        entity.SpriteData.TexturePath = texturePath;
                        MarkAsChanged();
                    }

                    ImGui.SameLine();
                    if (ImGui.Button("Browse"))
                    {
                        string path = ShowOpenFileDialog("png");
                        if (!string.IsNullOrEmpty(path))
                        {
                            entity.SpriteData.TexturePath = path;
                            LoadTexture(path);
                            MarkAsChanged();
                        }
                    }

                    float originX = entity.SpriteData.OriginX;
                    float originY = entity.SpriteData.OriginY;
                    float scaleX = entity.SpriteData.ScaleX;
                    float scaleY = entity.SpriteData.ScaleY;

                    if (ImGui.DragFloat("Origin X", ref originX, 0.1f))
                    {
                        entity.SpriteData.OriginX = originX;
                        MarkAsChanged();
                    }

                    if (ImGui.DragFloat("Origin Y", ref originY, 0.1f))
                    {
                        entity.SpriteData.OriginY = originY;
                        MarkAsChanged();
                    }

                    if (ImGui.DragFloat("Scale X", ref scaleX, 0.01f, 0.1f, 10f))
                    {
                        entity.SpriteData.ScaleX = scaleX;
                        MarkAsChanged();
                    }

                    if (ImGui.DragFloat("Scale Y", ref scaleY, 0.01f, 0.1f, 10f))
                    {
                        entity.SpriteData.ScaleY = scaleY;
                        MarkAsChanged();
                    }

                    if (ImGui.Button("Remove Sprite Data"))
                    {
                        entity.SpriteData = null;
                        MarkAsChanged();
                    }
                }
            }

            ImGui.EndChild();
        }

        [System.Runtime.InteropServices.DllImport("comdlg32.dll", SetLastError = true, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
        private static extern bool GetOpenFileName([System.Runtime.InteropServices.In, System.Runtime.InteropServices.Out] OpenFileName ofn);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential, CharSet = System.Runtime.InteropServices.CharSet.Auto)]
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

        private string ShowOpenFileDialog(string extension)
        {
            OpenFileName ofn = new OpenFileName();
            ofn.structSize = System.Runtime.InteropServices.Marshal.SizeOf(ofn);
            
            string filter = extension.ToLower() == "scene" ? "Scene Files\0*.scene\0All Files\0*.*\0\0" : 
                           extension.ToLower() == "png" ? "PNG Files\0*.png\0All Files\0*.*\0\0" : 
                           "All Files\0*.*\0\0";
            
            ofn.filter = filter;
            ofn.file = new string(new char[256]);
            ofn.maxFile = ofn.file.Length;
            ofn.fileTitle = new string(new char[64]);
            ofn.maxFileTitle = ofn.fileTitle.Length;
            ofn.initialDir = System.IO.Directory.GetCurrentDirectory();
            ofn.title = "Open File";
            ofn.defExt = extension;
            ofn.flags = 0x00080000 | 0x00001000 | 0x00000800 | 0x00000008;

            if (GetOpenFileName(ofn))
            {
                return ofn.file;
            }
            
            return null;
        }

        public void Dispose()
        {
            foreach (var texture in _loadedTextures.Values)
            {
                texture?.Dispose();
            }
            _loadedTextures.Clear();
            _texturePointers.Clear();
        }
    }
}
