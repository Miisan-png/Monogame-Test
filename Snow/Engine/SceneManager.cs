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
        public List<Light> Lights { get; private set; }
        public List<AudioSource> AudioSources { get; private set; }
        public List<Trigger> Triggers { get; private set; }
        public List<SpawnPoint> SpawnPoints { get; private set; }
        public Vector2 PlayerSpawnPosition { get; private set; }
        public Color BackgroundColor { get; private set; }
        public CameraSettings CameraSettings { get; private set; }
        public Dictionary<string, object> Properties { get; private set; }

        private List<ParticleEmitterData> _particleEmitters;
        private GraphicsDevice _graphicsDevice;
        private GraphicsManager _graphics;
        private EntityFactoryContext _factoryContext;

        public Scene(SceneData data, GraphicsDevice graphicsDevice, GraphicsManager graphics, EntityFactoryContext factoryContext)
        {
            Name = data.Name;
            Entities = new List<IEntity>();
            Lights = new List<Light>();
            AudioSources = new List<AudioSource>();
            Triggers = new List<Trigger>();
            SpawnPoints = new List<SpawnPoint>();
            Properties = data.Properties;
            _graphicsDevice = graphicsDevice;
            _graphics = graphics;
            _factoryContext = factoryContext;
            _particleEmitters = data.ParticleEmitters;

            BackgroundColor = ParseColor(data.BackgroundColor);
            CameraSettings = CreateCameraSettings(data.CameraSettings);

            LoadTilemap(data.Tilemap, data.Tileset);
            LoadEntities(data.Entities);
            LoadLights(data.Lights);
            LoadAudioSources(data.AudioSources);
            LoadTriggers(data.Triggers);
            LoadSpawnPoints(data.SpawnPoints);
        }

        private CameraSettings CreateCameraSettings(CameraSettingsData data)
        {
            if (data == null)
                return new CameraSettings();
            
            return new CameraSettings
            {
                RoomWidth = data.RoomWidth,
                RoomHeight = data.RoomHeight,
                FollowMode = data.FollowMode,
                SmoothSpeed = data.SmoothSpeed,
                Properties = data.Properties
            };
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

        private void LoadLights(List<LightData> lightDataList)
        {
            foreach (var lightData in lightDataList)
            {
                Lights.Add(new Light
                {
                    Id = lightData.Id,
                    Position = new Vector2(lightData.X, lightData.Y),
                    Radius = lightData.Radius,
                    Color = ParseColor(lightData.Color),
                    Intensity = lightData.Intensity,
                    Type = lightData.Type,
                    Properties = lightData.Properties
                });
            }
        }

        private void LoadAudioSources(List<AudioSourceData> audioDataList)
        {
            foreach (var audioData in audioDataList)
            {
                AudioSources.Add(new AudioSource
                {
                    Id = audioData.Id,
                    Position = new Vector2(audioData.X, audioData.Y),
                    AudioFile = audioData.AudioFile,
                    Volume = audioData.Volume,
                    Loop = audioData.Loop,
                    AutoPlay = audioData.AutoPlay,
                    MinDistance = audioData.MinDistance,
                    MaxDistance = audioData.MaxDistance,
                    Properties = audioData.Properties
                });
            }
        }

        private void LoadTriggers(List<TriggerData> triggerDataList)
        {
            foreach (var triggerData in triggerDataList)
            {
                Triggers.Add(new Trigger
                {
                    Id = triggerData.Id,
                    Bounds = new Rectangle((int)triggerData.X, (int)triggerData.Y, (int)triggerData.Width, (int)triggerData.Height),
                    TriggerType = triggerData.TriggerType,
                    Action = triggerData.Action,
                    Properties = triggerData.Properties
                });
            }
        }

        private void LoadSpawnPoints(List<SpawnPointData> spawnDataList)
        {
            foreach (var spawnData in spawnDataList)
            {
                SpawnPoints.Add(new SpawnPoint
                {
                    Id = spawnData.Id,
                    Position = new Vector2(spawnData.X, spawnData.Y),
                    EntityType = spawnData.EntityType,
                    SpawnDelay = spawnData.SpawnDelay,
                    MaxSpawns = spawnData.MaxSpawns,
                    Properties = spawnData.Properties
                });
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
                else if (colorString.Length == 8)
                {
                    int r = System.Convert.ToInt32(colorString.Substring(0, 2), 16);
                    int g = System.Convert.ToInt32(colorString.Substring(2, 2), 16);
                    int b = System.Convert.ToInt32(colorString.Substring(4, 2), 16);
                    int a = System.Convert.ToInt32(colorString.Substring(6, 2), 16);
                    return new Color(r, g, b, a);
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

            foreach (var spawnPoint in SpawnPoints)
            {
                spawnPoint.Update(gameTime);
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

        public Light GetLightById(string id)
        {
            return Lights.Find(l => l.Id == id);
        }

        public AudioSource GetAudioSourceById(string id)
        {
            return AudioSources.Find(a => a.Id == id);
        }

        public Trigger GetTriggerById(string id)
        {
            return Triggers.Find(t => t.Id == id);
        }

        public SpawnPoint GetSpawnPointById(string id)
        {
            return SpawnPoints.Find(s => s.Id == id);
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

    public class Light
    {
        public string Id { get; set; }
        public Vector2 Position { get; set; }
        public float Radius { get; set; }
        public Color Color { get; set; }
        public float Intensity { get; set; }
        public string Type { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public bool IsActive { get; set; }

        public Light()
        {
            IsActive = true;
            Properties = new Dictionary<string, object>();
        }
    }

    public class AudioSource
    {
        public string Id { get; set; }
        public Vector2 Position { get; set; }
        public string AudioFile { get; set; }
        public float Volume { get; set; }
        public bool Loop { get; set; }
        public bool AutoPlay { get; set; }
        public float MinDistance { get; set; }
        public float MaxDistance { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public bool IsPlaying { get; set; }

        public AudioSource()
        {
            Properties = new Dictionary<string, object>();
        }
    }

    public class Trigger
    {
        public string Id { get; set; }
        public Rectangle Bounds { get; set; }
        public string TriggerType { get; set; }
        public string Action { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        public bool IsActive { get; set; }
        public bool HasTriggered { get; set; }

        public Trigger()
        {
            IsActive = true;
            Properties = new Dictionary<string, object>();
        }

        public bool CheckCollision(Rectangle other)
        {
            return IsActive && Bounds.Intersects(other);
        }
    }

    public class SpawnPoint
    {
        public string Id { get; set; }
        public Vector2 Position { get; set; }
        public string EntityType { get; set; }
        public float SpawnDelay { get; set; }
        public int MaxSpawns { get; set; }
        public Dictionary<string, object> Properties { get; set; }
        
        private float _timer;
        private int _spawnCount;

        public SpawnPoint()
        {
            Properties = new Dictionary<string, object>();
            _timer = 0f;
            _spawnCount = 0;
        }

        public void Update(GameTime gameTime)
        {
            if (MaxSpawns > 0 && _spawnCount >= MaxSpawns)
                return;

            _timer += (float)gameTime.ElapsedGameTime.TotalSeconds;
        }

        public bool CanSpawn()
        {
            if (MaxSpawns > 0 && _spawnCount >= MaxSpawns)
                return false;

            if (_timer >= SpawnDelay)
            {
                _timer = 0f;
                _spawnCount++;
                return true;
            }

            return false;
        }

        public void Reset()
        {
            _timer = 0f;
            _spawnCount = 0;
        }
    }

    public class CameraSettings
    {
        public int RoomWidth { get; set; }
        public int RoomHeight { get; set; }
        public string FollowMode { get; set; }
        public float SmoothSpeed { get; set; }
        public Dictionary<string, object> Properties { get; set; }

        public CameraSettings()
        {
            RoomWidth = 320;
            RoomHeight = 180;
            FollowMode = "room";
            SmoothSpeed = 0f;
            Properties = new Dictionary<string, object>();
        }
    }
}
