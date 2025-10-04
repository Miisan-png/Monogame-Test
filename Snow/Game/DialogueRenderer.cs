using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Snow.Engine;
using System;

namespace Snow.Game
{
    public class DialogueRenderer
    {
        private BitmapFont _font;
        private string _fullText;
        private string _displayedText;
        private float _typewriterTimer;
        private float _typewriterSpeed;
        private int _currentCharIndex;
        private bool _isComplete;
        private Tween _alphaTween;
        private float _alpha;

        public bool IsComplete => _isComplete;
        public bool IsTyping => _currentCharIndex < _fullText.Length;

        public DialogueRenderer(GraphicsDevice device, float typewriterSpeed = 0.05f)
        {
            _font = new BitmapFont(device, "assets/Font/default/default_font_data.txt");
            _typewriterSpeed = typewriterSpeed;
            _fullText = "";
            _displayedText = "";
            _currentCharIndex = 0;
            _typewriterTimer = 0f;
            _isComplete = false;
            _alpha = 1f;
        }

        public void SetText(string text)
        {
            _fullText = text;
            _displayedText = "";
            _currentCharIndex = 0;
            _typewriterTimer = 0f;
            _isComplete = false;
            _alpha = 1f;
            _alphaTween = null;
        }

        public void SkipTypewriter()
        {
            _displayedText = _fullText;
            _currentCharIndex = _fullText.Length;
        }

        public void FadeOut(float duration)
        {
            _alphaTween = new Tween(_alpha, 0f, duration, EaseType.Linear);
        }

        public void Update(float deltaTime)
        {
            if (_alphaTween != null)
            {
                _alphaTween.Update(deltaTime);
                _alpha = _alphaTween.GetValue();
                if (_alphaTween.IsComplete)
                {
                    _isComplete = true;
                }
                return;
            }

            if (_currentCharIndex < _fullText.Length)
            {
                _typewriterTimer += deltaTime;
                if (_typewriterTimer >= _typewriterSpeed)
                {
                    _typewriterTimer = 0f;
                    _displayedText += _fullText[_currentCharIndex];
                    _currentCharIndex++;
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, float scale = 2.5f)
        {
            if (string.IsNullOrEmpty(_displayedText)) return;

            Color color = Color.White * _alpha;
            _font.DrawString(spriteBatch, _displayedText, position, color, scale, TextAlignment.Center);
        }
    }
}
