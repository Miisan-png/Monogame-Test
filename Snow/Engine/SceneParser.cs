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

        public SceneData()
        {
            Entities = new List<EntityData>();
            ParticleEmitters = new List<ParticleEmitterData>();
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

        public EntityData()
        {
            Properties = new Dictionary<string, object>();
            Animations = new Dictionary<string, List<string>>();
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
            EntityData currentEntity = null;
            ParticleEmitterData currentEmitter = null;
            Dictionary<string, List<string>> currentAnimations = null;
            Dictionary<string, object> currentProperties = null;

            for (int i = 0; i < lines.Length; i++)
            {
                string line = lines[i].Trim();

                if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#"))
                    continue;

                if (line.StartsWith("[") && line.EndsWith("]"))
                {
                    if (currentEntity != null)
                    {
                        sceneData.Entities.Add(currentEntity);
                        currentEntity = null;
                    }
                    if (currentEmitter != null)
                    {
                        sceneData.ParticleEmitters.Add(currentEmitter);
                        currentEmitter = null;
                    }

                    currentSection = line.Substring(1, line.Length - 2);
                    
                    if (currentSection.StartsWith("entity"))
                    {
                        currentEntity = new EntityData();
                        var idPart = ExtractAttribute(currentSection, "id");
                        if (idPart != null)
                            currentEntity.Id = idPart;
                    }
                    else if (currentSection.StartsWith("particle_emitter"))
                    {
                        currentEmitter = new ParticleEmitterData();
                        var idPart = ExtractAttribute(currentSection, "id");
                        if (idPart != null)
                            currentEmitter.Id = idPart;
                    }

                    continue;
                }

                if (line.Contains("="))
                {
                    var parts = line.Split(new[] { '=' }, 2);
                    string key = parts[0].Trim();
                    string value = parts[1].Trim();

                    if (currentSection == "scene")
                    {
                        ParseSceneProperty(sceneData, key, value);
                    }
                    else if (currentEntity != null)
                    {
                        ParseEntityProperty(currentEntity, key, value, ref currentAnimations, ref currentProperties);
                    }
                    else if (currentEmitter != null)
                    {
                        ParseEmitterProperty(currentEmitter, key, value, ref currentProperties);
                    }
                }
            }

            if (currentEntity != null)
                sceneData.Entities.Add(currentEntity);
            if (currentEmitter != null)
                sceneData.ParticleEmitters.Add(currentEmitter);

            return sceneData;
        }

        private static void ParseSceneProperty(SceneData scene, string key, string value)
        {
            value = value.Trim('"');
            
            switch (key)
            {
                case "name":
                    scene.Name = value;
                    break;
                case "tilemap":
                    scene.Tilemap = value;
                    break;
                case "tileset":
                    scene.Tileset = value;
                    break;
                case "background_color":
                    scene.BackgroundColor = value;
                    break;
            }
        }

        private static void ParseEntityProperty(EntityData entity, string key, string value, 
            ref Dictionary<string, List<string>> currentAnimations,
            ref Dictionary<string, object> currentProperties)
        {
            switch (key)
            {
                case "type":
                    entity.Type = value.Trim('"');
                    break;
                case "sprite":
                    entity.Sprite = value.Trim('"');
                    break;
                case "position":
                    var pos = ParseVector2(value);
                    entity.X = pos.Item1;
                    entity.Y = pos.Item2;
                    break;
                case "animations":
                    entity.Animations = ParseDictionary(value);
                    break;
                case "properties":
                    entity.Properties = ParseProperties(value);
                    break;
            }
        }

        private static void ParseEmitterProperty(ParticleEmitterData emitter, string key, string value,
            ref Dictionary<string, object> currentProperties)
        {
            switch (key)
            {
                case "position":
                    var pos = ParseVector2(value);
                    emitter.X = pos.Item1;
                    emitter.Y = pos.Item2;
                    break;
                case "type":
                    emitter.Type = value.Trim('"');
                    break;
                case "particle_texture":
                    emitter.ParticleTexture = value.Trim('"');
                    break;
                case "emission_rate":
                    emitter.EmissionRate = int.Parse(value);
                    break;
                case "properties":
                    emitter.Properties = ParseProperties(value);
                    break;
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
                    var val = token.Substring(colonIndex + 1).Trim().Trim('"');
                    
                    if (int.TryParse(val, out int intVal))
                        result[key] = intVal;
                    else if (float.TryParse(val, out float floatVal))
                        result[key] = floatVal;
                    else if (bool.TryParse(val, out bool boolVal))
                        result[key] = boolVal;
                    else
                        result[key] = val;
                }
            }

            return result;
        }

        private static string ExtractAttribute(string section, string attribute)
        {
            var startIndex = section.IndexOf(attribute + "=\"");
            if (startIndex == -1)
                return null;
            
            startIndex += attribute.Length + 2;
            var endIndex = section.IndexOf("\"", startIndex);
            
            if (endIndex == -1)
                return null;
            
            return section.Substring(startIndex, endIndex - startIndex);
        }
    }
}
