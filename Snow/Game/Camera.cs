using Microsoft.Xna.Framework;
using System;

namespace Snow.Engine
{
    public class Camera
    {
        public Vector2 Position { get; set; }
        public int RoomWidth { get; set; }
        public int RoomHeight { get; set; }
        public int ViewWidth { get; private set; }
        public int ViewHeight { get; private set; }

        private Vector2 _currentRoomOrigin;
        private float _shakeAmount;
        private float _shakeTimer;
        private Random _random;
        private Vector2 _shakeOffset;

        public Camera(int viewWidth, int viewHeight, int roomWidth, int roomHeight)
        {
            ViewWidth = viewWidth;
            ViewHeight = viewHeight;
            RoomWidth = roomWidth;
            RoomHeight = roomHeight;
            Position = Vector2.Zero;
            _currentRoomOrigin = Vector2.Zero;
            _random = new Random();
            _shakeAmount = 0f;
            _shakeTimer = 0f;
            _shakeOffset = Vector2.Zero;
        }

        public void Shake(float amount, float duration)
        {
            _shakeAmount = amount;
            _shakeTimer = duration;
        }

        public void Update(float deltaTime)
        {
            if (_shakeTimer > 0)
            {
                _shakeTimer -= deltaTime;
                
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                float strength = _shakeAmount * (_shakeTimer / 0.2f);
                _shakeOffset = new Vector2(
                    (float)Math.Cos(angle) * strength,
                    (float)Math.Sin(angle) * strength
                );
            }
            else
            {
                _shakeOffset = Vector2.Zero;
            }
        }

        public void Follow(Vector2 targetPosition)
        {
            int roomX = (int)(targetPosition.X / RoomWidth) * RoomWidth;
            int roomY = (int)(targetPosition.Y / RoomHeight) * RoomHeight;

            _currentRoomOrigin = new Vector2(roomX, roomY);
            Position = _currentRoomOrigin;
        }

        public Matrix GetTransformMatrix()
        {
            return Matrix.CreateTranslation(-Position.X + _shakeOffset.X, -Position.Y + _shakeOffset.Y, 0);
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            return screenPosition + Position;
        }

        public Vector2 WorldToScreen(Vector2 worldPosition)
        {
            return worldPosition - Position;
        }

        public bool IsInView(Vector2 position, float margin = 32f)
        {
            return position.X >= Position.X - margin &&
                   position.X <= Position.X + ViewWidth + margin &&
                   position.Y >= Position.Y - margin &&
                   position.Y <= Position.Y + ViewHeight + margin;
        }
    }
}
