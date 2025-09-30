using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;

namespace Snow.Game
{
    public class Player : Actor
    {
        private Texture2D _texture;
        private Color _color;
        public Vector2 Size { get; set; }

        public Player(Vector2 position, GraphicsDevice graphicsDevice) : base(position)
        {
            Size = new Vector2(32, 64);
            _color = Color.Red;
            
            _texture = new Texture2D(graphicsDevice, 1, 1);
            _texture.SetData(new[] { Color.White });
        }

        public override void Update(GameTime gameTime)
        {
            HandleInput();
            base.Update(gameTime);
        }

        private void HandleInput()
        {
            KeyboardState keyboard = Keyboard.GetState();
            Vector2 input = Vector2.Zero;

            if (keyboard.IsKeyDown(Keys.A) || keyboard.IsKeyDown(Keys.Left))
                input.X -= 1;
            if (keyboard.IsKeyDown(Keys.D) || keyboard.IsKeyDown(Keys.Right))
                input.X += 1;

            Velocity = input * 200f;
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive) return;

            Rectangle destRect = new Rectangle(
                (int)Position.X,
                (int)Position.Y,
                (int)Size.X,
                (int)Size.Y
            );

            spriteBatch.Draw(_texture, destRect, _color);
        }
    }
}
