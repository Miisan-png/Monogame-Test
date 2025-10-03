using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Snow.Engine
{
    public class SceneData
    {
        public string Name { get; set; }
        public string Tilemap { get; set; }
        public string Tileset { get; set; }
        public string BackgroundColor { get; set; }
        public List<EntityData> Entities { get; set; }
        public List<ParticleEmitterData> ParticleEmitters { get; set; }
        public List<LightData> Lights { get; set; }
        public List<AudioSourceData> AudioSources { get; set; }
        public CameraSettingsData CameraSettings { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public List<TriggerData> Triggers { get; set; }
        public List<SpawnPointData> SpawnPoints { get; set; }

        public SceneData()
        {
            Entities = new List<EntityData>();
            ParticleEmitters = new List<ParticleEmitterData>();
            Lights = new List<LightData>();
            AudioSources = new List<AudioSourceData>();
            Triggers = new List<TriggerData>();
            SpawnPoints = new List<SpawnPointData>();
            Properties = new Dictionary<string, object>();
        }
    }

    public class EntityData
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public string Sprite { get; set; }
        public Dictionary<string, List<string>> Animations { get; set; }
        public CollisionShape CollisionShape { get; set; }
        public SpriteData SpriteData { get; set; }

        public EntityData()
        {
            Properties = new Dictionary<string, object>();
            Animations = new Dictionary<string, List<string>>();
        }
    }

    public class CollisionShape
    {
        public string Type { get; set; }
        public float OffsetX { get; set; }
        public float OffsetY { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public float Radius { get; set; }

        public CollisionShape()
        {
            Type = "box";
            Width = 16;
            Height = 24;
        }
    }

    public class SpriteData
    {
        public string TexturePath { get; set; }
        public float OriginX { get; set; }
        public float OriginY { get; set; }
        public float ScaleX { get; set; }
        public float ScaleY { get; set; }

        public SpriteData()
        {
            ScaleX = 1f;
            ScaleY = 1f;
        }
    }

    public class ParticleEmitterData
    {
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string Type { get; set; }
        public string ParticleTexture { get; set; }
        public int EmissionRate { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public ParticleEmitterData()
        {
            Properties = new Dictionary<string, object>();
        }
    }

    public class LightData
    {
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Radius { get; set; }
        public string Color { get; set; }
        public float Intensity { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public LightData()
        {
            Properties = new Dictionary<string, object>();
            Type = "point";
            Intensity = 1.0f;
        }
    }

    public class AudioSourceData
    {
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string AudioFile { get; set; }
        public float Volume { get; set; }
        public bool Loop { get; set; }
        public bool AutoPlay { get; set; }
        public float MinDistance { get; set; }
        public float MaxDistance { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public AudioSourceData()
        {
            Properties = new Dictionary<string, object>();
            Volume = 1.0f;
            Loop = false;
            AutoPlay = false;
            MinDistance = 50f;
            MaxDistance = 300f;
        }
    }

    public class CameraSettingsData
    {
        public int RoomWidth { get; set; }
        public int RoomHeight { get; set; }
        public string FollowMode { get; set; }
        public float SmoothSpeed { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public CameraSettingsData()
        {
            Properties = new Dictionary<string, object>();
            RoomWidth = 320;
            RoomHeight = 180;
            FollowMode = "room";
            SmoothSpeed = 0f;
        }
    }

    public class TriggerData
    {
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public float Width { get; set; }
        public float Height { get; set; }
        public string TriggerType { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public TriggerData()
        {
            Properties = new Dictionary<string, object>();
            Width = 32f;
            Height = 32f;
        }
    }

    public class SpawnPointData
    {
        public string Id { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
        public string EntityType { get; set; }
        public float SpawnDelay { get; set; }
        public int MaxSpawns { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public SpawnPointData()
        {
            Properties = new Dictionary<string, object>();
            SpawnDelay = 0f;
            MaxSpawns = -1;
        }
    }

    public static class SceneParser
    {
        public static SceneData ParseScene(string filePath)
        {
            if (!File.Exists(filePath))
            {
                throw new FileNotFoundException($"Scene file not found: {filePath}");
            }

            var sceneData = new SceneData();
            var lines = File.ReadAllLines(filePath);
            
            string currentSection = null;
            object currentObject = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    AddCurrentObject(sceneData, currentSection, currentObject);
                    currentSection = line.Substring(1, line.Length - 2);
                    currentObject = CreateObject(currentSection);
                    continue;
                }

                if (line.Contains("=") && currentObject != null)
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    SetProperty(currentObject, currentSection, key, value);
                }
                else if (currentSection == "scene" && line.Contains("="))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();
                    ParseSceneProperty(sceneData, key, value);
                }
            }

            AddCurrentObject(sceneData, currentSection, currentObject);
            return sceneData;
        }

        private static object CreateObject(string section)
        {
            if (section.StartsWith("entity")) return new EntityData { Id = ExtractId(section) };
            if (section.StartsWith("particle_emitter")) return new ParticleEmitterData { Id = ExtractId(section) };
            if (section.StartsWith("light")) return new LightData { Id = ExtractId(section) };
            if (section.StartsWith("audio")) return new AudioSourceData { Id = ExtractId(section) };
            if (section.StartsWith("trigger")) return new TriggerData { Id = ExtractId(section) };
            if (section.StartsWith("spawn_point")) return new SpawnPointData { Id = ExtractId(section) };
            if (section == "camera") return new CameraSettingsData();
            return null;
        }

        private static void AddCurrentObject(SceneData sceneData, string section, object obj)
        {
            if (obj == null) return;

            if (obj is EntityData entity) sceneData.Entities.Add(entity);
            else if (obj is ParticleEmitterData emitter) sceneData.ParticleEmitters.Add(emitter);
            else if (obj is LightData light) sceneData.Lights.Add(light);
            else if (obj is AudioSourceData audio) sceneData.AudioSources.Add(audio);
            else if (obj is TriggerData trigger) sceneData.Triggers.Add(trigger);
            else if (obj is SpawnPointData spawn) sceneData.SpawnPoints.Add(spawn);
            else if (obj is CameraSettingsData camera) sceneData.CameraSettings = camera;
        }

        private static void SetProperty(object obj, string section, string key, string value)
        {
            if (obj is EntityData entity) ParseEntityProperty(entity, key, value);
            else if (obj is ParticleEmitterData emitter) ParseEmitterProperty(emitter, key, value);
            else if (obj is LightData light) ParseLightProperty(light, key, value);
            else if (obj is AudioSourceData audio) ParseAudioProperty(audio, key, value);
            else if (obj is TriggerData trigger) ParseTriggerProperty(trigger, key, value);
            else if (obj is SpawnPointData spawn) ParseSpawnPointProperty(spawn, key, value);
            else if (obj is CameraSettingsData camera) ParseCameraProperty(camera, key, value);
        }

        private static void ParseSceneProperty(SceneData scene, string key, string value)
        {
            value = value.Trim('"');
            
            switch (key)
            {
                case "name": scene.Name = value; break;
                case "tilemap": scene.Tilemap = value; break;
                case "tileset": scene.Tileset = value; break;
                case "background_color": scene.BackgroundColor = value; break;
                default:
                    if (value.StartsWith("{"))
                        scene.Properties[key] = ParseProperties(value);
                    else
                        scene.Properties[key] = ParseValue(value);
                    break;
            }
        }

        private static void ParseEntityProperty(EntityData entity, string key, string value)
        {
            switch (key)
            {
                case "type": entity.Type = value.Trim('"'); break;
                case "sprite": entity.Sprite = value.Trim('"'); break;
                case "position":
                    var pos = ParseVector2(value);
                    entity.X = pos.Item1;
                    entity.Y = pos.Item2;
                    break;
                case "animations": entity.Animations = ParseDictionary(value); break;
                case "properties": entity.Properties = ParseProperties(value); break;
                case "collision_shape": entity.CollisionShape = ParseCollisionShape(value); break;
                case "sprite_data": entity.SpriteData = ParseSpriteData(value); break;
                default: entity.Properties[key] = ParseValue(value); break;
            }
        }

        private static CollisionShape ParseCollisionShape(string value)
        {
            var shape = new CollisionShape();
            var props = ParseProperties(value);
            
            if (props.ContainsKey("type"))
                shape.Type = props["type"].ToString();
            if (props.ContainsKey("offset_x"))
                shape.OffsetX = Convert.ToSingle(props["offset_x"]);
            if (props.ContainsKey("offset_y"))
                shape.OffsetY = Convert.ToSingle(props["offset_y"]);
            if (props.ContainsKey("width"))
                shape.Width = Convert.ToSingle(props["width"]);
            if (props.ContainsKey("height"))
                shape.Height = Convert.ToSingle(props["height"]);
            if (props.ContainsKey("radius"))
                shape.Radius = Convert.ToSingle(props["radius"]);
                
            return shape;
        }

        private static SpriteData ParseSpriteData(string value)
        {
            var sprite = new SpriteData();
            var props = ParseProperties(value);
            
            if (props.ContainsKey("texture"))
                sprite.TexturePath = props["texture"].ToString();
            if (props.ContainsKey("origin_x"))
                sprite.OriginX = Convert.ToSingle(props["origin_x"]);
            if (props.ContainsKey("origin_y"))
                sprite.OriginY = Convert.ToSingle(props["origin_y"]);
            if (props.ContainsKey("scale_x"))
                sprite.ScaleX = Convert.ToSingle(props["scale_x"]);
            if (props.ContainsKey("scale_y"))
                sprite.ScaleY = Convert.ToSingle(props["scale_y"]);
                
            return sprite;
        }

        private static void ParseEmitterProperty(ParticleEmitterData emitter, string key, string value)
        {
            switch (key)
            {
                case "position":
                    var pos = ParseVector2(value);
                    emitter.X = pos.Item1;
                    emitter.Y = pos.Item2;
                    break;
                case "type": emitter.Type = value.Trim('"'); break;
                case "particle_texture": emitter.ParticleTexture = value.Trim('"'); break;
                case "emission_rate": emitter.EmissionRate = int.Parse(value); break;
                case "properties": emitter.Properties = ParseProperties(value); break;
                default: emitter.Properties[key] = ParseValue(value); break;
            }
        }

        private static void ParseLightProperty(LightData light, string key, string value)
        {
            switch (key)
            {
                case "position":
                    var pos = ParseVector2(value);
                    light.X = pos.Item1;
                    light.Y = pos.Item2;
                    break;
                case "radius": light.Radius = float.Parse(value); break;
                case "color": light.Color = value.Trim('"'); break;
                case "intensity": light.Intensity = float.Parse(value); break;
                case "type": light.Type = value.Trim('"'); break;
                case "properties": light.Properties = ParseProperties(value); break;
                default: light.Properties[key] = ParseValue(value); break;
            }
        }

        private static void ParseAudioProperty(AudioSourceData audio, string key, string value)
        {
            switch (key)
            {
                case "position":
                    var pos = ParseVector2(value);
                    audio.X = pos.Item1;
                    audio.Y = pos.Item2;
                    break;
                case "audio_file": audio.AudioFile = value.Trim('"'); break;
                case "volume": audio.Volume = float.Parse(value); break;
                case "loop": audio.Loop = bool.Parse(value); break;
                case "autoplay": audio.AutoPlay = bool.Parse(value); break;
                case "min_distance": audio.MinDistance = float.Parse(value); break;
                case "max_distance": audio.MaxDistance = float.Parse(value); break;
                case "properties": audio.Properties = ParseProperties(value); break;
                default: audio.Properties[key] = ParseValue(value); break;
            }
        }

        private static void ParseTriggerProperty(TriggerData trigger, string key, string value)
        {
            switch (key)
            {
                case "position":
                    var pos = ParseVector2(value);
                    trigger.X = pos.Item1;
                    trigger.Y = pos.Item2;
                    break;
                case "size":
                    var size = ParseVector2(value);
                    trigger.Width = size.Item1;
                    trigger.Height = size.Item2;
                    break;
                case "width": trigger.Width = float.Parse(value); break;
                case "height": trigger.Height = float.Parse(value); break;
                case "trigger_type": trigger.TriggerType = value.Trim('"'); break;
                case "action": trigger.Action = value.Trim('"'); break;
                case "properties": trigger.Properties = ParseProperties(value); break;
                default: trigger.Properties[key] = ParseValue(value); break;
            }
        }

        private static void ParseSpawnPointProperty(SpawnPointData spawn, string key, string value)
        {
            switch (key)
            {
                case "position":
                    var pos = ParseVector2(value);
                    spawn.X = pos.Item1;
                    spawn.Y = pos.Item2;
                    break;
                case "entity_type": spawn.EntityType = value.Trim('"'); break;
                case "spawn_delay": spawn.SpawnDelay = float.Parse(value); break;
                case "max_spawns": spawn.MaxSpawns = int.Parse(value); break;
                case "properties": spawn.Properties = ParseProperties(value); break;
                default: spawn.Properties[key] = ParseValue(value); break;
            }
        }

        private static void ParseCameraProperty(CameraSettingsData camera, string key, string value)
        {
            switch (key)
            {
                case "room_width": camera.RoomWidth = int.Parse(value); break;
                case "room_height": camera.RoomHeight = int.Parse(value); break;
                case "follow_mode": camera.FollowMode = value.Trim('"'); break;
                case "smooth_speed": camera.SmoothSpeed = float.Parse(value); break;
                case "properties": camera.Properties = ParseProperties(value); break;
                default: camera.Properties[key] = ParseValue(value); break;
            }
        }

        private static (float, float) ParseVector2(string value)
        {
            var parts = value.Split(',');
            float x = float.Parse(parts[0].Trim());
            float y = float.Parse(parts[1].Trim());
            return (x, y);
        }

        private static Dictionary<string, List<string>> ParseDictionary(string value)
        {
            var result = new Dictionary<string, List<string>>();
            
            value = value.Trim('{', '}').Trim();
            if (string.IsNullOrEmpty(value))
                return result;

            var inQuotes = false;
            var inBrackets = false;
            var currentToken = "";
            var tokens = new List<string>();

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    currentToken += c;
                }
                else if (c == '[')
                {
                    inBrackets = true;
                    currentToken += c;
                }
                else if (c == ']')
                {
                    inBrackets = false;
                    currentToken += c;
                }
                else if (c == ',' && !inQuotes && !inBrackets)
                {
                    tokens.Add(currentToken.Trim());
                    currentToken = "";
                }
                else
                {
                    currentToken += c;
                }
            }
            
            if (!string.IsNullOrEmpty(currentToken))
                tokens.Add(currentToken.Trim());

            foreach (var token in tokens)
            {
                var colonIndex = token.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = token.Substring(0, colonIndex).Trim().Trim('"');
                    var arrayPart = token.Substring(colonIndex + 1).Trim().Trim('[', ']');
                    
                    var items = arrayPart.Split(',')
                        .Select(s => s.Trim().Trim('"'))
                        .ToList();
                    
                    result[key] = items;
                }
            }

            return result;
        }

        private static Dictionary<string, object> ParseProperties(string value)
        {
            var result = new Dictionary<string, object>();
            
            value = value.Trim('{', '}').Trim();
            if (string.IsNullOrEmpty(value))
                return result;

            var inQuotes = false;
            var currentToken = "";
            var tokens = new List<string>();

            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                
                if (c == '"')
                {
                    inQuotes = !inQuotes;
                    currentToken += c;
                }
                else if (c == ',' && !inQuotes)
                {
                    tokens.Add(currentToken.Trim());
                    currentToken = "";
                }
                else
                {
                    currentToken += c;
                }
            }
            
            if (!string.IsNullOrEmpty(currentToken))
                tokens.Add(currentToken.Trim());

            foreach (var token in tokens)
            {
                var colonIndex = token.IndexOf(':');
                if (colonIndex > 0)
                {
                    var key = token.Substring(0, colonIndex).Trim().Trim('"');
                    var val = token.Substring(colonIndex + 1).Trim();
                    result[key] = ParseValue(val);
                }
            }

            return result;
        }

        private static object ParseValue(string value)
        {
            value = value.Trim('"');
            
            if (int.TryParse(value, out int intVal))
                return intVal;
            else if (float.TryParse(value, out float floatVal))
                return floatVal;
            else if (bool.TryParse(value, out bool boolVal))
                return boolVal;
            else
                return value;
        }

        private static string ExtractId(string section)
        {
            var startIndex = section.IndexOf("id=\"");
            if (startIndex == -1)
                return Guid.NewGuid().ToString();
            
            startIndex += 4;
            var endIndex = section.IndexOf("\"", startIndex);
            
            if (endIndex == -1)
                return Guid.NewGuid().ToString();
            
            return section.Substring(startIndex, endIndex - startIndex);
        }
    }
}
