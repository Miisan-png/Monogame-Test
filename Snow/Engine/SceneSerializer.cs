using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Snow.Engine
{
    public static class SceneSerializer
    {
        public static void SaveScene(SceneData sceneData, string filePath)
        {
            var sb = new StringBuilder();

            WriteSceneSection(sb, sceneData);
            
            if (sceneData.CameraSettings != null)
                WriteCameraSection(sb, sceneData.CameraSettings);

            foreach (var entity in sceneData.Entities)
                WriteEntitySection(sb, entity);

            foreach (var emitter in sceneData.ParticleEmitters)
                WriteParticleEmitterSection(sb, emitter);

            foreach (var light in sceneData.Lights)
                WriteLightSection(sb, light);

            foreach (var audio in sceneData.AudioSources)
                WriteAudioSection(sb, audio);

            foreach (var trigger in sceneData.Triggers)
                WriteTriggerSection(sb, trigger);

            foreach (var spawn in sceneData.SpawnPoints)
                WriteSpawnPointSection(sb, spawn);

            File.WriteAllText(filePath, sb.ToString());
        }

        private static void WriteSceneSection(StringBuilder sb, SceneData scene)
        {
            sb.AppendLine("[scene]");
            WriteProperty(sb, "name", scene.Name);
            WriteProperty(sb, "tilemap", scene.Tilemap);
            WriteProperty(sb, "tileset", scene.Tileset);
            WriteProperty(sb, "background_color", scene.BackgroundColor);
            WriteProperties(sb, scene.Properties);
            sb.AppendLine();
        }

        private static void WriteCameraSection(StringBuilder sb, CameraSettingsData camera)
        {
            sb.AppendLine("[camera]");
            WriteProperty(sb, "room_width", camera.RoomWidth);
            WriteProperty(sb, "room_height", camera.RoomHeight);
            WriteProperty(sb, "follow_mode", camera.FollowMode);
            WriteProperty(sb, "smooth_speed", camera.SmoothSpeed);
            WriteProperties(sb, camera.Properties);
            sb.AppendLine();
        }

        private static void WriteEntitySection(StringBuilder sb, EntityData entity)
        {
            sb.AppendLine($"[entity id=\"{entity.Id}\"]");
            WriteProperty(sb, "type", entity.Type);
            WriteProperty(sb, "position", $"{entity.X}, {entity.Y}");
            
            if (!string.IsNullOrEmpty(entity.Sprite))
                WriteProperty(sb, "sprite", entity.Sprite);
            
            if (entity.Animations != null && entity.Animations.Count > 0)
                WriteProperty(sb, "animations", FormatDictionary(entity.Animations));
            
            WriteProperties(sb, entity.Properties);
            sb.AppendLine();
        }

        private static void WriteParticleEmitterSection(StringBuilder sb, ParticleEmitterData emitter)
        {
            sb.AppendLine($"[particle_emitter id=\"{emitter.Id}\"]");
            WriteProperty(sb, "type", emitter.Type);
            WriteProperty(sb, "position", $"{emitter.X}, {emitter.Y}");
            WriteProperty(sb, "emission_rate", emitter.EmissionRate);
            
            if (!string.IsNullOrEmpty(emitter.ParticleTexture))
                WriteProperty(sb, "particle_texture", emitter.ParticleTexture);
            
            WriteProperties(sb, emitter.Properties);
            sb.AppendLine();
        }

        private static void WriteLightSection(StringBuilder sb, LightData light)
        {
            sb.AppendLine($"[light id=\"{light.Id}\"]");
            WriteProperty(sb, "position", $"{light.X}, {light.Y}");
            WriteProperty(sb, "radius", light.Radius);
            WriteProperty(sb, "color", light.Color);
            WriteProperty(sb, "intensity", light.Intensity);
            WriteProperty(sb, "type", light.Type);
            WriteProperties(sb, light.Properties);
            sb.AppendLine();
        }

        private static void WriteAudioSection(StringBuilder sb, AudioSourceData audio)
        {
            sb.AppendLine($"[audio id=\"{audio.Id}\"]");
            WriteProperty(sb, "position", $"{audio.X}, {audio.Y}");
            WriteProperty(sb, "audio_file", audio.AudioFile);
            WriteProperty(sb, "volume", audio.Volume);
            WriteProperty(sb, "loop", audio.Loop.ToString().ToLower());
            WriteProperty(sb, "autoplay", audio.AutoPlay.ToString().ToLower());
            WriteProperty(sb, "min_distance", audio.MinDistance);
            WriteProperty(sb, "max_distance", audio.MaxDistance);
            WriteProperties(sb, audio.Properties);
            sb.AppendLine();
        }

        private static void WriteTriggerSection(StringBuilder sb, TriggerData trigger)
        {
            sb.AppendLine($"[trigger id=\"{trigger.Id}\"]");
            WriteProperty(sb, "position", $"{trigger.X}, {trigger.Y}");
            WriteProperty(sb, "size", $"{trigger.Width}, {trigger.Height}");
            WriteProperty(sb, "trigger_type", trigger.TriggerType);
            WriteProperty(sb, "action", trigger.Action);
            WriteProperties(sb, trigger.Properties);
            sb.AppendLine();
        }

        private static void WriteSpawnPointSection(StringBuilder sb, SpawnPointData spawn)
        {
            sb.AppendLine($"[spawn_point id=\"{spawn.Id}\"]");
            WriteProperty(sb, "position", $"{spawn.X}, {spawn.Y}");
            WriteProperty(sb, "entity_type", spawn.EntityType);
            WriteProperty(sb, "spawn_delay", spawn.SpawnDelay);
            WriteProperty(sb, "max_spawns", spawn.MaxSpawns);
            WriteProperties(sb, spawn.Properties);
            sb.AppendLine();
        }

        private static void WriteProperty(StringBuilder sb, string key, object value)
        {
            if (value == null)
                return;

            if (value is string strValue)
            {
                if (strValue.StartsWith("#") || strValue.Contains(" ") || strValue.Contains(","))
                    sb.AppendLine($"{key} = {strValue}");
                else
                    sb.AppendLine($"{key} = \"{strValue}\"");
            }
            else if (value is float floatValue)
            {
                sb.AppendLine($"{key} = {floatValue}");
            }
            else
            {
                sb.AppendLine($"{key} = {value}");
            }
        }

        private static void WriteProperties(StringBuilder sb, Dictionary<string, object> properties)
        {
            if (properties == null || properties.Count == 0)
                return;

            var items = properties.Select(kvp => $"{kvp.Key}: {FormatValue(kvp.Value)}");
            sb.AppendLine($"properties = {{{string.Join(", ", items)}}}");
        }

        private static string FormatValue(object value)
        {
            if (value == null)
                return "null";

            if (value is string strValue)
                return $"\"{strValue}\"";
            
            if (value is bool boolValue)
                return boolValue.ToString().ToLower();

            return value.ToString();
        }

        private static string FormatDictionary(Dictionary<string, List<string>> dict)
        {
            var items = dict.Select(kvp => 
                $"\"{kvp.Key}\": [{string.Join(", ", kvp.Value.Select(v => $"\"{v}\""))}]"
            );
            return $"{{{string.Join(", ", items)}}}";
        }

        public static SceneData CreateEmptyScene(string name)
        {
            return new SceneData
            {
                Name = name,
                Tilemap = "",
                Tileset = "",
                BackgroundColor = "#000000",
                CameraSettings = new CameraSettingsData()
            };
        }

        public static void AddEntity(SceneData scene, string type, float x, float y, Dictionary<string, object> properties = null)
        {
            scene.Entities.Add(new EntityData
            {
                Id = Guid.NewGuid().ToString(),
                Type = type,
                X = x,
                Y = y,
                Properties = properties ?? new Dictionary<string, object>()
            });
        }

        public static void AddLight(SceneData scene, float x, float y, float radius, string color, float intensity = 1.0f)
        {
            scene.Lights.Add(new LightData
            {
                Id = Guid.NewGuid().ToString(),
                X = x,
                Y = y,
                Radius = radius,
                Color = color,
                Intensity = intensity
            });
        }

        public static void AddTrigger(SceneData scene, float x, float y, float width, float height, string triggerType, string action)
        {
            scene.Triggers.Add(new TriggerData
            {
                Id = Guid.NewGuid().ToString(),
                X = x,
                Y = y,
                Width = width,
                Height = height,
                TriggerType = triggerType,
                Action = action
            });
        }
    }
}
