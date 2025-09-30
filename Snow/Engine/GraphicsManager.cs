using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;
using System.IO;

namespace Snow
{
    public class GraphicsManager
    {
        private GraphicsDevice _graphicsDevice;
        private Dictionary<string, Texture2D> _textures;
        private SpriteBatch _spriteBatch;

        public SpriteBatch SpriteBatch => _spriteBatch;

        public GraphicsManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _textures = new Dictionary<string, Texture2D>();
            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public Texture2D LoadTexture(string name, string path)
        {
            if (_textures.ContainsKey(name))
            {
                return _textures[name];
            }

            if (!File.Exists(path))
            {
                throw new FileNotFoundException($"Texture not found: {path}");
            }

            Texture2D texture;
            using (FileStream fileStream = new FileStream(path, FileMode.Open))
            {
                texture = Texture2D.FromStream(_graphicsDevice, fileStream);
            }

            _textures.Add(name, texture);
            return texture;
        }

        public Texture2D GetTexture(string name)
        {
            if (_textures.TryGetValue(name, out Texture2D texture))
            {
                return texture;
            }
            throw new KeyNotFoundException($"Texture '{name}' not loaded");
        }

        public void UnloadTexture(string name)
        {
            if (_textures.TryGetValue(name, out Texture2D texture))
            {
                texture.Dispose();
                _textures.Remove(name);
            }
        }

        public void UnloadAll()
        {
            foreach (var texture in _textures.Values)
            {
                texture.Dispose();
            }
            _textures.Clear();
        }

        public void Dispose()
        {
            UnloadAll();
            _spriteBatch?.Dispose();
        }
    }
}
