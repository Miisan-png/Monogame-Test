using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using Snow.Game;

namespace Snow
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphics;
        private GraphicsManager _graphicsManager;
        private Player _player;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _graphicsManager = new GraphicsManager(GraphicsDevice);
            _player = new Player(new Vector2(400, 300), GraphicsDevice);
        }

        protected override void Update(GameTime gameTime)
        {
            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _player.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.CornflowerBlue);

            _graphicsManager.SpriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _player.Draw(_graphicsManager.SpriteBatch, gameTime);
            _graphicsManager.SpriteBatch.End();

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _graphicsManager?.Dispose();
            base.UnloadContent();
        }
    }
}
