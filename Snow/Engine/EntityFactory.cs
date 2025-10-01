using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System;
using System.Collections.Generic;

namespace Snow.Engine
{
    public interface IEntity
    {
        string Id { get; }
        Vector2 Position { get; set; }
        void Update(GameTime gameTime);
        void Draw(SpriteBatch spriteBatch, GameTime gameTime);
    }

    public static class EntityFactory
    {
        private static Dictionary<string, Func<EntityData, GraphicsDevice, GraphicsManager, EntityFactoryContext, IEntity>> _entityCreators;

        static EntityFactory()
        {
            _entityCreators = new Dictionary<string, Func<EntityData, GraphicsDevice, GraphicsManager, EntityFactoryContext, IEntity>>();
            RegisterDefaultEntities();
        }

        private static void RegisterDefaultEntities()
        {
            Register("PlayerSpawn", CreatePlayerSpawn);
            Register("Slime", CreateSlime);
            Register("Coin", CreateCoin);
            Register("Chest", CreateChest);
            Register("Spike", CreateSpike);
        }

        public static void Register(string type, Func<EntityData, GraphicsDevice, GraphicsManager, EntityFactoryContext, IEntity> creator)
        {
            _entityCreators[type] = creator;
        }

        public static IEntity CreateEntity(EntityData data, GraphicsDevice graphicsDevice, GraphicsManager graphics, EntityFactoryContext context)
        {
            if (_entityCreators.TryGetValue(data.Type, out var creator))
            {
                return creator(data, graphicsDevice, graphics, context);
            }

            throw new Exception($"Unknown entity type: {data.Type}");
        }

        private static IEntity CreatePlayerSpawn(EntityData data, GraphicsDevice device, GraphicsManager graphics, EntityFactoryContext context)
        {
            return new PlayerSpawnEntity(data.Id, new Vector2(data.X, data.Y));
        }

        private static IEntity CreateSlime(EntityData data, GraphicsDevice device, GraphicsManager graphics, EntityFactoryContext context)
        {
            float patrolDistance = data.Properties.ContainsKey("patrol_distance") 
                ? Convert.ToSingle(data.Properties["patrol_distance"]) 
                : 50f;
            float speed = data.Properties.ContainsKey("speed") 
                ? Convert.ToSingle(data.Properties["speed"]) 
                : 30f;

            return new SlimeEntity(data.Id, new Vector2(data.X, data.Y), graphics, device, patrolDistance, speed);
        }

        private static IEntity CreateCoin(EntityData data, GraphicsDevice device, GraphicsManager graphics, EntityFactoryContext context)
        {
            int value = data.Properties.ContainsKey("value") 
                ? Convert.ToInt32(data.Properties["value"]) 
                : 10;

            return new CoinEntity(data.Id, new Vector2(data.X, data.Y), graphics, device, value);
        }

        private static IEntity CreateChest(EntityData data, GraphicsDevice device, GraphicsManager graphics, EntityFactoryContext context)
        {
            return new ChestEntity(data.Id, new Vector2(data.X, data.Y), graphics, device);
        }

        private static IEntity CreateSpike(EntityData data, GraphicsDevice device, GraphicsManager graphics, EntityFactoryContext context)
        {
            return new SpikeEntity(data.Id, new Vector2(data.X, data.Y), graphics, device);
        }
    }

    public class EntityFactoryContext
    {
        public InputManager Input { get; set; }
        public ParticleSystem Particles { get; set; }
        public Tilemap Tilemap { get; set; }
    }

    public class PlayerSpawnEntity : IEntity
    {
        public string Id { get; private set; }
        public Vector2 Position { get; set; }

        public PlayerSpawnEntity(string id, Vector2 position)
        {
            Id = id;
            Position = position;
        }

        public void Update(GameTime gameTime) { }
        public void Draw(SpriteBatch spriteBatch, GameTime gameTime) { }
    }

    public class SlimeEntity : Actor, IEntity
    {
        public string Id { get; private set; }
        private GraphicsManager _graphics;
        private AnimatedSprite _sprite;
        private float _patrolDistance;
        private float _speed;
        private Vector2 _startPosition;
        private float _direction;
        private PhysicsComponent _physics;

        public SlimeEntity(string id, Vector2 position, GraphicsManager graphics, GraphicsDevice device, float patrolDistance, float speed) 
            : base(position)
        {
            Id = id;
            _graphics = graphics;
            _patrolDistance = patrolDistance;
            _speed = speed;
            _startPosition = position;
            _direction = 1f;

            _physics = new PhysicsComponent();
            _physics.MoveSpeed = speed;

            _sprite = new AnimatedSprite();
            LoadAnimations();
        }

        private void LoadAnimations()
        {
            Animation idle = new Animation("idle", 0.15f, true);
            idle.Frames.Add(CreateColoredTexture(_graphics, 16, 16, new Color(100, 200, 100)));
            _sprite.AddAnimation(idle);
            _sprite.Play("idle");
        }

        private Texture2D CreateColoredTexture(GraphicsManager graphics, int width, int height, Color color)
        {
            var texture = new Texture2D(graphics.SpriteBatch.GraphicsDevice, width, height);
            Color[] data = new Color[width * height];
            for (int i = 0; i < data.Length; i++)
                data[i] = color;
            texture.SetData(data);
            return texture;
        }

        public override void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            float distanceFromStart = Position.X - _startPosition.X;
            
            if (distanceFromStart > _patrolDistance)
                _direction = -1f;
            else if (distanceFromStart < -_patrolDistance)
                _direction = 1f;

            Velocity = new Vector2(_direction * _speed, Velocity.Y);
            Position += Velocity * deltaTime;

            _sprite.FlipX = _direction < 0;
            _sprite.Update(deltaTime);
        }

        public override void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            _sprite.Draw(spriteBatch, Position, Color.White);
        }
    }

    public class CoinEntity : IEntity
    {
        public string Id { get; private set; }
        public Vector2 Position { get; set; }
        private Texture2D _texture;
        private int _value;
        private float _bobTimer;
        public bool IsCollected { get; set; }

        public CoinEntity(string id, Vector2 position, GraphicsManager graphics, GraphicsDevice device, int value)
        {
            Id = id;
            Position = position;
            _value = value;
            _bobTimer = 0f;
            IsCollected = false;

            _texture = new Texture2D(device, 8, 8);
            Color[] data = new Color[64];
            for (int i = 0; i < data.Length; i++)
                data[i] = new Color(255, 215, 0);
            _texture.SetData(data);
        }

        public void Update(GameTime gameTime)
        {
            if (IsCollected) return;

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _bobTimer += deltaTime * 3f;
        }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            if (IsCollected) return;

            float bobOffset = (float)Math.Sin(_bobTimer) * 2f;
            Vector2 drawPos = Position + new Vector2(0, bobOffset);
            spriteBatch.Draw(_texture, drawPos, Color.White);
        }
    }

    public class ChestEntity : IEntity
    {
        public string Id { get; private set; }
        public Vector2 Position { get; set; }
        private Texture2D _texture;
        public bool IsOpened { get; set; }

        public ChestEntity(string id, Vector2 position, GraphicsManager graphics, GraphicsDevice device)
        {
            Id = id;
            Position = position;
            IsOpened = false;

            _texture = new Texture2D(device, 16, 16);
            Color[] data = new Color[256];
            for (int i = 0; i < data.Length; i++)
                data[i] = new Color(139, 69, 19);
            _texture.SetData(data);
        }

        public void Update(GameTime gameTime) { }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Draw(_texture, Position, Color.White);
        }
    }

    public class SpikeEntity : IEntity
    {
        public string Id { get; private set; }
        public Vector2 Position { get; set; }
        private Texture2D _texture;

        public SpikeEntity(string id, Vector2 position, GraphicsManager graphics, GraphicsDevice device)
        {
            Id = id;
            Position = position;

            _texture = new Texture2D(device, 16, 16);
            Color[] data = new Color[256];
            for (int i = 0; i < data.Length; i++)
                data[i] = new Color(150, 150, 150);
            _texture.SetData(data);
        }

        public void Update(GameTime gameTime) { }

        public void Draw(SpriteBatch spriteBatch, GameTime gameTime)
        {
            spriteBatch.Draw(_texture, Position, Color.White);
        }
    }
}
