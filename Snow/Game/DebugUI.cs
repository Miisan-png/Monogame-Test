using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using System;
using System.Collections.Generic;
using System.Text;

namespace Snow.Game
{
    public class DebugUI
    {
        private BitmapFont _font;
        private bool _enabled;
        private int _frameCount;
        private float _elapsedTime;
        private int _fps;
        private Queue<float> _frameTimes;
        private float _minFrameTime;
        private float _maxFrameTime;
        private float _avgFrameTime;
        
        private bool _showExtendedInfo;
        private KeyboardState _previousKeyboard;

        private StringBuilder _stringBuilder;

        public bool Enabled
        {
            get => _enabled;
            set => _enabled = value;
        }

        public DebugUI(GraphicsDevice device)
        {
            _font = new BitmapFont(device, "assets/Font/default/default_font_data.txt");
            _enabled = true;
            _frameTimes = new Queue<float>();
            _showExtendedInfo = false;
            _stringBuilder = new StringBuilder(512);
        }

        public void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.F3) && !_previousKeyboard.IsKeyDown(Keys.F3))
            {
                _enabled = !_enabled;
            }

            if (keyboard.IsKeyDown(Keys.F4) && !_previousKeyboard.IsKeyDown(Keys.F4))
            {
                _showExtendedInfo = !_showExtendedInfo;
            }

            _previousKeyboard = keyboard;

            if (!_enabled) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            _frameTimes.Enqueue(deltaTime);
            if (_frameTimes.Count > 60)
                _frameTimes.Dequeue();

            _elapsedTime += deltaTime;
            _frameCount++;

            if (_elapsedTime >= 0.5f)
            {
                _fps = (int)(_frameCount / _elapsedTime);
                
                _minFrameTime = float.MaxValue;
                _maxFrameTime = float.MinValue;
                _avgFrameTime = 0f;
                
                foreach (float ft in _frameTimes)
                {
                    _minFrameTime = Math.Min(_minFrameTime, ft);
                    _maxFrameTime = Math.Max(_maxFrameTime, ft);
                    _avgFrameTime += ft;
                }
                
                if (_frameTimes.Count > 0)
                    _avgFrameTime /= _frameTimes.Count;

                _frameCount = 0;
                _elapsedTime = 0;
            }
        }

        public void Draw(SpriteBatch spriteBatch, Player player, Camera camera, int entityCount = 0)
        {
            if (!_enabled) return;

            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            _stringBuilder.Clear();
            
            Color fpsColor = _fps >= 60 ? new Color(100, 255, 100) : 
                            _fps >= 30 ? new Color(255, 255, 100) : 
                            new Color(255, 100, 100);
            
            _stringBuilder.Append("FPS: ");
            _stringBuilder.Append(_fps);
            _font.DrawString(spriteBatch, _stringBuilder.ToString(), new Vector2(10, 10), fpsColor, 2f);
            
            _stringBuilder.Clear();
            _stringBuilder.Append("Position: (");
            _stringBuilder.Append((int)player.Position.X);
            _stringBuilder.Append(", ");
            _stringBuilder.Append((int)player.Position.Y);
            _stringBuilder.Append(")");
            _font.DrawString(spriteBatch, _stringBuilder.ToString(), new Vector2(10, 30), Color.White, 2f);
            
            _stringBuilder.Clear();
            _stringBuilder.Append("Velocity: (");
            _stringBuilder.Append(player.Velocity.X.ToString("F1"));
            _stringBuilder.Append(", ");
            _stringBuilder.Append(player.Velocity.Y.ToString("F1"));
            _stringBuilder.Append(")");
            _font.DrawString(spriteBatch, _stringBuilder.ToString(), new Vector2(10, 50), Color.Yellow, 2f);

            var physics = player.GetPhysics();
            if (physics != null)
            {
                Color groundColor = physics.IsGrounded ? new Color(100, 255, 100) : new Color(255, 100, 100);
                _stringBuilder.Clear();
                _stringBuilder.Append("Grounded: ");
                _stringBuilder.Append(physics.IsGrounded ? "YES" : "NO");
                _font.DrawString(spriteBatch, _stringBuilder.ToString(), new Vector2(10, 70), groundColor, 2f);
                
                if (physics.IsDashing)
                {
                    _font.DrawString(spriteBatch, "DASHING", new Vector2(10, 90), new Color(255, 100, 255), 2f);
                }
            }

            if (_showExtendedInfo)
            {
                int yOffset = 110;
                
                _stringBuilder.Clear();
                _stringBuilder.Append("Camera: (");
                _stringBuilder.Append((int)camera.Position.X);
                _stringBuilder.Append(", ");
                _stringBuilder.Append((int)camera.Position.Y);
                _stringBuilder.Append(")");
                _font.DrawString(spriteBatch, _stringBuilder.ToString(), new Vector2(10, yOffset), Color.Cyan, 2f);
                yOffset += 20;

                _stringBuilder.Clear();
                _stringBuilder.Append("Entities: ");
                _stringBuilder.Append(entityCount);
                _font.DrawString(spriteBatch, _stringBuilder.ToString(), new Vector2(10, yOffset), Color.White, 2f);
                yOffset += 20;

                _stringBuilder.Clear();
                _stringBuilder.Append("Frame Time: ");
                _stringBuilder.Append((_avgFrameTime * 1000f).ToString("F2"));
                _stringBuilder.Append(" ms");
                _font.DrawString(spriteBatch, _stringBuilder.ToString(), new Vector2(10, yOffset), Color.White, 2f);
                yOffset += 20;

                _stringBuilder.Clear();
                _stringBuilder.Append("Min/Max: ");
                _stringBuilder.Append((_minFrameTime * 1000f).ToString("F2"));
                _stringBuilder.Append(" / ");
                _stringBuilder.Append((_maxFrameTime * 1000f).ToString("F2"));
                _stringBuilder.Append(" ms");
                _font.DrawString(spriteBatch, _stringBuilder.ToString(), new Vector2(10, yOffset), Color.Gray, 2f);
                yOffset += 20;
            }

            _font.DrawString(spriteBatch, "F3: Toggle Debug | F4: Extended Info", 
                new Vector2(10, 720 - 30), new Color(150, 150, 150), 1.5f);

            spriteBatch.End();
        }

        public void DrawCollisionBoxes(SpriteBatch spriteBatch, Player player, Camera camera, Texture2D pixel)
        {
            if (!_enabled || !_showExtendedInfo) return;

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

            DrawBox(spriteBatch, pixel, playerBox, Color.Lime, 1);

            spriteBatch.End();
        }

        private void DrawBox(SpriteBatch spriteBatch, Texture2D pixel, Rectangle rect, Color color, int thickness = 1)
        {
            spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, rect.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Left, rect.Top, thickness, rect.Height), color);
            spriteBatch.Draw(pixel, new Rectangle(rect.Right - thickness, rect.Top, thickness, rect.Height), color);
        }
    }
}
