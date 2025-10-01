using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Snow.Engine
{
    public class GlowOrb
    {
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public Color Color { get; set; }
        public float Intensity { get; set; }
        public bool IsActive { get; set; }
        
        private float _pulseTimer;
        private float _pulseSpeed;
        private float _baseIntensity;

        public GlowOrb(Vector2 position, float radius, Color color, float intensity = 2.0f, float pulseSpeed = 1.0f)
        {
            Position = position;
            Radius = radius;
            Color = color;
            Intensity = intensity;
            _baseIntensity = intensity;
            _pulseSpeed = pulseSpeed;
            _pulseTimer = 0f;
            IsActive = true;
        }

        public void Update(float deltaTime)
        {
            _pulseTimer += deltaTime * _pulseSpeed;
            float pulse = (float)Math.Sin(_pulseTimer) * 0.3f + 1.0f;
            Intensity = _baseIntensity * pulse;
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D glowTexture)
        {
            if (!IsActive) return;

            float scale = Radius / (glowTexture.Width / 2f);
            Color drawColor = Color * Intensity;

            spriteBatch.Draw(
                glowTexture,
                Position,
                null,
                drawColor,
                0f,
                new Vector2(glowTexture.Width / 2f, glowTexture.Height / 2f),
                scale,
                SpriteEffects.None,
                0f
            );
        }
    }

    public class GlowSystem
    {
        private Texture2D _glowTexture;
        private Texture2D _blobTexture;
        private List<GlowOrb> _orbs;

        public GlowSystem(GraphicsDevice graphicsDevice)
        {
            _orbs = new List<GlowOrb>();
            CreateGlowTextures(graphicsDevice);
        }

        private void CreateGlowTextures(GraphicsDevice graphicsDevice)
        {
            int size = 64;
            _glowTexture = new Texture2D(graphicsDevice, size, size);
            Color[] glowData = new Color[size * size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size / 2f) / (size / 2f);
                    float dy = (y - size / 2f) / (size / 2f);
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    
                    float alpha = Math.Max(0, 1.0f - distance);
                    alpha = (float)Math.Pow(alpha, 0.5f);
                    
                    glowData[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            _glowTexture.SetData(glowData);

            int blobSize = 96;
            _blobTexture = new Texture2D(graphicsDevice, blobSize, blobSize);
            Color[] blobData = new Color[blobSize * blobSize];
            
            for (int y = 0; y < blobSize; y++)
            {
                for (int x = 0; x < blobSize; x++)
                {
                    float dx = (x - blobSize / 2f) / (blobSize / 2f);
                    float dy = (y - blobSize / 2f) / (blobSize / 2f);
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    
                    float alpha = Math.Max(0, 1.0f - distance);
                    alpha = (float)Math.Pow(alpha, 1.2f);
                    
                    blobData[y * blobSize + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            _blobTexture.SetData(blobData);
        }

        public void AddOrb(Vector2 position, float radius, Color color, float intensity = 2.0f, float pulseSpeed = 1.0f)
        {
            _orbs.Add(new GlowOrb(position, radius, color, intensity, pulseSpeed));
        }

        public void AddBlob(Vector2 position, float radius, Color color, float intensity = 1.5f)
        {
            _orbs.Add(new GlowOrb(position, radius, color, intensity, 0.5f));
        }

        public void Update(float deltaTime)
        {
            foreach (var orb in _orbs)
            {
                orb.Update(deltaTime);
            }
        }

        public void DrawOrbs(SpriteBatch spriteBatch)
        {
            foreach (var orb in _orbs)
            {
                orb.Draw(spriteBatch, _glowTexture);
            }
        }

        public void Clear()
        {
            _orbs.Clear();
        }

        public void Dispose()
        {
            _glowTexture?.Dispose();
            _blobTexture?.Dispose();
        }
    }
}
