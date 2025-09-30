using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Snow.Engine
{
    public class PostProcessing
    {
        private GraphicsDevice _graphicsDevice;
        private RenderTarget2D _gameRenderTarget;
        private RenderTarget2D _bloomRenderTarget;
        private SpriteBatch _spriteBatch;

        public Color CanvasModulate { get; set; } = Color.White;
        public float BloomIntensity { get; set; } = 0.3f;
        public int GameWidth { get; private set; }
        public int GameHeight { get; private set; }

        public PostProcessing(GraphicsDevice graphicsDevice, int gameWidth, int gameHeight)
        {
            _graphicsDevice = graphicsDevice;
            GameWidth = gameWidth;
            GameHeight = gameHeight;

            _gameRenderTarget = new RenderTarget2D(graphicsDevice, gameWidth, gameHeight);
            _bloomRenderTarget = new RenderTarget2D(graphicsDevice, gameWidth, gameHeight);
            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void BeginGameRender()
        {
            _graphicsDevice.SetRenderTarget(_gameRenderTarget);
            _graphicsDevice.Clear(Color.Black);
        }

        public void EndGameRender()
        {
            _graphicsDevice.SetRenderTarget(null);
        }

        public void ApplyPostProcessing()
        {
            _graphicsDevice.SetRenderTarget(_bloomRenderTarget);
            _graphicsDevice.Clear(Color.Transparent);

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
            _spriteBatch.Draw(_gameRenderTarget, Vector2.Zero, Color.White * BloomIntensity);
            _spriteBatch.End();

            _graphicsDevice.SetRenderTarget(null);
        }

        public void DrawFinal()
        {
            _graphicsDevice.Clear(Color.Black);

            Rectangle destRect = GetScaledDestinationRectangle();

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
            _spriteBatch.Draw(_gameRenderTarget, destRect, CanvasModulate);
            _spriteBatch.End();

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp);
            _spriteBatch.Draw(_bloomRenderTarget, destRect, Color.White);
            _spriteBatch.End();
        }

        private Rectangle GetScaledDestinationRectangle()
        {
            int screenWidth = _graphicsDevice.PresentationParameters.BackBufferWidth;
            int screenHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;

            float scaleX = (float)screenWidth / GameWidth;
            float scaleY = (float)screenHeight / GameHeight;
            float scale = System.Math.Min(scaleX, scaleY);

            int scaledWidth = (int)(GameWidth * scale);
            int scaledHeight = (int)(GameHeight * scale);

            int offsetX = (screenWidth - scaledWidth) / 2;
            int offsetY = (screenHeight - scaledHeight) / 2;

            return new Rectangle(offsetX, offsetY, scaledWidth, scaledHeight);
        }

        public void Dispose()
        {
            _gameRenderTarget?.Dispose();
            _bloomRenderTarget?.Dispose();
            _spriteBatch?.Dispose();
        }
    }
}






