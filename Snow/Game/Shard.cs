// Snow/Game/Shard.cs
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Snow.Engine;
using System;

namespace Snow.Game
{
    public class Shard : IEntity
    {
        public string Id { get; private set; }
        public Vector2 Position { get; set; }
        
        private Texture2D _texture;
        private float _bobTimer;
        private float _bobSpeed;
        private float _bobAmount;
        private Vector2 _basePosition;
        private bool _isCollected;
        private float _collectTimer;
        private Vector2 _collectVelocity;
        private Vector2 _targetPosition;
        private float _colorShiftTimer;
        private Color _currentColor;
        private ParticleSystem _particles;
        private Camera _camera;
        
        private Rectangle _collectionArea;
        private float _collectionRadius;
        
        private Random _random;
        
        private float _scale;
        private float _rotation;
        private float _rotationSpeed;
        
        public bool IsActive { get; set; }
        public bool IsFullyCollected { get; private set; }

        public Shard(string id, Vector2 position, GraphicsDevice device, ParticleSystem particles, Camera camera)
        {
            Id = id;
            Position = position;
            _basePosition = position;
            _particles = particles;
            _camera = camera;
            IsActive = true;
            IsFullyCollected = false;
            _isCollected = false;
            _collectTimer = 0f;
            _random = new Random(id.GetHashCode());
            
            _bobTimer = (float)_random.NextDouble() * MathHelper.TwoPi;
            _bobSpeed = 2.0f + (float)_random.NextDouble() * 1.0f;
            _bobAmount = 3.0f;
            
            _collectionRadius = 32f;
            _collectionArea = new Rectangle(
                (int)(position.X - _collectionRadius),
                (int)(position.Y - _collectionRadius),
                (int)(_collectionRadius * 2),
                (int)(_collectionRadius * 2)
            );
            
            _scale = 1.0f;
            _rotation = 0f;
            _rotationSpeed = 2.0f;
            
            _currentColor = new Color(100, 200, 255);
            _colorShiftTimer = 0f;
            
            _texture = new Texture2D(device, 16, 16);
            try
            {
                using (var stream = System.IO.File.OpenRead("assets/solo/bubble.png"))
                {
                    _texture = Texture2D.FromStream(device, stream);
                }
            }
            catch
            {
                Color[] data = new Color[16 * 16];
                for (int i = 0; i < data.Length; i++)
                    data[i] = new Color(100, 200, 255);
                _texture.SetData(data);
            }
        }

        public void Update(GameTime gameTime)
        {
            if (!IsActive || IsFullyCollected) return;
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            
            if (!_isCollected)
            {
                _bobTimer += deltaTime * _bobSpeed;
                float bobOffset = (float)Math.Sin(_bobTimer) * _bobAmount;
                Position = _basePosition + new Vector2(0, bobOffset);
                
                _rotation += deltaTime * _rotationSpeed;
                
                _colorShiftTimer += deltaTime * 3.0f;
                float colorPulse = (float)Math.Sin(_colorShiftTimer) * 0.3f + 0.7f;
                _currentColor = new Color(
                    (byte)(100 + colorPulse * 155),
                    (byte)(200 + colorPulse * 55),
                    (byte)(255)
                );
                
                _collectionArea = new Rectangle(
                    (int)(Position.X - _collectionRadius),
                    (int)(Position.Y - _collectionRadius),
                    (int)(_collectionRadius * 2),
                    (int)(_collectionRadius * 2)
                );
            }
            else
            {
                _collectTimer += deltaTime;
                
                float followSpeed = 300f + _collectTimer * 200f;
                Vector2 direction = _targetPosition - Position;
                float distance = direction.Length();
                
                if (distance > 1f)
                {
                    direction.Normalize();
                    _collectVelocity = direction * followSpeed;
                    Position += _collectVelocity * deltaTime;
                }
                else
                {
                    Position = _targetPosition;
                }
                
                _rotation += deltaTime * 15f;
                _scale = MathHelper.Lerp(_scale, 0.3f, deltaTime * 8f);
                
                _colorShiftTimer += deltaTime * 12.0f;
                Color[] pastelColors = new Color[]
                {
                    new Color(255, 150, 200),
                    new Color(150, 200, 255),
                    new Color(200, 255, 150),
                    new Color(255, 200, 150),
                    new Color(200, 150, 255)
                };
                
                int colorIndex = (int)(_colorShiftTimer * 2) % pastelColors.Length;
                int nextColorIndex = (colorIndex + 1) % pastelColors.Length;
                float t = (_colorShiftTimer * 2) % 1f;
                _currentColor = Color.Lerp(pastelColors[colorIndex], pastelColors[nextColorIndex], t);
                
                if (distance < 5f)
                {
                    IsFullyCollected = true;
                    SpawnCollectParticles();
                    _camera.Shake(3f, 0.15f);
                }
            }
        }

        public void Collect(Vector2 playerPosition)
        {
            if (_isCollected || IsFullyCollected) return;
            
            _isCollected = true;
            _collectTimer = 0f;
            _targetPosition = playerPosition + new Vector2(0, -12);
            
            for (int i = 0; i < 8; i++)
            {
                float angle = (float)(i / 8.0 * Math.PI * 2);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * 40f,
                    (float)Math.Sin(angle) * 40f
                );
                _particles.Emit(Position, vel, new Color(150, 220, 255), 0.4f, 1.2f);
            }
        }

        public bool CheckCollision(Rectangle playerBounds)
        {
            if (_isCollected || IsFullyCollected) return false;
            return _collectionArea.Intersects(playerBounds);
        }

        private void SpawnCollectParticles()
        {
            for (int i = 0; i < 20; i++)
            {
                float angle = (float)(_random.NextDouble() * Math.PI * 2);
                float speed = (float)(_random.NextDouble() * 80 + 40);
                Vector2 vel = new Vector2(
                    (float)Math.Cos(angle) * speed,
                    (float)Math.Sin(angle) * speed
                );
                
                Color[] colors = new Color[]
                {
                    new Color(255, 150, 200),
                    new Color(150, 200, 255),
                    new Color(200, 255, 150),
                    new Color(255, 255, 150)
                };
                
                _particles.Emit(Position, vel, colors[_random.Next(colors.Length)], 0.6f, 1.5f);
            }
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (!IsActive || IsFullyCollected) return;
            
            Vector2 origin = new Vector2(_texture.Width / 2, _texture.Height / 2);
            Vector2 drawPos = Position + new Vector2(8, 8);
            
            spriteBatch.Draw(
                _texture,
                drawPos,
                null,
                _currentColor,
                _rotation,
                origin,
                _scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}