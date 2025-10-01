using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Snow.Engine
{
    public class PostProcessing
    {
        private GraphicsDevice _graphicsDevice;
        private RenderTarget2D _gameRenderTarget;
        private RenderTarget2D _bloomExtractTarget;
        private RenderTarget2D _bloomBlurHTarget;
        private RenderTarget2D _bloomBlurVTarget;
        private SpriteBatch _spriteBatch;
        private Effect _bloomEffect;
        private Effect _modulateEffect;

        public Color CanvasModulate { get; set; } = new Color(0.6f, 0.4f, 0.9f, 1.0f);
        public float BloomThreshold { get; set; } = 0.5f;
        public float BloomIntensity { get; set; } = 2.0f;
        public int GameWidth { get; private set; }
        public int GameHeight { get; private set; }

        public PostProcessing(GraphicsDevice graphicsDevice, int gameWidth, int gameHeight)
        {
            _graphicsDevice = graphicsDevice;
            GameWidth = gameWidth;
            GameHeight = gameHeight;

            _gameRenderTarget = new RenderTarget2D(graphicsDevice, gameWidth, gameHeight);
            _bloomExtractTarget = new RenderTarget2D(graphicsDevice, gameWidth, gameHeight);
            _bloomBlurHTarget = new RenderTarget2D(graphicsDevice, gameWidth, gameHeight);
            _bloomBlurVTarget = new RenderTarget2D(graphicsDevice, gameWidth, gameHeight);
            _spriteBatch = new SpriteBatch(graphicsDevice);
        }

        public void LoadShaders(Effect bloomEffect, Effect modulateEffect)
        {
            _bloomEffect = bloomEffect;
            _modulateEffect = modulateEffect;
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
            if (_bloomEffect == null || _modulateEffect == null)
                return;

            _bloomEffect.Parameters["BloomThreshold"].SetValue(BloomThreshold);
            _bloomEffect.Parameters["BloomIntensity"].SetValue(BloomIntensity);
            _bloomEffect.Parameters["TextureSize"].SetValue(new Vector2(GameWidth, GameHeight));

            _graphicsDevice.SetRenderTarget(_bloomExtractTarget);
            _graphicsDevice.Clear(Color.Transparent);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _bloomEffect);
            _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["ExtractBright"];
            _bloomEffect.Parameters["ScreenTexture"].SetValue(_gameRenderTarget);
            _spriteBatch.Draw(_gameRenderTarget, Vector2.Zero, Color.White);
            _spriteBatch.End();

            _graphicsDevice.SetRenderTarget(_bloomBlurHTarget);
            _graphicsDevice.Clear(Color.Transparent);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _bloomEffect);
            _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["BlurHorizontal"];
            _bloomEffect.Parameters["ScreenTexture"].SetValue(_bloomExtractTarget);
            _spriteBatch.Draw(_bloomExtractTarget, Vector2.Zero, Color.White);
            _spriteBatch.End();

            _graphicsDevice.SetRenderTarget(_bloomBlurVTarget);
            _graphicsDevice.Clear(Color.Transparent);
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _bloomEffect);
            _bloomEffect.CurrentTechnique = _bloomEffect.Techniques["BlurVertical"];
            _bloomEffect.Parameters["ScreenTexture"].SetValue(_bloomBlurHTarget);
            _spriteBatch.Draw(_bloomBlurHTarget, Vector2.Zero, Color.White);
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

            if (_bloomBlurVTarget != null)
            {
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp);
                _spriteBatch.Draw(_bloomBlurVTarget, destRect, Color.White);
                _spriteBatch.End();
            }
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
            _bloomExtractTarget?.Dispose();
            _bloomBlurHTarget?.Dispose();
            _bloomBlurVTarget?.Dispose();
            _spriteBatch?.Dispose();
        }
    }
}
