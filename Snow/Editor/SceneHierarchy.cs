using ImGuiNET;
using Snow.Engine;
using System;
using System.Collections.Generic;
using Vector2 = System.Numerics.Vector2;
using Vector4 = System.Numerics.Vector4;

namespace Snow.Editor
{
    public class SceneHierarchy
    {
        private SceneData _currentScene;
        private string _currentScenePath;
        private int _selectedEntityIndex = -1;
        private bool _isOpen = true;
        private Action<int> _onEntitySelected;
        private Action _onSceneModified;

        public bool IsOpen { get => _isOpen; set => _isOpen = value; }
        public int SelectedEntityIndex => _selectedEntityIndex;

        public SceneHierarchy()
        {
        }

        public void SetEntitySelectedCallback(Action<int> callback)
        {
            _onEntitySelected = callback;
        }

        public void SetSceneModifiedCallback(Action callback)
        {
            _onSceneModified = callback;
        }

        public void LoadScene(SceneData sceneData, string scenePath)
        {
            _currentScene = sceneData;
            _currentScenePath = scenePath;
            _selectedEntityIndex = -1;
        }

        public void Render()
        {
            if (!_isOpen) return;

            bool isOpen = _isOpen;
            ImGui.Begin("Scene Hierarchy", ref isOpen);
            _isOpen = isOpen;

            if (_currentScene == null)
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), "No scene loaded");
                ImGui.End();
                return;
            }

            // Scene info
            ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), "Scene:");
            ImGui.SameLine();
            ImGui.Text(_currentScene.Name ?? "Unnamed");

            ImGui.Separator();

            // Entity list
            ImGui.TextColored(new Vector4(1f, 0.8f, 0.4f, 1f), $"Entities ({_currentScene.Entities.Count})");
            
            ImGui.BeginChild("EntityList", new System.Numerics.Vector2(0, -40), ImGuiChildFlags.Border);

            for (int i = 0; i < _currentScene.Entities.Count; i++)
            {
                var entity = _currentScene.Entities[i];
                RenderEntityNode(entity, i);
            }

            ImGui.EndChild();

            // Bottom buttons
            ImGui.Separator();
            if (ImGui.Button("Add Entity"))
            {
                ImGui.OpenPopup("AddEntityPopup");
            }

            ImGui.SameLine();
            
            if (ImGui.Button("Delete Selected") && _selectedEntityIndex >= 0)
            {
                _currentScene.Entities.RemoveAt(_selectedEntityIndex);
                _selectedEntityIndex = -1;
                _onSceneModified?.Invoke();
            }

            RenderAddEntityPopup();

            ImGui.End();
        }

        private void RenderEntityNode(EntityData entity, int index)
        {
            bool isSelected = _selectedEntityIndex == index;
            
            // Icon based on type
            string icon = GetEntityIcon(entity.Type);
            
            ImGuiTreeNodeFlags flags = ImGuiTreeNodeFlags.Leaf | 
                                       ImGuiTreeNodeFlags.NoTreePushOnOpen | 
                                       ImGuiTreeNodeFlags.SpanFullWidth;
            
            if (isSelected)
                flags |= ImGuiTreeNodeFlags.Selected;

            // Entity color based on type
            Vector4 color = GetEntityColor(entity.Type);
            ImGui.PushStyleColor(ImGuiCol.Text, color);
            
            bool nodeOpen = ImGui.TreeNodeEx($"{icon} {entity.Type}##{entity.Id}", flags);
            
            ImGui.PopStyleColor();

            if (ImGui.IsItemClicked())
            {
                _selectedEntityIndex = index;
                _onEntitySelected?.Invoke(index);
            }

            // Right-click context menu
            if (ImGui.BeginPopupContextItem($"EntityContext_{entity.Id}"))
            {
                ImGui.TextColored(new Vector4(0.7f, 0.7f, 0.7f, 1f), entity.Type);
                ImGui.Separator();
                
                if (ImGui.MenuItem("Focus in Scene"))
                {
                    _selectedEntityIndex = index;
                    _onEntitySelected?.Invoke(index);
                }
                
                if (ImGui.MenuItem("Duplicate"))
                {
                    DuplicateEntity(index);
                }
                
                ImGui.Separator();
                
                if (ImGui.MenuItem("Delete", "Del"))
                {
                    _currentScene.Entities.RemoveAt(index);
                    _selectedEntityIndex = -1;
                    _onSceneModified?.Invoke();
                }
                
                ImGui.EndPopup();
            }

            // Tooltip with details
            if (ImGui.IsItemHovered())
            {
                ImGui.BeginTooltip();
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), entity.Type);
                ImGui.Separator();
                ImGui.Text($"ID: {entity.Id}");
                ImGui.Text($"Position: ({entity.X:F1}, {entity.Y:F1})");
                
                if (entity.CollisionShape != null)
                {
                    ImGui.TextColored(new Vector4(0.4f, 1f, 0.4f, 1f), $"Collision: {entity.CollisionShape.Type}");
                }
                
                if (entity.Properties != null && entity.Properties.Count > 0)
                {
                    ImGui.TextColored(new Vector4(1f, 0.8f, 0.4f, 1f), $"Properties: {entity.Properties.Count}");
                }
                
                ImGui.EndTooltip();
            }
        }

        private void RenderAddEntityPopup()
        {
            if (ImGui.BeginPopup("AddEntityPopup"))
            {
                ImGui.TextColored(new Vector4(0.4f, 0.8f, 1f, 1f), "Add Entity");
                ImGui.Separator();

                string[] entityTypes = { "PlayerSpawn", "Slime", "Coin", "Chest", "Spike" };
                
                foreach (var type in entityTypes)
                {
                    string icon = GetEntityIcon(type);
                    if (ImGui.MenuItem($"{icon} {type}"))
                    {
                        AddEntity(type);
                    }
                }

                ImGui.EndPopup();
            }
        }

        private void AddEntity(string type)
        {
            var newEntity = new EntityData
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                X = 160,
                Y = 90,
                Properties = new Dictionary<string, object>()
            };

            // Add default collision for certain types
            if (type == "PlayerSpawn" || type == "Slime")
            {
                newEntity.CollisionShape = new CollisionShape
                {
                    Type = "box",
                    Width = 16,
                    Height = 24
                };
            }

            _currentScene.Entities.Add(newEntity);
            _selectedEntityIndex = _currentScene.Entities.Count - 1;
            _onEntitySelected?.Invoke(_selectedEntityIndex);
            _onSceneModified?.Invoke();
        }

        private void DuplicateEntity(int index)
        {
            var original = _currentScene.Entities[index];
            var duplicate = new EntityData
            {
                Id = Guid.NewGuid().ToString(),
                Type = original.Type,
                X = original.X + 20,
                Y = original.Y + 20,
                Properties = new Dictionary<string, object>(original.Properties),
                Sprite = original.Sprite,
                Animations = original.Animations != null ? new Dictionary<string, List<string>>(original.Animations) : null
            };

            if (original.CollisionShape != null)
            {
                duplicate.CollisionShape = new CollisionShape
                {
                    Type = original.CollisionShape.Type,
                    OffsetX = original.CollisionShape.OffsetX,
                    OffsetY = original.CollisionShape.OffsetY,
                    Width = original.CollisionShape.Width,
                    Height = original.CollisionShape.Height,
                    Radius = original.CollisionShape.Radius
                };
            }

            if (original.SpriteData != null)
            {
                duplicate.SpriteData = new SpriteData
                {
                    TexturePath = original.SpriteData.TexturePath,
                    OriginX = original.SpriteData.OriginX,
                    OriginY = original.SpriteData.OriginY,
                    ScaleX = original.SpriteData.ScaleX,
                    ScaleY = original.SpriteData.ScaleY
                };
            }

            _currentScene.Entities.Add(duplicate);
            _selectedEntityIndex = _currentScene.Entities.Count - 1;
            _onEntitySelected?.Invoke(_selectedEntityIndex);
            _onSceneModified?.Invoke();
        }

        private string GetEntityIcon(string type)
        {
            switch (type)
            {
                case "PlayerSpawn": return "👤";
                case "Slime": return "🟢";
                case "Coin": return "🪙";
                case "Chest": return "📦";
                case "Spike": return "⚠️";
                default: return "⬜";
            }
        }

        private Vector4 GetEntityColor(string type)
        {
            switch (type)
            {
                case "PlayerSpawn": return new Vector4(0.4f, 0.8f, 1f, 1f);    // Cyan
                case "Slime": return new Vector4(0.4f, 1f, 0.4f, 1f);          // Green
                case "Coin": return new Vector4(1f, 0.8f, 0.2f, 1f);           // Gold
                case "Chest": return new Vector4(0.8f, 0.6f, 0.3f, 1f);        // Brown
                case "Spike": return new Vector4(1f, 0.4f, 0.4f, 1f);          // Red
                default: return new Vector4(0.8f, 0.8f, 0.8f, 1f);             // Gray
            }
        }

        public EntityData GetSelectedEntity()
        {
            if (_selectedEntityIndex >= 0 && _selectedEntityIndex < _currentScene?.Entities.Count)
                return _currentScene.Entities[_selectedEntityIndex];
            return null;
        }
    }
}
