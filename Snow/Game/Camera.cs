using Microsoft.Xna.Framework;
using System;

namespace Snow.Engine
{
    public class Camera
    {
        public Vector2 Position { get; set; }
        public float Zoom { get; set; }
        public float Rotation { get; set; }

        private int _viewportWidth;
        private int _viewportHeight;
        private int _virtualWidth;
        private int _virtualHeight;
        
        private Vector2 _shakeOffset;
        private float _shakeIntensity;
        private float _shakeDuration;
        private Random _random = new Random();

        public int ViewportWidth => _viewportWidth;
        public int ViewportHeight => _viewportHeight;

        public Camera(int viewportWidth, int viewportHeight, int virtualWidth, int virtualHeight)
        {
            _viewportWidth = viewportWidth;
            _viewportHeight = viewportHeight;
            _virtualWidth = virtualWidth;
            _virtualHeight = virtualHeight;
            Zoom = 1.0f;
            Rotation = 0f;
            Position = Vector2.Zero;
            _shakeOffset = Vector2.Zero;
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
        }

        public void Follow(Vector2 targetPosition)
        {
            Position = new Vector2(
                targetPosition.X - _virtualWidth / 2,
                targetPosition.Y - _virtualHeight / 2
            );
        }

        public void Update(float deltaTime)
        {
            if (_shakeDuration > 0)
            {
                _shakeDuration -= deltaTime;
                
                float offsetX = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
                float offsetY = (float)(_random.NextDouble() * 2 - 1) * _shakeIntensity;
                _shakeOffset = new Vector2(offsetX, offsetY);
                
                if (_shakeDuration <= 0)
                {
                    _shakeOffset = Vector2.Zero;
                }
            }
        }

        public Matrix GetTransformMatrix()
        {
            Vector2 effectivePosition = Position + _shakeOffset;
            
            return Matrix.CreateTranslation(new Vector3(-effectivePosition.X, -effectivePosition.Y, 0)) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(Zoom) *
                   Matrix.CreateTranslation(new Vector3(_viewportWidth / 2, _viewportHeight / 2, 0));
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            Matrix inverseTransform = Matrix.Invert(GetTransformMatrix());
            return Vector2.Transform(screenPosition, inverseTransform);
        }
    }
}