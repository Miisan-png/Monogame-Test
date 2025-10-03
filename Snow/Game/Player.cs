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
        private List<GhostTrail> _ghostTrails;
        private float _ghostSpawnTimer;
        private Color _dashColor = new Color(100, 200, 255, 255);
        private Color _normalColor = Color.White;
        private float _coyoteTimer;
        private float _jumpBufferTimer;

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
            _ghostTrails = new List<GhostTrail>();
            _ghostSpawnTimer = 0f;
            _coyoteTimer = 0f;
            _jumpBufferTimer = 0f;
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

            _sprite.Play("idle");
        }

        public override void Update(GameTime gameTime)
        {
            float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            float moveX = _input.GetAxisHorizontal();
            float moveY = _input.GetAxisVertical();
            bool jumpPressed = _input.IsKeyPressed(Keys.C);
            bool jumpReleased = _input.IsKeyReleased(Keys.C);
            bool dashPressed = _input.IsKeyPressed(Keys.X);

            if (_physics.IsGrounded) _coyoteTimer = 0.12f; else _coyoteTimer -= dt;
            if (jumpPressed) _jumpBufferTimer = 0.12f; else _jumpBufferTimer -= dt;

            bool canJump = (_coyoteTimer > 0f && jumpPressed) || (_physics.IsGrounded && _jumpBufferTimer > 0f);
            bool dashInput = dashPressed && _physics.CanDash;

            if (!_wasDashing && dashInput) _camera.Shake(4f, 0.2f);

            _physics.Update(dt, moveX, moveY, canJump, jumpReleased, dashInput);

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
                _sprite.Play("walk");
                _sprite.FlipX = moveX < 0;
                _dustTimer += dt;
                if (_dustTimer > 0.12f)
                {
                    Vector2 dustPos = Position + new Vector2(8, 24);
                    Vector2 dustDir = new Vector2(-System.Math.Sign(moveX), -0.5f);
                    _particles.EmitDust(dustPos, dustDir, 2, new Color(200, 200, 200));
                    _dustTimer = 0f;
                }
            }
            else
            {
                _sprite.Play("idle");
            }

            if (System.Math.Abs(moveX) > 0.1f)
                _sprite.FlipX = moveX < 0;

            if (!_wasGrounded && _physics.IsGrounded)
            {
                Vector2 landPos = Position + new Vector2(8, 24);
                _particles.EmitBurst(landPos, 10, 30f, 70f, new Color(220, 220, 220), 0.4f, 1.2f);
                _camera.Shake(2f, 0.15f);
            }

            _wasGrounded = _physics.IsGrounded;
            _wasDashing = _physics.IsDashing;
            _sprite.Update(dt);
            Velocity = _physics.Velocity;
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
                Tint = new Color(100, 200, 255, 255)
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

            Color drawColor = _physics.IsDashing ? _dashColor : _normalColor;
            _sprite.Draw(spriteBatch, Position, drawColor, 1f);
        }

        public PhysicsComponent GetPhysics()
        {
            return _physics;
        }
    }
}
