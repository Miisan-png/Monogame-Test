using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Snow.Engine;
using System;

namespace Snow.Game
{
    public class UIButton
    {
        public string Text { get; set; }
        public Vector2 Position { get; set; }
        public bool IsFocused { get; set; }
        public Action OnClick { get; set; }
        
        private float _scale;
        private float _targetScale;
        private Color _color;
        private Color _targetColor;
        private BitmapFont _font;
        
        private Color _normalColor = Color.White;
        private Color _focusedColor = new Color(0, 255, 255);
        private float _normalScale = 2.5f;
        private float _focusedScale = 3.0f;

        public UIButton(string text, Vector2 position, BitmapFont font, Action onClick)
        {
            Text = text;
            Position = position;
            _font = font;
            OnClick = onClick;
            IsFocused = false;
            _scale = _normalScale;
            _targetScale = _normalScale;
            _color = _normalColor;
            _targetColor = _normalColor;
        }

        public void Update(float deltaTime)
        {
            _targetScale = IsFocused ? _focusedScale : _normalScale;
            _targetColor = IsFocused ? _focusedColor : _normalColor;
            
            _scale = MathHelper.Lerp(_scale, _targetScale, deltaTime * 12f);
            _color = Color.Lerp(_color, _targetColor, deltaTime * 12f);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            _font.DrawString(spriteBatch, Text, Position, _color, _scale, TextAlignment.Center);
        }

        public Rectangle GetBounds()
        {
            Vector2 size = _font.MeasureString(Text, _scale);
            return new Rectangle(
                (int)(Position.X - size.X / 2),
                (int)(Position.Y - size.Y / 2),
                (int)size.X,
                (int)size.Y
            );
        }
    }
}
