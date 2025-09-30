using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Snow.Engine
{
    public abstract class Actor
    {
        public Vector2 Position { get; set; }
        public Vector2 Velocity { get; set; }
        public Vector2 Scale { get; set; }
        public float Rotation { get; set; }
        public bool IsActive { get; set; }

        protected Actor(Vector2 position)
        {
            Position = position;
            Velocity = Vector2.Zero;
            Scale = Vector2.One;
            Rotation = 0f;
            IsActive = true;
        }

        public virtual void Update(GameTime gameTime)
        {
            if (!IsActive) return;
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            Position += Velocity * deltaTime;
        }

        public abstract void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }
}
