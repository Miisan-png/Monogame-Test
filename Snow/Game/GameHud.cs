// Snow/Game/GameHUD.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Snow.Engine;
using System;

namespace Snow.Game
{
    public class GameHUD
    {
        private BitmapFont _font;
        private Texture2D _pixel;
        private int _shardCount;
        private int _totalShards;
        private float _shardCounterScale;
        private float _shardPulseTimer;
        
        public GameHUD(GraphicsDevice device)
        {
            _font = new BitmapFont(device, "assets/Font/default/default_font_data.txt");
            
            _pixel = new Texture2D(device, 1, 1);
            _pixel.SetData(new[] { Color.White });
            
            _shardCount = 0;
            _totalShards = 0;
            _shardCounterScale = 2.5f;
            _shardPulseTimer = 0f;
        }

        public void SetShardCount(int collected, int total)
        {
            if (collected > _shardCount)
            {
                _shardCounterScale = 3.5f;
                _shardPulseTimer = 0.3f;
            }
            _shardCount = collected;
            _totalShards = total;
        }

        public void Update(float deltaTime)
        {
            if (_shardPulseTimer > 0f)
            {
                _shardPulseTimer -= deltaTime;
                _shardCounterScale = MathHelper.Lerp(_shardCounterScale, 2.5f, deltaTime * 8f);
            }
            else
            {
                _shardCounterScale = 2.5f;
            }
        }

        public void DrawHealthBar(SpriteBatch spriteBatch, int health, int maxHealth)
        {
            int barX = 20;
            int barY = 20;
            int barWidth = 100;
            int barHeight = 12;
            
            spriteBatch.Draw(_pixel, new Rectangle(barX - 2, barY - 2, barWidth + 4, barHeight + 4), Color.Black * 0.5f);
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barWidth, barHeight), new Color(50, 50, 50));
            
            int healthWidth = (int)((float)health / maxHealth * barWidth);
            Color healthColor = health > maxHealth * 0.5f ? new Color(100, 255, 100) : 
                               health > maxHealth * 0.25f ? new Color(255, 200, 100) : 
                               new Color(255, 100, 100);
            
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, healthWidth, barHeight), healthColor);
            
            _font.DrawString(spriteBatch, "HP", new Vector2(barX, barY - 18), Color.White, 2f);
        }

        public void DrawStaminaBar(SpriteBatch spriteBatch, float stamina, float maxStamina)
        {
            int barX = 20;
            int barY = 45;
            int barWidth = 100;
            int barHeight = 8;
            
            spriteBatch.Draw(_pixel, new Rectangle(barX - 2, barY - 2, barWidth + 4, barHeight + 4), Color.Black * 0.5f);
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, barWidth, barHeight), new Color(40, 40, 60));
            
            int staminaWidth = (int)(stamina / maxStamina * barWidth);
            Color staminaColor = stamina > 20f ? new Color(100, 200, 255) : new Color(255, 100, 100);
            
            spriteBatch.Draw(_pixel, new Rectangle(barX, barY, staminaWidth, barHeight), staminaColor);
            
            _font.DrawString(spriteBatch, "STAMINA", new Vector2(barX, barY - 18), new Color(150, 150, 200), 1.8f);
        }

        public void DrawShardCounter(SpriteBatch spriteBatch)
        {
            Vector2 pos = new Vector2(280, 20);
            
            string text = $"{_shardCount}/{_totalShards}";
            
            Color counterColor = new Color(150, 220, 255);
            if (_shardPulseTimer > 0f)
            {
                float pulse = (float)Math.Sin(_shardPulseTimer * 20f) * 0.5f + 0.5f;
                counterColor = Color.Lerp(counterColor, new Color(255, 255, 150), pulse);
            }
            
            _font.DrawString(spriteBatch, text, pos, counterColor, _shardCounterScale, TextAlignment.Right);
            _font.DrawString(spriteBatch, "SHARDS", new Vector2(pos.X, pos.Y - 20), new Color(100, 180, 230), 2f, TextAlignment.Right);
        }

        public void Draw(SpriteBatch spriteBatch, Player player, int shardCount, int totalShards)
        {
            SetShardCount(shardCount, totalShards);
            
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            DrawStaminaBar(spriteBatch, player.Stamina, player.MaxStamina);
            DrawShardCounter(spriteBatch);
            
            spriteBatch.End();
        }
    }
}