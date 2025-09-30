using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;

namespace Snow.Engine
{
    public class DebugOverlay
    {
        private SpriteFont _font;
        private Texture2D _pixel;
        private bool _enabled;
        private int _frameCount;
        private float _elapsedTime;
        private int _fps;
        private Queue<float> _frameTimes;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public DebugOverlay(GraphicsDevice graphicsDevice)
        {
            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });
            _enabled = false;
            _frameTimes = new Queue<float>();
        }

        public void SetFont(SpriteFont font)
        {
            _font = font;
        }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            _frameTimes.Enqueue(deltaTime);
            if (_frameTimes.Count > 60)
                _frameTimes.Dequeue();

            _elapsedTime += deltaTime;
            _frameCount++;

            if (_elapsedTime >= 0.5f)
            {
                _fps = (int)(_frameCount / _elapsedTime);
                _frameCount = 0;
                _elapsedTime = 0;
            }

            if (Keyboard.GetState().IsKeyDown(Keys.F3))
            {
                _enabled = !_enabled;
            }
        }

        public void DrawText(SpriteBatch spriteBatch, Vector2 position, string text, Color color)
        {
            if (_font == null) return;

            spriteBatch.DrawString(_font, text, position + new Vector2(1, 1), Color.Black);
            spriteBatch.DrawString(_font, text, position, color);
        }

        public void DrawBox(SpriteBatch spriteBatch, Rectangle rect, Color color, int thickness = 1)
        {
            spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
        }

        public void Draw(SpriteBatch spriteBatch, Actor player, Camera camera)
        {
            if (!_enabled) return;

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            int y = 10;
            int lineHeight = 16;

            DrawText(spriteBatch, new Vector2(10, y), $"FPS: {_fps}", Color.Yellow);
            y += lineHeight;

            DrawText(spriteBatch, new Vector2(10, y), $"Pos: ({(int)player.Position.X}, {(int)player.Position.Y})", Color.White);
            y += lineHeight;

            DrawText(spriteBatch, new Vector2(10, y), $"Vel: ({(int)player.Velocity.X}, {(int)player.Velocity.Y})", Color.White);
            y += lineHeight;

            var physicsField = player.GetType().GetField("_physics", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (physicsField != null)
            {
                var physics = physicsField.GetValue(player) as PhysicsComponent;
                if (physics != null)
                {
                    DrawText(spriteBatch, new Vector2(10, y), $"Grounded: {physics.IsGrounded}", 
                        physics.IsGrounded ? Color.Green : Color.Red);
                    y += lineHeight;
                }
            }

            DrawText(spriteBatch, new Vector2(10, y), $"Cam: ({(int)camera.Position.X}, {(int)camera.Position.Y})", Color.Cyan);

            spriteBatch.End();
        }

        public void DrawCollisionBoxes(SpriteBatch spriteBatch, Actor player, Camera camera)
        {
            if (!_enabled) return;

            spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: camera.GetTransformMatrix()
            );

            Rectangle playerBox = new Rectangle(
                (int)player.Position.X,
                (int)player.Position.Y,
                16,
                24
            );

            DrawBox(spriteBatch, playerBox, Color.Lime, 1);

            spriteBatch.End();
        }

        public void Dispose()
        {
            _pixel?.Dispose();
        }
    }
}
