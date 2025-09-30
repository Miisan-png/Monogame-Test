using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Snow.Engine
{
    public class Animation
    {
        public string Name { get; set; }
        public List<Texture2D> Frames { get; set; }
        public float FrameTime { get; set; }
        public bool Loop { get; set; }

        public Animation(string name, float frameTime = 0.1f, bool loop = true)
        {
            Name = name;
            Frames = new List<Texture2D>();
            FrameTime = frameTime;
            Loop = loop;
        }
    }

    public class AnimatedSprite
    {
        private Dictionary<string, Animation> _animations;
        private Animation _currentAnimation;
        private int _currentFrame;
        private float _frameTimer;
        private bool _isPlaying;

        public Vector2 Origin { get; set; }
        public bool FlipX { get; set; }

        public AnimatedSprite()
        {
            _animations = new Dictionary<string, Animation>();
            _currentFrame = 0;
            _frameTimer = 0f;
            _isPlaying = true;
            Origin = Vector2.Zero;
            FlipX = false;
        }

        public void AddAnimation(Animation animation)
        {
            _animations[animation.Name] = animation;
            if (_currentAnimation == null)
            {
                _currentAnimation = animation;
            }
        }

        public void Play(string animationName, bool restart = false)
        {
            if (_animations.TryGetValue(animationName, out Animation animation))
            {
                if (_currentAnimation != animation || restart)
                {
                    _currentAnimation = animation;
                    _currentFrame = 0;
                    _frameTimer = 0f;
                    _isPlaying = true;
                }
            }
        }

        public void Update(float deltaTime)
        {
            if (!_isPlaying || _currentAnimation == null || _currentAnimation.Frames.Count == 0)
                return;

            _frameTimer += deltaTime;

            if (_frameTimer >= _currentAnimation.FrameTime)
            {
                _frameTimer -= _currentAnimation.FrameTime;
                _currentFrame++;

                if (_currentFrame >= _currentAnimation.Frames.Count)
                {
                    if (_currentAnimation.Loop)
                    {
                        _currentFrame = 0;
                    }
                    else
                    {
                        _currentFrame = _currentAnimation.Frames.Count - 1;
                        _isPlaying = false;
                    }
                }
            }
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 position, Color color, float scale = 1f)
        {
            if (_currentAnimation == null || _currentAnimation.Frames.Count == 0)
                return;

            Texture2D currentTexture = _currentAnimation.Frames[_currentFrame];
            SpriteEffects effects = FlipX ? SpriteEffects.FlipHorizontally : SpriteEffects.None;

            spriteBatch.Draw(
                currentTexture,
                position,
                null,
                color,
                0f,
                Origin,
                scale,
                effects,
                0f
            );
        }

        public string GetCurrentAnimationName()
        {
            return _currentAnimation?.Name;
        }

        public bool IsAnimationFinished()
        {
            if (_currentAnimation == null || _currentAnimation.Loop)
                return false;

            return !_isPlaying;
        }
    }
}


