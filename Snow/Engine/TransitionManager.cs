using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.IO;

namespace Snow.Engine
{
    public enum TransitionType
    {
        CircleReveal,
        CircleClose,
        Fade,
        Wipe
    }

    public class Transition
    {
        public TransitionType Type { get; set; }
        public float Duration { get; set; }
        public float Progress { get; set; }
        public bool IsComplete { get; set; }
        public Vector2 FocalPoint { get; set; }
        public Action OnComplete { get; set; }
        public float Delay { get; set; }
        public float DelayTimer { get; set; }

        public Transition(TransitionType type, float duration, Vector2 focalPoint, float delay = 0f)
        {
            Type = type;
            Duration = duration;
            Progress = 0f;
            IsComplete = false;
            FocalPoint = focalPoint;
            Delay = delay;
            DelayTimer = 0f;
        }
    }

    public class TransitionManager
    {
        private GraphicsDevice _graphicsDevice;
        private Texture2D _pixel;
        private Transition _currentTransition;
        private SpriteBatch _spriteBatch;
        private Effect _maskEffect;

        public bool IsTransitioning => _currentTransition != null && !_currentTransition.IsComplete;

        public TransitionManager(GraphicsDevice graphicsDevice)
        {
            _graphicsDevice = graphicsDevice;
            _spriteBatch = new SpriteBatch(graphicsDevice);

            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            LoadShader();
        }

        private void LoadShader()
        {
            try
            {
                string shaderPath = "Snow/Shaders/CircleMask.fx";
                if (File.Exists(shaderPath))
                {
                    byte[] shaderBytes = File.ReadAllBytes(shaderPath);
                    _maskEffect = new Effect(_graphicsDevice, shaderBytes);
                    System.Console.WriteLine("Transition shader loaded successfully");
                }
                else
                {
                    System.Console.WriteLine($"Shader file not found: {shaderPath}");
                    _maskEffect = null;
                }
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Transition shader failed to load: {e.Message}");
                _maskEffect = null;
            }
        }

        public void StartTransition(TransitionType type, float duration, Vector2 focalPoint, float delay = 0f, Action onComplete = null)
        {
            System.Console.WriteLine($"Starting transition: {type}, Duration: {duration}, FocalPoint: {focalPoint}, Delay: {delay}");
            _currentTransition = new Transition(type, duration, focalPoint, delay)
            {
                OnComplete = onComplete
            };
        }

        public void Update(float deltaTime)
        {
            if (_currentTransition == null || _currentTransition.IsComplete)
                return;

            if (_currentTransition.DelayTimer < _currentTransition.Delay)
            {
                _currentTransition.DelayTimer += deltaTime;
                return;
            }

            _currentTransition.Progress += deltaTime / _currentTransition.Duration;

            if (_currentTransition.Progress >= 1f)
            {
                _currentTransition.Progress = 1f;
                _currentTransition.IsComplete = true;
                _currentTransition.OnComplete?.Invoke();
            }
        }

        public void Draw()
        {
            if (_currentTransition == null || _currentTransition.IsComplete)
                return;

            int screenWidth = _graphicsDevice.PresentationParameters.BackBufferWidth;
            int screenHeight = _graphicsDevice.PresentationParameters.BackBufferHeight;

            float scaleX = (float)screenWidth / 320f;
            float scaleY = (float)screenHeight / 180f;
            float scale = Math.Min(scaleX, scaleY);

            int scaledWidth = (int)(320 * scale);
            int scaledHeight = (int)(180 * scale);
            int offsetX = (screenWidth - scaledWidth) / 2;
            int offsetY = (screenHeight - scaledHeight) / 2;

            Vector2 screenFocalPoint = new Vector2(
                _currentTransition.FocalPoint.X * scale + offsetX,
                _currentTransition.FocalPoint.Y * scale + offsetY
            );

            if (_currentTransition.DelayTimer < _currentTransition.Delay)
            {
                float smallRadius = 30f * scale;
                
                if (_maskEffect != null)
                {
                    DrawCircleWithShader(screenWidth, screenHeight, screenFocalPoint, smallRadius);
                }
                else
                {
                    DrawFallbackCircle(screenWidth, screenHeight, screenFocalPoint, smallRadius);
                }
                return;
            }

            switch (_currentTransition.Type)
            {
                case TransitionType.CircleReveal:
                    DrawCircleReveal(screenWidth, screenHeight, screenFocalPoint, scale);
                    break;
                case TransitionType.CircleClose:
                    DrawCircleClose(screenWidth, screenHeight, screenFocalPoint, scale);
                    break;
                case TransitionType.Fade:
                    DrawFade(screenWidth, screenHeight);
                    break;
            }
        }

        private void DrawCircleWithShader(int screenWidth, int screenHeight, Vector2 center, float radius)
        {
            _maskEffect.Parameters["Center"]?.SetValue(center);
            _maskEffect.Parameters["Radius"]?.SetValue(radius);
            _maskEffect.Parameters["Resolution"]?.SetValue(new Vector2(screenWidth, screenHeight));

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _maskEffect);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            _spriteBatch.End();
        }

        private void DrawFallbackCircle(int screenWidth, int screenHeight, Vector2 center, float radius)
        {
            // Fallback: Draw a simple fade
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * 0.9f);
            _spriteBatch.End();
        }

        private void DrawCircleReveal(int screenWidth, int screenHeight, Vector2 screenFocalPoint, float scale)
        {
            float maxDist = (float)Math.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);
            float startRadius = 30f * scale;
            float currentRadius = MathHelper.Lerp(startRadius, maxDist, EaseOutCubic(_currentTransition.Progress));

            if (_maskEffect != null)
            {
                DrawCircleWithShader(screenWidth, screenHeight, screenFocalPoint, currentRadius);
            }
            else
            {
                // Fallback: simple fade in
                float alpha = 1f - _currentTransition.Progress;
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * alpha);
                _spriteBatch.End();
            }
        }

        private void DrawCircleClose(int screenWidth, int screenHeight, Vector2 screenFocalPoint, float scale)
        {
            float maxDist = (float)Math.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);
            float currentRadius = MathHelper.Lerp(maxDist, 0, EaseInCubic(_currentTransition.Progress));

            if (_maskEffect != null)
            {
                DrawCircleWithShader(screenWidth, screenHeight, screenFocalPoint, currentRadius);
            }
            else
            {
                // Fallback: simple fade out
                float alpha = _currentTransition.Progress;
                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.Black * alpha);
                _spriteBatch.End();
            }
        }

        private void DrawFade(int screenWidth, int screenHeight)
        {
            float alpha = 1f - _currentTransition.Progress;
            
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
            
            Rectangle fullScreen = new Rectangle(0, 0, screenWidth, screenHeight);
            _spriteBatch.Draw(_pixel, fullScreen, Color.Black * alpha);
            
            _spriteBatch.End();
        }

        private float EaseOutCubic(float t)
        {
            float t1 = t - 1f;
            return t1 * t1 * t1 + 1f;
        }

        private float EaseInCubic(float t)
        {
            return t * t * t;
        }

        public void Dispose()
        {
            _pixel?.Dispose();
            _maskEffect?.Dispose();
            _spriteBatch?.Dispose();
        }
    }
}