using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using Snow.Game;
using System;
using System.Collections.Generic;

namespace Snow
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private GraphicsDeviceManager _graphics;
        private GraphicsManager _graphicsManager;
        private InputManager _input;
        private PostProcessing _postProcessing;
        private Camera _camera;
        private ParticleSystem _particles;
        private DebugOverlay _debug;
        private DebugConsole _console;
        private Player _player;
        private Tilemap _tilemap;
        private Random _random;
        private float _windTimer;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = 1280;
            _graphics.PreferredBackBufferHeight = 720;
            _graphics.ApplyChanges();

            Window.AllowUserResizing = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _console = new DebugConsole();
            _console.Open();

            _graphicsManager = new GraphicsManager(GraphicsDevice);
            _input = new InputManager();
            _postProcessing = new PostProcessing(GraphicsDevice, 320, 180);
            _camera = new Camera(320, 180, 320, 180);
            _particles = new ParticleSystem(GraphicsDevice, 5000);
            _debug = new DebugOverlay(GraphicsDevice);
            _random = new Random();
            _windTimer = 0f;

            _console.Log("Snow Engine initialized");

            try
            {
                LoadLevel("levels/lvl.json", "assets/world_tileset.png");
                _console.LogSuccess("Level loaded successfully");
            }
            catch (Exception ex)
            {
                _console.LogError($"Failed to load level: {ex.Message}");
                _console.Log("Using fallback infinite floor");
                CreateInfiniteFloor();
            }

            _player = new Player(new Vector2(160, 130), GraphicsDevice, _input, _graphicsManager, _particles);
            _console.Log("Player spawned at (160, 130)");
            
            _debug.Enabled = true;
        }

        private void LoadLevel(string levelPath, string tilesetPath)
        {
            LevelData levelData = LevelLoader.LoadLevel(levelPath);
            List<Texture2D> tileset = LoadTileset(tilesetPath, levelData.TileSize);
            _tilemap = new Tilemap(levelData, tileset);
            
            _console.Log($"Level: {levelData.GridWidth}x{levelData.GridHeight}, TileSize: {levelData.TileSize}");
            _console.Log($"Tileset: {tileset.Count} tiles loaded");
        }

        private List<Texture2D> LoadTileset(string tilesetPath, int tileSize)
        {
            List<Texture2D> tiles = new List<Texture2D>();
            Texture2D tilesetImage = _graphicsManager.LoadTexture("tileset", tilesetPath);
            
            int tilesX = tilesetImage.Width / tileSize;
            int tilesY = tilesetImage.Height / tileSize;
            
            Color[] tilesetData = new Color[tilesetImage.Width * tilesetImage.Height];
            tilesetImage.GetData(tilesetData);
            
            for (int y = 0; y < tilesY; y++)
            {
                for (int x = 0; x < tilesX; x++)
                {
                    Texture2D tile = new Texture2D(GraphicsDevice, tileSize, tileSize);
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

        private void CreateInfiniteFloor()
        {
            int width = 1000;
            int height = 50;
            int tileSize = 16;
            int floorY = 11;

            LevelData levelData = new LevelData
            {
                TileSize = tileSize,
                GridWidth = width,
                GridHeight = height,
                WorldData = new int[height][],
                CollisionData = new bool[height][]
            };

            for (int y = 0; y < height; y++)
            {
                levelData.WorldData[y] = new int[width];
                levelData.CollisionData[y] = new bool[width];
                
                for (int x = 0; x < width; x++)
                {
                    levelData.WorldData[y][x] = 0;
                    
                    if (y >= floorY)
                    {
                        levelData.CollisionData[y][x] = true;
                    }
                    else
                    {
                        levelData.CollisionData[y][x] = false;
                    }
                }
            }

            List<Texture2D> tileset = new List<Texture2D>();
            _tilemap = new Tilemap(levelData, tileset);
        }

        protected override void Update(GameTime gameTime)
        {
            _input.Update();

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (_input.IsKeyPressed(Keys.F3))
            {
                _debug.Enabled = !_debug.Enabled;
            }

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _player.Update(gameTime);
            
            CollisionSystem.ResolveCollision(_player, _tilemap, deltaTime);

            _windTimer += deltaTime;
            if (_windTimer > 0.05f)
            {
                float cameraLeft = _camera.Position.X;
                float cameraTop = _camera.Position.Y;

                for (int i = 0; i < 3; i++)
                {
                    float spawnX = cameraLeft - 10;
                    float spawnY = (float)(_random.NextDouble() * 180) + cameraTop;
                    Vector2 windVel = new Vector2((float)(_random.NextDouble() * 60 + 40), (float)(_random.NextDouble() * 10 - 5));
                    float life = (float)(_random.NextDouble() * 2.0 + 1.5);
                    
                    _particles.Emit(
                        new Vector2(spawnX, spawnY),
                        windVel,
                        new Color(180, 180, 200, 100),
                        life,
                        1f
                    );
                }
                
                _windTimer = 0f;
            }

            _camera.Follow(_player.Position);
            _particles.Update(deltaTime);
            _debug.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _postProcessing.BeginGameRender();

            _graphicsManager.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetTransformMatrix()
            );

            _tilemap.Draw(_graphicsManager.SpriteBatch, _camera);

            _graphicsManager.SpriteBatch.End();

            _particles.Draw(_graphicsManager.SpriteBatch, _camera.GetTransformMatrix());

            _graphicsManager.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetTransformMatrix()
            );
            _player.Draw(_graphicsManager.SpriteBatch, gameTime);
            _graphicsManager.SpriteBatch.End();

            _debug.DrawCollisionBoxes(_graphicsManager.SpriteBatch, _player, _camera);

            _postProcessing.EndGameRender();

            _postProcessing.ApplyPostProcessing();
            _postProcessing.DrawFinal();

            _debug.Draw(_graphicsManager.SpriteBatch, _player, _camera);

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _debug?.Dispose();
            _console?.Dispose();
            _particles?.Dispose();
            _postProcessing?.Dispose();
            _graphicsManager?.Dispose();
            base.UnloadContent();
        }
    }
}