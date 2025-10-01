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

        public void Draw(SpriteBatch sb, Player player)
        {
            sb.Begin(samplerState: SamplerState.PointClamp);
            _font.DrawString(sb, $"Player Pos: {player.Position.X:0}, {player.Position.Y:0}", new Vector2(20, 20), Color.White, 3f);
            _font.DrawString(sb, $"Velocity: {player.Velocity.X:0.0}, {player.Velocity.Y:0.0}", new Vector2(20, 50), Color.Yellow, 3f);
            _font.DrawString(sb, "Snow Engine Font Rendering", new Vector2(20, 90), Color.Blue, 6f);
            sb.End();
        }
    }
}
