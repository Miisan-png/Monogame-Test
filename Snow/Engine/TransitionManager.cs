using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;

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
        private RenderTarget2D _maskTarget;
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

            string shaderCode = @"
                #if OPENGL
                    #define SV_POSITION POSITION
                    #define VS_SHADERMODEL vs_3_0
                    #define PS_SHADERMODEL ps_3_0
                #else
                    #define VS_SHADERMODEL vs_4_0
                    #define PS_SHADERMODEL ps_4_0
                #endif

                float2 Center;
                float Radius;
                float2 Resolution;

                sampler2D MaskSampler : register(s0);

                struct VertexShaderOutput
                {
                    float4 Position : SV_POSITION;
                    float4 Color : COLOR0;
                    float2 TextureCoordinates : TEXCOORD0;
                };

                float4 MainPS(VertexShaderOutput input) : COLOR
                {
                    float2 pixelPos = input.TextureCoordinates * Resolution;
                    float dist = distance(pixelPos, Center);
                    
                    if (dist > Radius)
                    {
                        return float4(0, 0, 0, 1);
                    }
                    else
                    {
                        return float4(0, 0, 0, 0);
                    }
                }

                technique BasicColorDrawing
                {
                    pass P0
                    {
                        PixelShader = compile PS_SHADERMODEL MainPS();
                    }
                };
            ";

            try
            {
                _maskEffect = new Effect(graphicsDevice, System.Text.Encoding.UTF8.GetBytes(shaderCode));
            }
            catch
            {
                _maskEffect = null;
            }
        }

        public void StartTransition(TransitionType type, float duration, Vector2 focalPoint, float delay = 0f, Action onComplete = null)
        {
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

            if (_currentTransition.DelayTimer < _currentTransition.Delay)
            {
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

                float smallRadius = 30f * scale;

                if (_maskEffect != null)
                {
                    _maskEffect.Parameters["Center"].SetValue(screenFocalPoint);
                    _maskEffect.Parameters["Radius"].SetValue(smallRadius);
                    _maskEffect.Parameters["Resolution"].SetValue(new Vector2(screenWidth, screenHeight));

                    _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _maskEffect);
                    _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
                    _spriteBatch.End();
                }
                else
                {
                    _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

                    for (int y = 0; y < screenHeight; y += 2)
                    {
                        for (int x = 0; x < screenWidth; x += 2)
                        {
                            Vector2 pixelPos = new Vector2(x, y);
                            float dist = Vector2.Distance(pixelPos, screenFocalPoint);
                            
                            if (dist > smallRadius)
                            {
                                _spriteBatch.Draw(_pixel, new Rectangle(x, y, 2, 2), Color.Black);
                            }
                        }
                    }

                    _spriteBatch.End();
                }
                return;
            }

            switch (_currentTransition.Type)
            {
                case TransitionType.CircleReveal:
                    DrawCircleReveal(screenWidth, screenHeight);
                    break;
                case TransitionType.CircleClose:
                    DrawCircleClose(screenWidth, screenHeight);
                    break;
                case TransitionType.Fade:
                    DrawFade(screenWidth, screenHeight);
                    break;
            }
        }

        private void DrawCircleReveal(int screenWidth, int screenHeight)
        {
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

            float maxDist = (float)Math.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);
            float startRadius = 30f * scale;
            float currentRadius = MathHelper.Lerp(startRadius, maxDist, EaseOutCubic(_currentTransition.Progress));

            if (_maskEffect != null)
            {
                _maskEffect.Parameters["Center"].SetValue(screenFocalPoint);
                _maskEffect.Parameters["Radius"].SetValue(currentRadius);
                _maskEffect.Parameters["Resolution"].SetValue(new Vector2(screenWidth, screenHeight));

                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _maskEffect);
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
                _spriteBatch.End();
            }
            else
            {
                DrawCircleRevealFallback(screenWidth, screenHeight, screenFocalPoint, currentRadius);
            }
        }

        private void DrawCircleRevealFallback(int screenWidth, int screenHeight, Vector2 focalPoint, float currentRadius)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

            for (int y = 0; y < screenHeight; y += 2)
            {
                for (int x = 0; x < screenWidth; x += 2)
                {
                    Vector2 pixelPos = new Vector2(x, y);
                    float dist = Vector2.Distance(pixelPos, focalPoint);
                    
                    if (dist > currentRadius)
                    {
                        _spriteBatch.Draw(_pixel, new Rectangle(x, y, 2, 2), Color.Black);
                    }
                }
            }

            _spriteBatch.End();
        }

        private void DrawCircleClose(int screenWidth, int screenHeight)
        {
            float maxDist = (float)Math.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);
            float currentRadius = MathHelper.Lerp(maxDist, 0, EaseInCubic(_currentTransition.Progress));

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

            if (_maskEffect != null)
            {
                _maskEffect.Parameters["Center"].SetValue(screenFocalPoint);
                _maskEffect.Parameters["Radius"].SetValue(currentRadius);
                _maskEffect.Parameters["Resolution"].SetValue(new Vector2(screenWidth, screenHeight));

                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _maskEffect);
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
                _spriteBatch.End();
            }
            else
            {
                DrawCircleCloseFallback(screenWidth, screenHeight, screenFocalPoint, currentRadius);
            }
        }

        private void DrawCircleCloseFallback(int screenWidth, int screenHeight, Vector2 focalPoint, float currentRadius)
        {
            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);

            for (int y = 0; y < screenHeight; y += 2)
            {
                for (int x = 0; x < screenWidth; x += 2)
                {
                    Vector2 pixelPos = new Vector2(x, y);
                    float dist = Vector2.Distance(pixelPos, focalPoint);
                    
                    if (dist > currentRadius)
                    {
                        _spriteBatch.Draw(_pixel, new Rectangle(x, y, 2, 2), Color.Black);
                    }
                }
            }

            _spriteBatch.End();
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

        private float EaseInOutQuad(float t)
        {
            return t < 0.5f ? 2f * t * t : 1f - (float)Math.Pow(-2f * t + 2f, 2f) / 2f;
        }

        private float EaseInCubic(float t)
        {
            return t * t * t;
        }

        public void Dispose()
        {
            _pixel?.Dispose();
            _maskTarget?.Dispose();
            _maskEffect?.Dispose();
            _spriteBatch?.Dispose();
        }
    }
}
