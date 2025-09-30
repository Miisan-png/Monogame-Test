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
        
        public Vector2 Size { get; set; }

        public Player(Vector2 position, GraphicsDevice graphicsDevice, InputManager input, GraphicsManager graphics) : base(position)
        {
            Size = new Vector2(16, 24);
            _input = input;
            _graphics = graphics;

            _physics = new PhysicsComponent();
            _physics.IsGrounded = true;

            _sprite = new AnimatedSprite();
            LoadAnimations();
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
            }
            else
            {
                _sprite.Play("idle");
            }

            _sprite.Update(deltaTime);

            Velocity = _physics.Velocity;

            if (Position.Y > 500)
            {
                _physics.IsGrounded = true;
                Position = new Vector2(Position.X, 500);
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

