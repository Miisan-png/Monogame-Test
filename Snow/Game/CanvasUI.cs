using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Snow.Engine;

namespace Snow.Game
{
    public class CanvasUI
    {
        private BitmapFont _font;

        public CanvasUI(GraphicsDevice device)
        {
            _font = new BitmapFont(device, "assets/Font/default/default_font_data.txt");
        }

        public void Update(GameTime gameTime)
        {
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            spriteBatch.End();
        }
    }
}
