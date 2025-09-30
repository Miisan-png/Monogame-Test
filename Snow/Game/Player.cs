using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;

namespace Snow.Game
{
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
            bool jumpPressed = _input.IsKeyPressed(Keys.C);
            bool jumpReleased = _input.IsKeyReleased(Keys.C);
            bool dashPressed = _input.IsKeyPressed(Keys.X);

            _physics.Update(deltaTime, moveInput, jumpPressed, jumpReleased, dashPressed);

            if (System.Math.Abs(moveInput) > 0.1f)
            {
                _sprite.Play("walk");
                _sprite.FlipX = moveInput < 0;
                
                if (_physics.IsGrounded)
                {
                    _dustTimer += deltaTime;
                    if (_dustTimer > 0.1f)
                    {
                        Vector2 dustPos = Position + new Vector2(8, 24);
                        Vector2 dustDir = new Vector2(-System.Math.Sign(moveInput), -0.5f);
                        _particles.EmitDust(dustPos, dustDir, 2, new Color(200, 200, 200));
                        _dustTimer = 0f;
                    }
                }
            }
            else
            {
                _sprite.Play("idle");
            }

            if (!_wasGrounded && _physics.IsGrounded)
            {
                Vector2 landPos = Position + new Vector2(8, 24);
                _particles.EmitBurst(landPos, 8, 20f, 60f, new Color(220, 220, 220), 0.4f, 1f);
            }

            _wasGrounded = _physics.IsGrounded;
            _sprite.Update(deltaTime);

            Velocity = _physics.Velocity;

            if (Position.Y > 150)
            {
                _physics.IsGrounded = true;
                Position = new Vector2(Position.X, 150);
                if (_physics.Velocity.Y > 0)
                {
                    Vector2 vel = _physics.Velocity;
                    vel.Y = 0;
                    _physics.Velocity = vel;
                }
            }
            else
            {
                _physics.IsGrounded = false;
            }

            base.Update(gameTime);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;

            _sprite.Draw(spriteBatch, Position, Color.White, 1f);
        }
    }
}








