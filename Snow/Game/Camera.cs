using Microsoft.Xna.Framework;
using System;

namespace Snow.Engine
{
    public enum CameraMode
    {
        Room,      
        Follow     
    }

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
        
        private CameraMode _mode;
        private int _roomWidth;
        private int _roomHeight;
        private Rectangle _currentRoom;
        private Rectangle _targetRoom;
        private bool _isTransitioning;
        private float _transitionProgress;
        private float _transitionSpeed = 3.0f;
        private Vector2 _transitionStartPos;
        
        private Vector2 _followTarget;
        private float _followSmoothSpeed = 5.0f;
        
        private Rectangle _bounds;
        private bool _hasBounds = false;

        public int ViewportWidth => _viewportWidth;
        public int ViewportHeight => _viewportHeight;
        public CameraMode Mode { get => _mode; set => _mode = value; }

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
            
            _mode = CameraMode.Room;
            _roomWidth = virtualWidth;
            _roomHeight = virtualHeight;
            _currentRoom = new Rectangle(0, 0, _roomWidth, _roomHeight);
            _targetRoom = _currentRoom;
            _isTransitioning = false;
        }

        public void SetBounds(int width, int height)
        {
            _bounds = new Rectangle(0, 0, width, height);
            _hasBounds = true;
        }

        public void SetRoomSize(int width, int height)
        {
            _roomWidth = width;
            _roomHeight = height;
        }

        public void Shake(float intensity, float duration)
        {
            _shakeIntensity = intensity;
            _shakeDuration = duration;
        }

        public void Follow(Vector2 targetPosition)
        {
            _followTarget = targetPosition;

            if (_mode == CameraMode.Room)
            {
                int roomX = (int)(targetPosition.X / _roomWidth) * _roomWidth;
                int roomY = (int)(targetPosition.Y / _roomHeight) * _roomHeight;

                if (_hasBounds)
                {
                    roomX = (int)MathHelper.Clamp(roomX, 0, Math.Max(0, _bounds.Width - _roomWidth));
                    roomY = (int)MathHelper.Clamp(roomY, 0, Math.Max(0, _bounds.Height - _roomHeight));
                }

                Rectangle newRoom = new Rectangle(roomX, roomY, _roomWidth, _roomHeight);

                if (newRoom != _currentRoom && !_isTransitioning)
                {
                    _targetRoom = newRoom;
                    _isTransitioning = true;
                    _transitionProgress = 0f;
                    _transitionStartPos = Position;
                }
            }
            else if (_mode == CameraMode.Follow)
            {
                // Follow mode 
            }
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
            
            if (_mode == CameraMode.Room)
            {
                if (_isTransitioning)
                {
                    _transitionProgress += deltaTime * _transitionSpeed;
                    
                    if (_transitionProgress >= 1.0f)
                    {
                        _transitionProgress = 1.0f;
                        _isTransitioning = false;
                        _currentRoom = _targetRoom;
                        Position = new Vector2(_currentRoom.X, _currentRoom.Y);
                    }
                    else
                    {
                        float t = EaseOutCubic(_transitionProgress);
                        Vector2 targetPos = new Vector2(_targetRoom.X, _targetRoom.Y);
                        Position = Vector2.Lerp(_transitionStartPos, targetPos, t);
                    }
                }
                else
                {
                    Position = new Vector2(_currentRoom.X, _currentRoom.Y);
                }
            }
            else if (_mode == CameraMode.Follow)
            {
                Vector2 targetPos = new Vector2(
                    _followTarget.X - _virtualWidth / 2,
                    _followTarget.Y - _virtualHeight / 2
                );
                
                if (_hasBounds)
                {
                    targetPos.X = MathHelper.Clamp(targetPos.X, _bounds.Left, Math.Max(_bounds.Left, _bounds.Right - _virtualWidth));
                    targetPos.Y = MathHelper.Clamp(targetPos.Y, _bounds.Top, Math.Max(_bounds.Top, _bounds.Bottom - _virtualHeight));
                }
                
                Position = Vector2.Lerp(Position, targetPos, deltaTime * _followSmoothSpeed);
            }
        }

        private float EaseOutCubic(float t)
        {
            float t1 = t - 1f;
            return t1 * t1 * t1 + 1f;
        }

        public Matrix GetTransformMatrix()
        {
            Vector2 effectivePosition = Position + _shakeOffset;
            
            return Matrix.CreateTranslation(new Vector3(-effectivePosition.X, -effectivePosition.Y, 0)) *
                   Matrix.CreateRotationZ(Rotation) *
                   Matrix.CreateScale(Zoom);
        }

        public Vector2 ScreenToWorld(Vector2 screenPosition)
        {
            Matrix inverseTransform = Matrix.Invert(GetTransformMatrix());
            return Vector2.Transform(screenPosition, inverseTransform);
        }
        
        public Rectangle GetCurrentRoom()
        {
            return _currentRoom;
        }
    }
}