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

    public class PhysicsParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public Vector2 Force;
        public Color Color;
        public float Size;
        public float Mass;
        public float Drag;
        public float Bounciness;
        public bool Active;
        public float GlowIntensity;
        public float PulseTimer;
        public float PulseSpeed;
    }

    public class ParticleSystem
    {
        private Particle[] _particles;
        private PhysicsParticle[] _physicsParticles;
        private int _particleCount;
        private int _physicsParticleCount;
        private Texture2D _particleTexture;
        private Texture2D _squareTexture;
        private SpriteBatch _spriteBatch;
        private Random _random;

        public ParticleSystem(GraphicsDevice graphicsDevice, int maxParticles)
        {
            _particles = new Particle[maxParticles];
            for (int i = 0; i < maxParticles; i++)
            {
                _particles[i] = new Particle();
            }

            _physicsParticles = new PhysicsParticle[500];
            for (int i = 0; i < 500; i++)
            {
                _physicsParticles[i] = new PhysicsParticle
                {
                    Mass = 1f,
                    Drag = 0.98f,
                    Bounciness = 0.6f,
                    GlowIntensity = 1f,
                    PulseSpeed = 1f
                };
            }

            _particleCount = 0;
            _physicsParticleCount = 0;
            _random = new Random();

            _particleTexture = new Texture2D(graphicsDevice, 1, 1);
            _particleTexture.SetData(new[] { Color.White });

            _squareTexture = new Texture2D(graphicsDevice, 4, 4);
            Color[] squareData = new Color[16];
            for (int i = 0; i < 16; i++)
                squareData[i] = Color.White;
            _squareTexture.SetData(squareData);

            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void EmitPhysicsParticle(Vector2 position, Vector2 velocity, Color color, float size, float mass = 1f)
        {
            for (int i = 0; i < _physicsParticles.Length; i++)
            {
                if (!_physicsParticles[i].Active)
                {
                    _physicsParticles[i].Position = position;
                    _physicsParticles[i].Velocity = velocity;
                    _physicsParticles[i].Force = Vector2.Zero;
                    _physicsParticles[i].Color = color;
                    _physicsParticles[i].Size = size;
                    _physicsParticles[i].Mass = mass;
                    _physicsParticles[i].Active = true;
                    _physicsParticles[i].GlowIntensity = 1.5f;
                    _physicsParticles[i].PulseTimer = (float)_random.NextDouble() * MathHelper.TwoPi;
                    _physicsParticles[i].PulseSpeed = 0.5f + (float)_random.NextDouble() * 1.5f;
                    _physicsParticleCount++;
                    break;
                }
            }
        }

        public void ApplyForceToPhysicsParticles(Vector2 center, float radius, Vector2 force)
        {
            for (int i = 0; i < _physicsParticles.Length; i++)
            {
                if (_physicsParticles[i].Active)
                {
                    Vector2 diff = _physicsParticles[i].Position - center;
                    float distance = diff.Length();
                    
                    if (distance < radius && distance > 0.1f)
                    {
                        float strength = 1f - (distance / radius);
                        strength = (float)Math.Pow(strength, 2);
                        Vector2 direction = diff / distance;
                        _physicsParticles[i].Force += direction * force.Length() * strength;
                    }
                }
            }
        }

        public void ApplyRadialForce(Vector2 center, float radius, float strength)
        {
            for (int i = 0; i < _physicsParticles.Length; i++)
            {
                if (_physicsParticles[i].Active)
                {
                    Vector2 diff = _physicsParticles[i].Position - center;
                    float distance = diff.Length();
                    
                    if (distance < radius && distance > 0.1f)
                    {
                        float forceMagnitude = strength * (1f - (distance / radius));
                        Vector2 direction = diff / distance;
                        _physicsParticles[i].Force += direction * forceMagnitude;
                    }
                }
            }
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

        public void Update(float deltaTime, Rectangle worldBounds, Tilemap tilemap = null)
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

            for (int i = 0; i < _physicsParticles.Length; i++)
            {
                if (_physicsParticles[i].Active)
                {
                    var p = _physicsParticles[i];
                    
                    p.PulseTimer += deltaTime * p.PulseSpeed;
                    
                    Vector2 acceleration = p.Force / p.Mass;
                    p.Velocity += acceleration * deltaTime;
                    p.Velocity *= p.Drag;
                    
                    p.Velocity.Y += 15f * deltaTime;
                    
                    Vector2 newPos = p.Position + p.Velocity * deltaTime;
                    
                    if (tilemap != null)
                    {
                        int tileX = (int)(newPos.X / tilemap.TileSize);
                        int tileY = (int)(newPos.Y / tilemap.TileSize);
                        
                        if (tilemap.IsSolid(tileX, tileY))
                        {
                            int oldTileX = (int)(p.Position.X / tilemap.TileSize);
                            int oldTileY = (int)(p.Position.Y / tilemap.TileSize);
                            
                            if (oldTileX != tileX)
                            {
                                p.Velocity.X *= -p.Bounciness;
                                newPos.X = p.Position.X;
                            }
                            
                            if (oldTileY != tileY)
                            {
                                p.Velocity.Y *= -p.Bounciness;
                                newPos.Y = p.Position.Y;
                            }
                        }
                    }
                    
                    p.Position = newPos;
                    
                    if (p.Position.X < worldBounds.Left)
                    {
                        p.Position.X = worldBounds.Left;
                        p.Velocity.X *= -p.Bounciness;
                    }
                    else if (p.Position.X > worldBounds.Right)
                    {
                        p.Position.X = worldBounds.Right;
                        p.Velocity.X *= -p.Bounciness;
                    }
                    
                    if (p.Position.Y < worldBounds.Top)
                    {
                        p.Position.Y = worldBounds.Top;
                        p.Velocity.Y *= -p.Bounciness;
                    }
                    else if (p.Position.Y > worldBounds.Bottom)
                    {
                        p.Position.Y = worldBounds.Bottom;
                        p.Velocity.Y *= -p.Bounciness;
                    }
                    
                    p.Force = Vector2.Zero;
                    
                    _physicsParticles[i] = p;
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

        public void DrawPhysicsParticles(SpriteBatch spriteBatch, Matrix? transformMatrix = null)
        {
            if (_physicsParticleCount == 0) return;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.PointClamp,
                null,
                null,
                null,
                transformMatrix
            );

            for (int i = 0; i < _physicsParticles.Length; i++)
            {
                if (_physicsParticles[i].Active)
                {
                    var p = _physicsParticles[i];
                    float pulse = (float)Math.Sin(p.PulseTimer) * 0.3f + 0.7f;
                    Color drawColor = p.Color * pulse;

                    spriteBatch.Draw(
                        _squareTexture,
                        p.Position,
                        null,
                        drawColor,
                        0f,
                        new Vector2(2, 2),
                        p.Size,
                        SpriteEffects.None,
                        0f
                    );
                }
            }

            spriteBatch.End();
        }

        public void DrawPhysicsParticlesGlow(SpriteBatch spriteBatch, Texture2D glowTexture, Matrix? transformMatrix = null)
        {
            if (_physicsParticleCount == 0) return;

            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.Additive,
                SamplerState.PointClamp,
                null,
                null,
                null,
                transformMatrix
            );

            for (int i = 0; i < _physicsParticles.Length; i++)
            {
                if (_physicsParticles[i].Active)
                {
                    var p = _physicsParticles[i];
                    float pulse = (float)Math.Sin(p.PulseTimer) * 0.4f + 1.0f;
                    Color glowColor = p.Color * p.GlowIntensity * pulse * 0.8f;

                    float glowScale = p.Size * 3f;
                    spriteBatch.Draw(
                        glowTexture,
                        p.Position,
                        null,
                        glowColor,
                        0f,
                        new Vector2(glowTexture.Width / 2, glowTexture.Height / 2),
                        glowScale,
                        SpriteEffects.None,
                        0f
                    );
                }
            }

            spriteBatch.End();
        }

        public int GetPhysicsParticleCount()
        {
            return _physicsParticleCount;
        }

        public void ClearPhysicsParticles()
        {
            for (int i = 0; i < _physicsParticles.Length; i++)
            {
                _physicsParticles[i].Active = false;
            }
            _physicsParticleCount = 0;
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
            _squareTexture?.Dispose();
            _spriteBatch?.Dispose();
        }
    }
}
