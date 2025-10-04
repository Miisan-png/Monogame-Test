using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using System.Collections.Generic;

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

    public class Player : Actor
    {
        private PhysicsComponent _physics;
        private InputManager _input;
        private AnimatedSprite _sprite;
        private GraphicsManager _graphics;
        private ParticleSystem _particles;
        private Camera _camera;
        public Vector2 Size { get; set; }
        private float _dustTimer;
        private bool _wasGrounded;
        private bool _wasDashing;
        private bool _wasWallSliding;
        private List<GhostTrail> _ghostTrails;
        private float _ghostSpawnTimer;
        private Color _dashColor = new Color(100, 200, 255, 255);
        private Color _wallSlideColor = new Color(255, 150, 100, 255);
        private Color _normalColor = Color.White;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private CollisionShape _collisionShape;
        private float _wallSlideParticleTimer;
        private int _wallDirection;
        private bool _canWallJump;
        private float _wallJumpCooldown;
        private float _stamina;
        private float _maxStamina = 100f;
        private float _staminaDrainRate = 30f;
        private float _staminaRegenRate = 60f;
        private bool _isClimbing;
        private float _climbSpeed = 80f;

        public float Stamina => _stamina;
        public float MaxStamina => _maxStamina;
        public bool IsWallSliding => _physics.IsWallSliding;
        public bool IsClimbing => _isClimbing;

        public Player(Vector2 position, GraphicsDevice graphicsDevice, InputManager input, GraphicsManager graphics, ParticleSystem particles, Camera camera) : base(position)
        {
            Size = new Vector2(16, 24);
            _input = input;
            _graphics = graphics;
            _particles = particles;
            _camera = camera;
            _physics = new PhysicsComponent();
            _physics.IsGrounded = true;
            _sprite = new AnimatedSprite();
            LoadAnimations();
            _dustTimer = 0f;
            _wasGrounded = false;
            _wasDashing = false;
            _wasWallSliding = false;
            _ghostTrails = new List<GhostTrail>();
            _ghostSpawnTimer = 0f;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
            _wallSlideParticleTimer = 0f;
            _wallDirection = 0;
            _canWallJump = false;
            _wallJumpCooldown = 0f;
            _stamina = _maxStamina;
            _isClimbing = false;
            
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
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveX = _input.GetAxisHorizontal();
            float moveY = _input.GetAxisVertical();
            bool jumpPressed = _input.IsKeyPressed(Keys.C);
            bool jumpReleased = _input.IsKeyReleased(Keys.C);
            bool jumpHeld = _input.IsKeyDown(Keys.C);
            bool dashPressed = _input.IsKeyPressed(Keys.X);

            UpdateTimers(dt);
            CheckWallContact();
            HandleStamina(dt, moveY, jumpHeld);
            HandleWallSliding(dt, moveX, moveY, jumpHeld);
            HandleClimbing(dt, moveX, moveY, jumpHeld);

            bool canJump = (_coyoteTimer > 0f && jumpPressed) || 
                          (_physics.IsGrounded && _jumpBufferTimer > 0f) ||
                          (_canWallJump && jumpPressed && _wallJumpCooldown <= 0f);
            
            bool dashInput = dashPressed && _physics.CanDash;

            if (!_wasDashing && dashInput) _camera.Shake(4f, 0.2f);

            Vector2 wallJumpDirection = Vector2.Zero;
            if (_canWallJump && jumpPressed && _wallJumpCooldown <= 0f)
            {
                wallJumpDirection = new Vector2(-_wallDirection * 1.2f, -1.5f);
                _wallJumpCooldown = 0.3f;
                _physics.ResetCoyoteTime();
                
                Vector2 particlePos = Position + new Vector2(_wallDirection > 0 ? 16 : 0, 12);
                _particles.EmitBurst(particlePos, 8, 40f, 80f, new Color(255, 200, 100), 0.5f, 1.5f);
                _camera.Shake(3f, 0.15f);
            }

            _physics.Update(dt, moveX, moveY, canJump, jumpReleased, dashInput, wallJumpDirection);

            HandleEffects(dt, moveX);
            UpdateAnimations(moveX);

            if (!_wasGrounded && _physics.IsGrounded)
            {
                Vector2 landPos = Position + new Vector2(8, 24);
                _particles.EmitBurst(landPos, 10, 30f, 70f, new Color(220, 220, 220), 0.4f, 1.2f);
                _camera.Shake(2f, 0.15f);
                _stamina = _maxStamina;
            }

            if (!_wasWallSliding && _physics.IsWallSliding)
            {
                Vector2 wallPos = Position + new Vector2(_wallDirection > 0 ? 16 : 0, 12);
                _particles.EmitBurst(wallPos, 5, 20f, 40f, new Color(200, 150, 100), 0.3f, 1.0f);
            }

            _wasGrounded = _physics.IsGrounded;
            _wasDashing = _physics.IsDashing;
            _wasWallSliding = _physics.IsWallSliding;
            _sprite.Update(dt);
            Velocity = _physics.Velocity;
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
            if (_isClimbing || (_physics.IsWallSliding && jumpHeld))
            {
                _stamina -= _staminaDrainRate * dt;
                if (_stamina < 0f) _stamina = 0f;
            }
            else if (_physics.IsGrounded || !_physics.IsWallSliding)
            {
                _stamina += _staminaRegenRate * dt;
                if (_stamina > _maxStamina) _stamina = _maxStamina;
            }
        }

        private void HandleWallSliding(float dt, float moveX, float moveY, bool jumpHeld)
        {
            if (_physics.IsWallSliding && !_physics.IsGrounded && !_physics.IsDashing)
            {
                _wallSlideParticleTimer += dt;
                if (_wallSlideParticleTimer > 0.1f)
                {
                    Vector2 wallPos = Position + new Vector2(_wallDirection > 0 ? 16 : -2, 8 + (float)(new System.Random().NextDouble() * 8));
                    Vector2 particleVel = new Vector2(-_wallDirection * 20f, (float)(new System.Random().NextDouble() * 10 + 5));
                    _particles.Emit(wallPos, particleVel, new Color(180, 120, 80), 0.4f, 0.8f);
                    _wallSlideParticleTimer = 0f;
                }
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

        private void HandleEffects(float dt, float moveX)
        {
            if (_physics.IsDashing)
            {
                _ghostSpawnTimer += dt;
                if (_ghostSpawnTimer > 0.06f)
                {
                    SpawnGhostTrail();
                    _ghostSpawnTimer = 0f;
                }
            }

            UpdateGhostTrails(dt);

            if (System.Math.Abs(moveX) > 0.1f && _physics.IsGrounded)
            {
                _dustTimer += dt;
                if (_dustTimer > 0.12f)
                {
                    Vector2 dustPos = Position + new Vector2(8, 24);
                    Vector2 dustDir = new Vector2(-System.Math.Sign(moveX), -0.5f);
                    _particles.EmitDust(dustPos, dustDir, 2, new Color(200, 200, 200));
                    _dustTimer = 0f;
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

        private void SpawnGhostTrail()
        {
            Texture2D frame = _graphics.GetTexture("player_walk_7");
            Color trailColor = _physics.IsWallSliding ? _wallSlideColor : _dashColor;
            
            _ghostTrails.Add(new GhostTrail
            {
                Position = Position,
                Texture = frame,
                Alpha = 0.28f,
                Life = 0.32f,
                FlipX = _sprite.FlipX,
                Tint = trailColor
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

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;

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

            Color drawColor = _physics.IsDashing ? _dashColor : 
                             _physics.IsWallSliding ? _wallSlideColor : 
                             _normalColor;
            
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