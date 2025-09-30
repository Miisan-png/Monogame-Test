using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Snow.Engine
{
    public class Particle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Color Color;
        public float Life;
        public float MaxLife;
        public float Size;
        public bool Active;
    }

    public class ParticleSystem
    {
        private Particle[] _particles;
        private int _particleCount;
        private Texture2D _particleTexture;
        private SpriteBatch _spriteBatch;
        private Random _random;

        public ParticleSystem(GraphicsDevice graphicsDevice, int maxParticles)
        {
            _particles = new Particle[maxParticles];
            for (int i = 0; i < maxParticles; i++)
            {
                _particles[i] = new Particle();
            }

            _particleCount = 0;
            _random = new Random();

            _particleTexture = new Texture2D(graphicsDevice, 1, 1);
            _particleTexture.SetData(new[] { Color.White });

            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void Emit(Vector2 position, Vector2 velocity, Color color, float life, float size)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (!_particles[i].Active)
                {
                    _particles[i].Position = position;
                    _particles[i].Velocity = velocity;
                    _particles[i].Color = color;
                    _particles[i].Life = life;
                    _particles[i].MaxLife = life;
                    _particles[i].Size = size;
                    _particles[i].Active = true;
                    _particleCount++;
                    break;
                }
            }
        }

        public void EmitBurst(Vector2 position, int count, float speedMin, float speedMax, Color color, float life, float size)
        {
            for (int i = 0; i < count; i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                float speed = (float)(_random.NextDouble() * (speedMax - speedMin) + speedMin);
                Vector2 velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                Emit(position, velocity, color, life, size);
            }
        }

        public void EmitDust(Vector2 position, Vector2 direction, int count, Color color)
        {
            for (int i = 0; i < count; i++)
            {
                float spreadX = (float)(_random.NextDouble() - 0.5) * 20f;
                float spreadY = (float)(_random.NextDouble() - 0.5) * 5f;
                Vector2 vel = direction * (float)(_random.NextDouble() * 30f + 20f);
                vel.X += spreadX;
                vel.Y += spreadY;
                
                float life = (float)(_random.NextDouble() * 0.3 + 0.2);
                Emit(position, vel, color, life, 1f);
            }
        }

        public void Update(float deltaTime)
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i].Active)
                {
                    _particles[i].Life -= deltaTime;
                    if (_particles[i].Life <= 0)
                    {
                        _particles[i].Active = false;
                        _particleCount--;
                        continue;
                    }

                    _particles[i].Position += _particles[i].Velocity * deltaTime;
                    _particles[i].Velocity.Y += 200f * deltaTime;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        {
            if (_particleCount == 0) return;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null,
                null,
                transformMatrix
            );

            for (int i = 0; i < _particles.Length; i++)
            {
                if (_particles[i].Active)
                {
                    float alpha = _particles[i].Life / _particles[i].MaxLife;
                    Color drawColor = _particles[i].Color * alpha;

                    spriteBatch.Draw(
                        _particleTexture,
                        _particles[i].Position,
                        null,
                        drawColor,
                        0f,
                        Vector2.Zero,
                        _particles[i].Size,
                        SpriteEffects.None,
                        0f
                    );
                }
            }

            spriteBatch.End();
        }

        public void Clear()
        {
            for (int i = 0; i < _particles.Length; i++)
            {
                _particles[i].Active = false;
            }
            _particleCount = 0;
        }

        public void Dispose()
        {
            _particleTexture?.Dispose();
            _spriteBatch?.Dispose();
        }
    }
}
