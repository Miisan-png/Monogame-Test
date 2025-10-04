using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using System.Collections.Generic;
using System;

namespace Snow.Game
{
    public class GhostTrail
    {
        public Vector2 Position;
        public Texture2D Texture;
        public float Alpha;
        public float Life;
        public bool FlipX;
        public Color Tint;
    }

    public class DashTrailParticle
    {
        public Vector2 Position;
        public Vector2 Velocity;
        public float Alpha;
        public float Life;
        public float MaxLife;
    }

    public class SpriteParticle
    {
        public Vector2 Position;
        public List<Texture2D> Frames;
        public int CurrentFrame;
        public float FrameTimer;
        public float FrameTime;
        public bool Active;
        public float Rotation;
        public Vector2 Origin;
    }

    public class Player : Actor
    {
        private PhysicsComponent _physics;
        private InputManager _input;
        private AnimatedSprite _sprite;
        private GraphicsManager _graphics;
        private Camera _camera;
        public Vector2 Size { get; set; }
        private bool _wasGrounded;
        private bool _wasDashing;
        private List<GhostTrail> _ghostTrails;
        private float _ghostSpawnTimer;
        private Color _dashColor = new Color(100, 200, 255, 255);
        private Color _normalColor = Color.White;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private CollisionShape _collisionShape;
        private int _wallDirection;
        private bool _canWallJump;
        private float _wallJumpCooldown;
        private float _stamina;
        private float _maxStamina = 100f;
        private float _staminaRegenRate = 60f;
        private bool _isClimbing;
        private float _climbSpeed = 80f;
        private bool _isDead = false;
        private Vector2 _respawnPosition;
        private List<SpriteParticle> _spriteParticles;
        private List<Texture2D> _jumpParticles;
        private List<Texture2D> _landParticles;
        private List<Texture2D> _dashLineParticles;
        private List<DashTrailParticle> _dashTrail;
        private Texture2D _dashPixel;
        private float _dashTrailSpawnTimer;
        private Color _dashTrailColor = new Color(100, 200, 255);
        private float _lastDashDirection;
        private bool _dashJustEnded;
        private bool _isFrozen;
        private float _freezeTimer;

        public float Stamina => _stamina;
        public float MaxStamina => _maxStamina;
        public bool IsWallSliding => _physics.IsWallSliding;
        public bool IsClimbing => _isClimbing;
        public bool IsDead => _isDead;

        public Player(Vector2 position, GraphicsDevice graphicsDevice, InputManager input, GraphicsManager graphics, ParticleSystem particles, Camera camera) : base(position)
        {
            Size = new Vector2(16, 24);
            _input = input;
            _graphics = graphics;
            _camera = camera;
            _physics = new PhysicsComponent();
            _physics.IsGrounded = true;
            _sprite = new AnimatedSprite();
            LoadAnimations();
            _wasGrounded = false;
            _wasDashing = false;
            _ghostTrails = new List<GhostTrail>();
            _ghostSpawnTimer = 0f;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _wallDirection = 0;
            _canWallJump = false;
            _wallJumpCooldown = 0f;
            _stamina = _maxStamina;
            _isClimbing = false;
            _respawnPosition = position;
            _spriteParticles = new List<SpriteParticle>();
            _dashTrail = new List<DashTrailParticle>();
            _dashTrailSpawnTimer = 0f;
            _lastDashDirection = 0f;
            _dashJustEnded = false;
            _isFrozen = false;
            _freezeTimer = 0f;

            LoadParticleTextures(graphicsDevice);
            
            _collisionShape = new CollisionShape
            {
                Type = "box",
                OffsetX = 0,
                OffsetY = 0,
                Width = 16,
                Height = 24,
                Radius = 0
            };
        }

        private void LoadParticleTextures(GraphicsDevice device)
        {
            _jumpParticles = new List<Texture2D>();
            _landParticles = new List<Texture2D>();
            _dashLineParticles = new List<Texture2D>();

            for (int i = 1; i <= 4; i++)
            {
                try
                {
                    _jumpParticles.Add(_graphics.LoadTexture($"jump_p{i}", $"assets/particles/jump_p{i}.png"));
                }
                catch { }
            }

            for (int i = 1; i <= 6; i++)
            {
                try
                {
                    _landParticles.Add(_graphics.LoadTexture($"land_p{i}", $"assets/particles/land_p{i}.png"));
                }
                catch { }
            }

            for (int i = 1; i <= 4; i++)
            {
                try
                {
                    _dashLineParticles.Add(_graphics.LoadTexture($"dash_line_p{i}", $"assets/particles/dash_line_p{i}.png"));
                }
                catch { }
            }

            _dashPixel = new Texture2D(device, 1, 1);
            _dashPixel.SetData(new[] { Color.White });
        }

        public void SetRespawnPosition(Vector2 position)
        {
            _respawnPosition = position;
        }

        public void Die()
        {
            if (_isDead) return;
            
            _isDead = true;
            IsActive = false;
            _camera.Shake(8f, 0.3f);
        }

        public void Respawn()
        {
            _isDead = false;
            IsActive = true;
            Position = _respawnPosition;
            Velocity = Vector2.Zero;
            _physics.Velocity = Vector2.Zero;
            _stamina = _maxStamina;
            _physics.IsGrounded = false;
        }

        public void Freeze(float duration)
        {
            _isFrozen = true;
            _freezeTimer = duration;
        }

        public void Unfreeze()
        {
            _isFrozen = false;
            _freezeTimer = 0f;
        }

        public void ApplyCollisionShape(CollisionShape shape)
        {
            if (shape != null)
            {
                _collisionShape = new CollisionShape
                {
                    Type = shape.Type,
                    OffsetX = shape.OffsetX,
                    OffsetY = shape.OffsetY,
                    Width = shape.Width,
                    Height = shape.Height,
                    Radius = shape.Radius
                };
                Size = new Vector2(shape.Width, shape.Height);
            }
        }

        public void ApplySpriteData(SpriteData spriteData)
        {
            if (spriteData != null)
            {
                _sprite.Origin = new Vector2(spriteData.OriginX, spriteData.OriginY);
                Scale = new Vector2(spriteData.ScaleX, spriteData.ScaleY);
            }
        }

        public Rectangle GetCollisionBox()
        {
            return new Rectangle(
                (int)(Position.X + _collisionShape.OffsetX),
                (int)(Position.Y + _collisionShape.OffsetY),
                (int)_collisionShape.Width,
                (int)_collisionShape.Height
            );
        }

        private void LoadAnimations()
        {
            Animation idle = new Animation("idle", 0.1f, true);
            for (int i = 1; i <= 6; i++)
                idle.Frames.Add(_graphics.LoadTexture($"player_idle_{i}", $"assets/Main/Player/Player{i}.png"));
            _sprite.AddAnimation(idle);

            Animation walk = new Animation("walk", 0.08f, true);
            for (int i = 7; i <= 10; i++)
                walk.Frames.Add(_graphics.LoadTexture($"player_walk_{i}", $"assets/Main/Player/Player{i}.png"));
            _sprite.AddAnimation(walk);

            Animation wallSlide = new Animation("wallSlide", 0.15f, true);
            for (int i = 1; i <= 3; i++)
                wallSlide.Frames.Add(_graphics.LoadTexture($"player_wallslide_{i}", $"assets/Main/Player/Player{i}.png"));
            _sprite.AddAnimation(wallSlide);

            Animation climb = new Animation("climb", 0.12f, true);
            for (int i = 4; i <= 6; i++)
                climb.Frames.Add(_graphics.LoadTexture($"player_climb_{i}", $"assets/Main/Player/Player{i}.png"));
            _sprite.AddAnimation(climb);

            _sprite.Play("idle");
        }

        public override void Update(GameTime gameTime)
        {
            if (_isDead) return;

            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

            if (_isFrozen)
            {
                _freezeTimer -= dt;
                if (_freezeTimer <= 0f)
                {
                    _isFrozen = false;
                    _freezeTimer = 0f;
                }
                _sprite.Update(dt);
                return;
            }

            float moveX = _input.GetAxisHorizontal();
            float moveY = _input.GetAxisVertical();
            bool jumpPressed = _input.IsKeyPressed(Keys.C);
            bool jumpReleased = _input.IsKeyReleased(Keys.C);
            bool jumpHeld = _input.IsKeyDown(Keys.C);
            bool dashPressed = _input.IsKeyPressed(Keys.X);

            UpdateTimers(dt);
            CheckWallContact();
            HandleStamina(dt, moveY, jumpHeld);
            HandleClimbing(dt, moveX, moveY, jumpHeld);

            bool canJump = (_coyoteTimer > 0f && jumpPressed) || 
                          (_physics.IsGrounded && _jumpBufferTimer > 0f) ||
                          (_canWallJump && jumpPressed && _wallJumpCooldown <= 0f);
            
            bool dashInput = dashPressed && _physics.CanDash;

            if (!_wasDashing && dashInput)
            {
                _camera.Shake(4f, 0.2f);
                SpawnDashParticles();
            }

            Vector2 wallJumpDirection = Vector2.Zero;
            if (_canWallJump && jumpPressed && _wallJumpCooldown <= 0f)
            {
                wallJumpDirection = new Vector2(-_wallDirection * 1.2f, -1.5f);
                _wallJumpCooldown = 0.3f;
                _physics.ResetCoyoteTime();
                _camera.Shake(3f, 0.15f);
            }

            if (canJump && _physics.IsGrounded && !_wasDashing)
            {
                SpawnJumpParticles();
            }

            _physics.Update(dt, moveX, moveY, canJump, jumpReleased, dashInput, wallJumpDirection);

            HandleDashEffects(dt, moveX);
            UpdateAnimations(moveX);

            if (!_wasGrounded && _physics.IsGrounded)
            {
                SpawnLandParticles();
                _camera.Shake(1f, 0.08f);
                _stamina = _maxStamina;
            }

            if (_wasDashing && !_physics.IsDashing)
            {
                _dashJustEnded = true;
                ScatterDashParticles();
            }
            else if (!_physics.IsDashing)
            {
                _dashJustEnded = false;
            }

            _wasGrounded = _physics.IsGrounded;
            _wasDashing = _physics.IsDashing;
            _sprite.Update(dt);
            Velocity = _physics.Velocity;

            UpdateSpriteParticles(dt);
            UpdateDashTrail(dt);
            UpdateGhostTrails(dt);
        }

        private void UpdateTimers(float dt)
        {
            if (_physics.IsGrounded) _coyoteTimer = 0.12f; else _coyoteTimer -= dt;
            if (_input.IsKeyPressed(Keys.C)) _jumpBufferTimer = 0.12f; else _jumpBufferTimer -= dt;
            if (_wallJumpCooldown > 0f) _wallJumpCooldown -= dt;
        }

        private void CheckWallContact()
        {
            _wallDirection = 0;
            _canWallJump = false;

            if (_physics.IsWallSliding)
            {
                float moveX = _input.GetAxisHorizontal();
                if (moveX > 0.1f) _wallDirection = 1;
                else if (moveX < -0.1f) _wallDirection = -1;
                
                if (_wallDirection != 0 && !_physics.IsGrounded)
                {
                    _canWallJump = true;
                }
            }
        }

        private void HandleStamina(float dt, float moveY, bool jumpHeld)
        {
            if (_physics.IsGrounded || !_physics.IsWallSliding)
            {
                _stamina += _staminaRegenRate * dt;
                if (_stamina > _maxStamina) _stamina = _maxStamina;
            }
        }

        private void HandleClimbing(float dt, float moveX, float moveY, bool jumpHeld)
        {
            _isClimbing = false;
            
            if (_physics.IsWallSliding && jumpHeld && _stamina > 0f && System.Math.Abs(moveY) > 0.1f)
            {
                _isClimbing = true;
                Vector2 vel = _physics.Velocity;
                vel.Y = moveY * _climbSpeed;
                _physics.Velocity = vel;
            }
        }

        private void HandleDashEffects(float dt, float moveX)
        {
            if (_physics.IsDashing)
            {
                Vector2 dashDir = _physics.DashDirection;
                _lastDashDirection = (float)Math.Atan2(dashDir.Y, dashDir.X);

                _ghostSpawnTimer += dt;
                if (_ghostSpawnTimer > 0.05f)
                {
                    SpawnGhostTrail();
                    _ghostSpawnTimer = 0f;
                }

                _dashTrailSpawnTimer += dt;
                if (_dashTrailSpawnTimer > 0.03f)
                {
                    Random rand = new Random();
                    Vector2 offset = new Vector2(
                        (float)(rand.NextDouble() - 0.5) * 10f,
                        (float)(rand.NextDouble() - 0.5) * 10f
                    );

                    Vector2 backwardVel = -dashDir * (20f + (float)rand.NextDouble() * 15f);

                    _dashTrail.Add(new DashTrailParticle
                    {
                        Position = Position + new Vector2(8, 12) + offset,
                        Velocity = backwardVel,
                        Alpha = 0.8f,
                        Life = 0.7f,
                        MaxLife = 0.7f
                    });
                    _dashTrailSpawnTimer = 0f;
                }
            }
        }

        private void ScatterDashParticles()
        {
            Random rand = new Random();
            foreach (var particle in _dashTrail)
            {
                float angle = (float)(rand.NextDouble() * Math.PI * 2);
                float speed = 30f + (float)rand.NextDouble() * 40f;
                particle.Velocity = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                );
            }
        }

        private void UpdateDashTrail(float dt)
        {
            for (int i = _dashTrail.Count - 1; i >= 0; i--)
            {
                var particle = _dashTrail[i];
                particle.Life -= dt;
                particle.Position += particle.Velocity * dt;
                particle.Velocity *= 0.92f;
                
                float t = particle.Life / particle.MaxLife;
                particle.Alpha = t * 0.7f;

                if (particle.Life <= 0f)
                {
                    _dashTrail.RemoveAt(i);
                }
            }
        }

        private void SpawnGhostTrail()
        {
            Texture2D frame = _graphics.GetTexture("player_walk_7");
            
            _ghostTrails.Add(new GhostTrail
            {
                Position = Position,
                Texture = frame,
                Alpha = 0.28f,
                Life = 0.32f,
                FlipX = _sprite.FlipX,
                Tint = _dashColor
            });
        }

        private void UpdateGhostTrails(float dt)
        {
            for (int i = _ghostTrails.Count - 1; i >= 0; i--)
            {
                _ghostTrails[i].Life -= dt;
                float t = _ghostTrails[i].Life / 0.32f;
                if (t < 0f) t = 0f;
                _ghostTrails[i].Alpha = t * 0.28f;
                if (_ghostTrails[i].Life <= 0f)
                    _ghostTrails.RemoveAt(i);
            }
        }

        private void SpawnJumpParticles()
        {
            if (_jumpParticles.Count == 0) return;

            _spriteParticles.Add(new SpriteParticle
            {
                Position = Position + new Vector2(8, 35),
                Frames = new List<Texture2D>(_jumpParticles),
                CurrentFrame = 0,
                FrameTimer = 0f,
                FrameTime = 0.05f,
                Active = true,
                Rotation = 0f,
                Origin = new Vector2(_jumpParticles[0].Width / 2, _jumpParticles[0].Height)
            });
        }

        private void SpawnLandParticles()
        {
            if (_landParticles.Count == 0) return;

            _spriteParticles.Add(new SpriteParticle
            {
                Position = Position + new Vector2(8, 35),
                Frames = new List<Texture2D>(_landParticles),
                CurrentFrame = 0,
                FrameTimer = 0f,
                FrameTime = 0.05f,
                Active = true,
                Rotation = 0f,
                Origin = new Vector2(_landParticles[0].Width / 2, _landParticles[0].Height)
            });
        }

        private void SpawnDashParticles()
        {
            if (_dashLineParticles.Count == 0) return;

            Vector2 dashDir = _physics.DashDirection;
            float rotation = (float)Math.Atan2(dashDir.Y, dashDir.X);

            _spriteParticles.Add(new SpriteParticle
            {
                Position = Position + new Vector2(8, 12),
                Frames = new List<Texture2D>(_dashLineParticles),
                CurrentFrame = 0,
                FrameTimer = 0f,
                FrameTime = 0.04f,
                Active = true,
                Rotation = rotation,
                Origin = new Vector2(_dashLineParticles[0].Width / 2, _dashLineParticles[0].Height / 2)
            });
        }

        private void UpdateSpriteParticles(float dt)
        {
            for (int i = _spriteParticles.Count - 1; i >= 0; i--)
            {
                var particle = _spriteParticles[i];
                particle.FrameTimer += dt;

                if (particle.FrameTimer >= particle.FrameTime)
                {
                    particle.FrameTimer = 0f;
                    particle.CurrentFrame++;

                    if (particle.CurrentFrame >= particle.Frames.Count)
                    {
                        particle.Active = false;
                        _spriteParticles.RemoveAt(i);
                    }
                }
            }
        }

        private void UpdateAnimations(float moveX)
        {
            if (_physics.IsDashing)
            {
                return;
            }
            else if (_isClimbing)
            {
                _sprite.Play("climb");
            }
            else if (_physics.IsWallSliding)
            {
                _sprite.Play("wallSlide");
                _sprite.FlipX = _wallDirection < 0;
            }
            else if (System.Math.Abs(moveX) > 0.1f && _physics.IsGrounded)
            {
                _sprite.Play("walk");
                _sprite.FlipX = moveX < 0;
            }
            else
            {
                _sprite.Play("idle");
            }

            if (System.Math.Abs(moveX) > 0.1f && !_physics.IsWallSliding)
                _sprite.FlipX = moveX < 0;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;

            foreach (var trail in _dashTrail)
            {
                byte alpha = (byte)(trail.Alpha * 255);
                Color trailColor = new Color(_dashTrailColor.R, _dashTrailColor.G, _dashTrailColor.B, alpha);
                spriteBatch.Draw(_dashPixel, trail.Position, null, trailColor, 0f, new Vector2(0.5f), 1.8f, SpriteEffects.None, 0f);
            }

            for (int i = 0; i < _ghostTrails.Count; i++)
            {
                var g = _ghostTrails[i];
                float a = g.Alpha;
                byte A = (byte)(a * 255f);
                byte R = (byte)(g.Tint.R * a);
                byte G = (byte)(g.Tint.G * a);
                byte B = (byte)(g.Tint.B * a);
                var ghostColor = new Color(R, G, B, A);
                var fx = g.FlipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                spriteBatch.Draw(g.Texture, g.Position, null, ghostColor, 0f, Vector2.Zero, 1f, fx, 0f);
            }

            foreach (var particle in _spriteParticles)
            {
                if (particle.Active && particle.CurrentFrame < particle.Frames.Count)
                {
                    spriteBatch.Draw(
                        particle.Frames[particle.CurrentFrame],
                        particle.Position,
                        null,
                        Color.White,
                        particle.Rotation,
                        particle.Origin,
                        1f,
                        SpriteEffects.None,
                        0f
                    );
                }
            }

            Color drawColor = _physics.IsDashing ? _dashColor : _normalColor;
            
            if (_stamina < 20f && (_isClimbing || _physics.IsWallSliding))
            {
                float pulse = (float)System.Math.Sin(gameTime.TotalGameTime.TotalSeconds * 8) * 0.3f + 0.7f;
                drawColor = Color.Lerp(drawColor, Color.Red, 1f - pulse);
            }

            _sprite.Draw(spriteBatch, Position, drawColor, 1f);
        }

        public PhysicsComponent GetPhysics()
        {
            return _physics;
        }
    }
}
