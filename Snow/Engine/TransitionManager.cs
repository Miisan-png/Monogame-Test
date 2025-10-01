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

            CreateShader();
        }

        private void CreateShader()
        {
            string shaderCode = @"
                #if OPENGL
                    #define SV_POSITION POSITION
                    #define VS_SHADERMODEL vs_3_0
                    #define PS_SHADERMODEL ps_3_0
                #else
                    #define VS_SHADERMODEL vs_4_0_level_9_1
                    #define PS_SHADERMODEL ps_4_0_level_9_1
                #endif

                float2 Center;
                float Radius;
                float2 Resolution;

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
                    
                    float alpha = dist > Radius ? 1.0 : 0.0;
                    return float4(0, 0, 0, alpha);
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
                _maskEffect = new Effect(_graphicsDevice, System.Text.Encoding.UTF8.GetBytes(shaderCode));
                System.Console.WriteLine("Transition shader compiled successfully");
            }
            catch (Exception e)
            {
                System.Console.WriteLine($"Transition shader failed to compile: {e.Message}");
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

            if (_maskEffect == null)
            {
                System.Console.WriteLine("Mask effect is null - shader failed to compile");
                return;
            }

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

                _maskEffect.Parameters["Center"].SetValue(screenFocalPoint);
                _maskEffect.Parameters["Radius"].SetValue(smallRadius);
                _maskEffect.Parameters["Resolution"].SetValue(new Vector2(screenWidth, screenHeight));

                _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _maskEffect);
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
                _spriteBatch.End();
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

        private void DrawCircleReveal(int screenWidth, int screenHeight, Vector2 screenFocalPoint, float scale)
        {
            float maxDist = (float)Math.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);
            float startRadius = 30f * scale;
            float currentRadius = MathHelper.Lerp(startRadius, maxDist, EaseOutCubic(_currentTransition.Progress));

            _maskEffect.Parameters["Center"].SetValue(screenFocalPoint);
            _maskEffect.Parameters["Radius"].SetValue(currentRadius);
            _maskEffect.Parameters["Resolution"].SetValue(new Vector2(screenWidth, screenHeight));

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _maskEffect);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
            _spriteBatch.End();
        }

        private void DrawCircleClose(int screenWidth, int screenHeight, Vector2 screenFocalPoint, float scale)
        {
            float maxDist = (float)Math.Sqrt(screenWidth * screenWidth + screenHeight * screenHeight);
            float currentRadius = MathHelper.Lerp(maxDist, 0, EaseInCubic(_currentTransition.Progress));

            _maskEffect.Parameters["Center"].SetValue(screenFocalPoint);
            _maskEffect.Parameters["Radius"].SetValue(currentRadius);
            _maskEffect.Parameters["Resolution"].SetValue(new Vector2(screenWidth, screenHeight));

            _spriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp, null, null, _maskEffect);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, screenWidth, screenHeight), Color.White);
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
