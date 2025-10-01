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
        private bool _wasGrounded;
        private bool _wasDashing;
        private GlowSystem _glowSystem;

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
            _wasGrounded = false;
            _wasDashing = false;

            try
            {
                Effect bloomEffect = Content.Load<Effect>("Shaders/Bloom");
                Effect modulateEffect = Content.Load<Effect>("Shaders/Modulate");
                _postProcessing.LoadShaders(bloomEffect, modulateEffect);
                _postProcessing.CanvasModulate = new Color(0.55f, 0.35f, 0.85f, 1.0f);
                _postProcessing.BloomThreshold = 0.35f;
                _postProcessing.BloomIntensity = 3.5f;
                _console.LogSuccess("Shaders loaded");
            }
            catch (Exception ex)
            {
                _console.LogWarning($"Shaders not found: {ex.Message}");
            }

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
            
            _glowSystem = new GlowSystem(GraphicsDevice);
            
            _glowSystem.AddOrb(new Vector2(100, 100), 25f, new Color(255, 40, 255), 4.5f, 2.5f);
            _glowSystem.AddOrb(new Vector2(250, 80), 20f, new Color(80, 255, 255), 4.0f, 2.0f);
            _glowSystem.AddOrb(new Vector2(180, 140), 30f, new Color(255, 60, 200), 4.2f, 2.2f);
            _glowSystem.AddOrb(new Vector2(140, 90), 18f, new Color(255, 100, 255), 3.8f, 1.8f);
            
            _glowSystem.AddBlob(new Vector2(200, 160), 50fx, new Color(200, 80, 255), 3.5f);
            _glowSystem.AddBlob(new Vector2(120, 120), 60f, new Color(255, 60, 255), 3.2f);
            
            _console.Log("Glow orbs added to scene");
            
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

            var physics = _player.GetPhysics();

            if (physics.IsDashing && !_wasDashing)
            {
                _camera.Shake(2f, 0.15f);
            }

            if (physics.IsGrounded && !_wasGrounded)
            {
                _camera.Shake(1.5f, 0.12f);
            }

            _wasGrounded = physics.IsGrounded;
            _wasDashing = physics.IsDashing;

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
            _camera.Update(deltaTime);
            _particles.Update(deltaTime);
            _glowSystem.Update(deltaTime);
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

            _graphicsManager.SpriteBatch.Begin(
                samplerState: SamplerState.LinearClamp,
                blendState: BlendState.Additive,
                transformMatrix: _camera.GetTransformMatrix()
            );
            
            _glowSystem.DrawOrbs(_graphicsManager.SpriteBatch);
            
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
            _glowSystem?.Dispose();
            base.UnloadContent();
        }
    }
}


