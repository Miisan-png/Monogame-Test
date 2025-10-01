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
    }

    public class Player : Actor
    {
        private PhysicsComponent _physics;
        private InputManager _input;
        private AnimatedSprite _sprite;
        private GraphicsManager _graphics;
        private ParticleSystem _particles;
        
        public Vector2 Size { get; set; }
        private float _dustTimer;
        private bool _wasGrounded;
        private bool _wasDashing;
        private List<GhostTrail> _ghostTrails;
        private float _ghostSpawnTimer;

        public Player(Vector2 position, GraphicsDevice graphicsDevice, InputManager input, GraphicsManager graphics, ParticleSystem particles) : base(position)
        {
            Size = new Vector2(16, 24);
            _input = input;
            _graphics = graphics;
            _particles = particles;

            _physics = new PhysicsComponent();
            _physics.IsGrounded = true;

            _sprite = new AnimatedSprite();
            LoadAnimations();
            
            _dustTimer = 0f;
            _wasGrounded = false;
            _wasDashing = false;
            _ghostTrails = new List<GhostTrail>();
            _ghostSpawnTimer = 0f;
        }

        private void LoadAnimations()
        {
            Animation idle = new Animation("idle", 0.1f, true);
            for (int i = 1; i <= 6; i++)
            {
                idle.Frames.Add(_graphics.LoadTexture($"player_idle_{i}", $"assets/Main/Player/Player{i}.png"));
            }
            _sprite.AddAnimation(idle);

            Animation walk = new Animation("walk", 0.08f, true);
            for (int i = 7; i <= 10; i++)
            {
                walk.Frames.Add(_graphics.LoadTexture($"player_walk_{i}", $"assets/Main/Player/Player{i}.png"));
            }
            _sprite.AddAnimation(walk);

            _sprite.Play("idle");
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float moveInput = _input.GetAxisHorizontal();
            float verticalInput = _input.GetAxisVertical();
            bool jumpPressed = _input.IsKeyPressed(Keys.C);
            bool jumpReleased = _input.IsKeyReleased(Keys.C);
            bool dashPressed = _input.IsKeyPressed(Keys.X);

            bool justDashed = dashPressed && _physics.CanDash;

            _physics.Update(deltaTime, moveInput, verticalInput, jumpPressed, jumpReleased, dashPressed);

            if (_physics.IsDashing)
            {
                _ghostSpawnTimer += deltaTime;
                if (_ghostSpawnTimer > 0.08f)
                {
                    SpawnGhostTrail();
                    _ghostSpawnTimer = 0f;
                }
            }

            UpdateGhostTrails(deltaTime);

            if (System.Math.Abs(moveInput) > 0.1f && _physics.IsGrounded)
            {
                _sprite.Play("walk");
                _sprite.FlipX = moveInput < 0;
                
                _dustTimer += deltaTime;
                if (_dustTimer > 0.12f)
                {
                    Vector2 dustPos = Position + new Vector2(8, 24);
                    Vector2 dustDir = new Vector2(-System.Math.Sign(moveInput), -0.5f);
                    _particles.EmitDust(dustPos, dustDir, 2, new Color(200, 200, 200));
                    _dustTimer = 0f;
                }
            }
            else
            {
                _sprite.Play("idle");
            }

            if (System.Math.Abs(moveInput) > 0.1f)
            {
                _sprite.FlipX = moveInput < 0;
            }

            if (!_wasGrounded && _physics.IsGrounded)
            {
                Vector2 landPos = Position + new Vector2(8, 24);
                _particles.EmitBurst(landPos, 10, 30f, 70f, new Color(220, 220, 220), 0.4f, 1.2f);
            }

            _wasGrounded = _physics.IsGrounded;
            _wasDashing = _physics.IsDashing;
            _sprite.Update(deltaTime);

            Velocity = _physics.Velocity;
        }

        private void SpawnGhostTrail()
        {
            Texture2D currentFrame = _graphics.GetTexture("player_walk_7");
            
            _ghostTrails.Add(new GhostTrail
            {
                Position = Position,
                Texture = currentFrame,
                Alpha = 0.25f,
                Life = 0.4f,
                FlipX = _sprite.FlipX
            });
        }

        private void UpdateGhostTrails(float deltaTime)
        {
            for (int i = _ghostTrails.Count - 1; i >= 0; i--)
            {
                _ghostTrails[i].Life -= deltaTime;
                _ghostTrails[i].Alpha = _ghostTrails[i].Life / 0.4f * 0.25f;
                
                if (_ghostTrails[i].Life <= 0)
                {
                    _ghostTrails.RemoveAt(i);
                }
            }
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;

            foreach (var ghost in _ghostTrails)
            {
                Color ghostColor = new Color(255, 255, 255, (int)(ghost.Alpha * 255));
                SpriteEffects effects = ghost.FlipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;
                
                spriteBatch.Draw(
                    ghost.Texture,
                    ghost.Position,
                    null,
                    ghostColor,
                    0f,
                    Vector2.Zero,
                    1f,
                    effects,
                    0f
                );
            }

            _sprite.Draw(spriteBatch, Position, Color.White, 1f);
        }

        public PhysicsComponent GetPhysics()
        {
            return _physics;
        }
    }
}
