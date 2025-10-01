using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Collections.Generic;

namespace Snow.Engine
{
    public class Scene
    {
        public string Name { get; private set; }
        public Tilemap Tilemap { get; private set; }
        public List<IEntity> Entities { get; private set; }
        public Vector2 PlayerSpawnPosition { get; private set; }
        public Color BackgroundColor { get; private set; }

        private List<ParticleEmitterData> _particleEmitters;
        private GraphicsDevice _graphicsDevice;
        private GraphicsManager _graphics;
        private EntityFactoryContext _factoryContext;

        public Scene(SceneData data, GraphicsDevice graphicsDevice, GraphicsManager graphics, EntityFactoryContext factoryContext)
        {
            Name = data.Name;
            Entities = new List<IEntity>();
            _graphicsDevice = graphicsDevice;
            _graphics = graphics;
            _factoryContext = factoryContext;
            _particleEmitters = data.ParticleEmitters;

            BackgroundColor = ParseColor(data.BackgroundColor);

            LoadTilemap(data.Tilemap, data.Tileset);
            LoadEntities(data.Entities);
        }

        private void LoadTilemap(string tilemapPath, string tilesetPath)
        {
            LevelData levelData = LevelLoader.LoadLevel(tilemapPath);
            List<Texture2D> tileset = LoadTileset(tilesetPath, levelData.TileSize);
            Tilemap = new Tilemap(levelData, tileset);
            _factoryContext.Tilemap = Tilemap;
        }

        private List<Texture2D> LoadTileset(string tilesetPath, int tileSize)
        {
            List<Texture2D> tiles = new List<Texture2D>();
            Texture2D tilesetImage = _graphics.LoadTexture("tileset", tilesetPath);
            
            int tilesX = tilesetImage.Width / tileSize;
            int tilesY = tilesetImage.Height / tileSize;
            
            Color[] tilesetData = new Color[tilesetImage.Width * tilesetImage.Height];
            tilesetImage.GetData(tilesetData);
            
            for (int y = 0; y < tilesY; y++)
            {
                for (int x = 0; x < tilesX; x++)
                {
                    Texture2D tile = new Texture2D(_graphicsDevice, tileSize, tileSize);
                    Color[] tileData = new Color[tileSize * tileSize];
                    
                    for (int ty = 0; ty < tileSize; ty++)
                    {
                        for (int tx = 0; tx < tileSize; tx++)
                        {
                            int sourceX = x * tileSize + tx;
                            int sourceY = y * tileSize + ty;
                            int sourceIndex = sourceY * tilesetImage.Width + sourceX;
                            int destIndex = ty * tileSize + tx;
                            
                            tileData[destIndex] = tilesetData[sourceIndex];
                        }
                    }
                    
                    tile.SetData(tileData);
                    tiles.Add(tile);
                }
            }
            
            return tiles;
        }

        private void LoadEntities(List<EntityData> entityDataList)
        {
            foreach (var entityData in entityDataList)
            {
                if (entityData.Type == "PlayerSpawn")
                {
                    PlayerSpawnPosition = new Vector2(entityData.X, entityData.Y);
                    continue;
                }

                var entity = EntityFactory.CreateEntity(entityData, _graphicsDevice, _graphics, _factoryContext);
                Entities.Add(entity);
            }
        }

        private Color ParseColor(string colorString)
        {
            if (string.IsNullOrEmpty(colorString))
                return Color.Black;

            if (colorString.StartsWith("#"))
            {
                colorString = colorString.Substring(1);
                
                if (colorString.Length == 6)
                {
                    int r = System.Convert.ToInt32(colorString.Substring(0, 2), 16);
                    int g = System.Convert.ToInt32(colorString.Substring(2, 2), 16);
                    int b = System.Convert.ToInt32(colorString.Substring(4, 2), 16);
                    return new Color(r, g, b);
                }
            }

            return Color.Black;
        }

        public void Update(GameTime gameTime)
        {
            foreach (var entity in Entities)
            {
                entity.Update(gameTime);
            }
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera)
        {
            Tilemap.Draw(spriteBatch, camera);
        }

        public void DrawEntities(SpriteBatch spriteBatch, GameTime gameTime)
        {
            foreach (var entity in Entities)
            {
                entity.Draw(spriteBatch, gameTime);
            }
        }

        public IEntity GetEntityById(string id)
        {
            return Entities.Find(e => e.Id == id);
        }

        public T GetEntityById<T>(string id) where T : class, IEntity
        {
            return Entities.Find(e => e.Id == id) as T;
        }

        public List<T> GetEntitiesByType<T>() where T : class, IEntity
        {
            List<T> result = new List<T>();
            foreach (var entity in Entities)
            {
                if (entity is T typedEntity)
                    result.Add(typedEntity);
            }
            return result;
        }
    }

    public class SceneManager
    {
        private Scene _currentScene;
        private GraphicsDevice _graphicsDevice;
        private GraphicsManager _graphics;
        private EntityFactoryContext _factoryContext;

        public Scene CurrentScene => _currentScene;

        public SceneManager(GraphicsDevice graphicsDevice, GraphicsManager graphics)
        {
            _graphicsDevice = graphicsDevice;
            _graphics = graphics;
            _factoryContext = new EntityFactoryContext();
        }

        public void SetFactoryContext(EntityFactoryContext context)
        {
            _factoryContext = context;
        }

        public Scene LoadScene(string scenePath)
        {
            SceneData sceneData = SceneParser.ParseScene(scenePath);
            _currentScene = new Scene(sceneData, _graphicsDevice, _graphics, _factoryContext);
            return _currentScene;
        }

        public void Update(GameTime gameTime)
        {
            _currentScene?.Update(gameTime);
        }

        public void Draw(SpriteBatch spriteBatch, Camera camera, GameTime gameTime)
        {
            if (_currentScene == null)
                return;

            _currentScene.Draw(spriteBatch, camera);
        }

        public void DrawEntities(SpriteBatch spriteBatch, GameTime gameTime)
        {
            _currentScene?.DrawEntities(spriteBatch, gameTime);
        }
    }
}
