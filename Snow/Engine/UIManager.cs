using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using System;
using System.Collections.Generic;

namespace Snow.Game
{
    public class UIManager
    {
        private List<UIButton> _buttons;
        private int _selectedIndex;
        private BitmapFont _font;
        private KeyboardState _previousKeyboard;
        private Camera _camera;
        private ParticleSystem _particles;
        private float _particleTimer;

        public UIManager(GraphicsDevice device, Camera camera, ParticleSystem particles)
        {
            _buttons = new List<UIButton>();
            _selectedIndex = 0;
            _font = new BitmapFont(device, "assets/Font/default/default_font_data.txt");
            _camera = camera;
            _particles = particles;
            _particleTimer = 0f;
        }

        public void AddButton(string text, Vector2 position, Action onClick)
        {
            _buttons.Add(new UIButton(text, position, _font, onClick));
            if (_buttons.Count == 1)
            {
                _buttons[0].IsFocused = true;
            }
        }

        public void ClearButtons()
        {
            _buttons.Clear();
            _selectedIndex = 0;
        }

        public void Update(float deltaTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.Down) && !_previousKeyboard.IsKeyDown(Keys.Down))
            {
                _buttons[_selectedIndex].IsFocused = false;
                _selectedIndex = (_selectedIndex + 1) % _buttons.Count;
                _buttons[_selectedIndex].IsFocused = true;
                _camera.Shake(2f, 0.1f);
                SpawnParticles(_buttons[_selectedIndex].Position);
            }

            if (keyboard.IsKeyDown(Keys.Up) && !_previousKeyboard.IsKeyDown(Keys.Up))
            {
                _buttons[_selectedIndex].IsFocused = false;
                _selectedIndex = (_selectedIndex - 1 + _buttons.Count) % _buttons.Count;
                _buttons[_selectedIndex].IsFocused = true;
                _camera.Shake(2f, 0.1f);
                SpawnParticles(_buttons[_selectedIndex].Position);
            }

            if ((keyboard.IsKeyDown(Keys.Enter) || keyboard.IsKeyDown(Keys.Space)) && 
                (!_previousKeyboard.IsKeyDown(Keys.Enter) && !_previousKeyboard.IsKeyDown(Keys.Space)))
            {
                _buttons[_selectedIndex].OnClick?.Invoke();
                _camera.Shake(5f, 0.2f);
                SpawnClickParticles(_buttons[_selectedIndex].Position);
            }

            _particleTimer += deltaTime;
            if (_particleTimer > 0.08f && _buttons.Count > 0)
            {
                Vector2 pos = _buttons[_selectedIndex].Position;
                pos.X += (float)(new Random().NextDouble() - 0.5) * 60f;
                pos.Y += (float)(new Random().NextDouble() - 0.5) * 20f;
                
                _particles.Emit(
                    pos,
                    new Vector2((float)(new Random().NextDouble() - 0.5) * 20f, (float)(new Random().NextDouble() - 0.5) * 20f),
                    new Color(0, 255, 255, 150),
                    0.5f,
                    1.2f
                );
                _particleTimer = 0f;
            }

            foreach (var button in _buttons)
            {
                button.Update(deltaTime);
            }

            _previousKeyboard = keyboard;
        }

        private void SpawnParticles(Vector2 position)
        {
            for (int i = 0; i < 8; i++)
            {
                float angle = (float)(new Random().NextDouble() * Math.PI * 2);
                float speed = (float)(new Random().NextDouble() * 40 + 20);
                Vector2 velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                
                _particles.Emit(
                    position,
                    velocity,
                    new Color(0, 255, 255),
                    0.6f,
                    1.5f
                );
            }
        }

        private void SpawnClickParticles(Vector2 position)
        {
            for (int i = 0; i < 20; i++)
            {
                float angle = (float)(new Random().NextDouble() * Math.PI * 2);
                float speed = (float)(new Random().NextDouble() * 80 + 40);
                Vector2 velocity = new Vector2((float)Math.Cos(angle) * speed, (float)Math.Sin(angle) * speed);
                
                _particles.Emit(
                    position,
                    velocity,
                    new Color(255, 255, 100),
                    0.8f,
                    2.0f
                );
            }
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            
            foreach (var button in _buttons)
            {
                button.Draw(spriteBatch);
            }
            
            spriteBatch.End();
        }
    }
}
