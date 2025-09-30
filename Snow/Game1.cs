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
            _graphicsManager = new GraphicsManager(GraphicsDevice);
            _input = new InputManager();
            _postProcessing = new PostProcessing(GraphicsDevice, 320, 180);
            _camera = new Camera(320, 180, 320, 180);
            _particles = new ParticleSystem(GraphicsDevice, 5000);
            _debug = new DebugOverlay(GraphicsDevice);
            _random = new Random();
            _windTimer = 0f;

            LoadLevel("levels/level1.json");

            _player = new Player(new Vector2(160, 10), GraphicsDevice, _input, _graphicsManager, _particles);
            
            _debug.Enabled = true;
        }

        private void LoadLevel(string levelPath)
        {
            try
            {
                Console.WriteLine($"Loading level: {levelPath}");
                LevelData levelData = LevelLoader.LoadLevel(levelPath);
                
                Console.WriteLine($"Level size: {levelData.GridWidth}x{levelData.GridHeight}");
                Console.WriteLine($"Tile size: {levelData.TileSize}");
                
                int solidCount = 0;
                int tileCount = 0;
                for (int y = 0; y < levelData.GridHeight; y++)
                {
                    for (int x = 0; x < levelData.GridWidth; x++)
                    {
                        if (levelData.WorldData[y][x] > 0)
                            tileCount++;
                        if (levelData.CollisionData[y][x])
                            solidCount++;
                    }
                }
                Console.WriteLine($"Tiles placed: {tileCount}");
                Console.WriteLine($"Solid tiles: {solidCount}");

                Texture2D tilesetImage = _graphicsManager.LoadTexture("tileset", "assets/Tilesheet.png");
                
                List<Texture2D> tileset = new List<Texture2D>();
                int tilesX = tilesetImage.Width / levelData.TileSize;
                int tilesY = tilesetImage.Height / levelData.TileSize;
                
                Console.WriteLine($"Tileset: {tilesX}x{tilesY} tiles");

                for (int y = 0; y < tilesY; y++)
                {
                    for (int x = 0; x < tilesX; x++)
                    {
                        Texture2D tile = new Texture2D(GraphicsDevice, levelData.TileSize, levelData.TileSize);
                        Color[] data = new Color[levelData.TileSize * levelData.TileSize];
                        Rectangle sourceRect = new Rectangle(
                            x * levelData.TileSize,
                            y * levelData.TileSize,
                            levelData.TileSize,
                            levelData.TileSize
                        );
                        
                        Color[] fullData = new Color[tilesetImage.Width * tilesetImage.Height];
                        tilesetImage.GetData(fullData);
                        
                        for (int ty = 0; ty < levelData.TileSize; ty++)
                        {
                            for (int tx = 0; tx < levelData.TileSize; tx++)
                            {
                                int sourceIndex = (sourceRect.Y + ty) * tilesetImage.Width + (sourceRect.X + tx);
                                int destIndex = ty * levelData.TileSize + tx;
                                data[destIndex] = fullData[sourceIndex];
                            }
                        }
                        
                        tile.SetData(data);
                        tileset.Add(tile);
                    }
                }

                _tilemap = new Tilemap(levelData, tileset);
                Console.WriteLine("Tilemap created!");
            }
            catch (Exception e)
            {
                Console.WriteLine($"ERROR loading level: {e.Message}");
                Console.WriteLine(e.StackTrace);
            }
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

            _windTimer += deltaTime;
            if (_windTimer > 0.05f && _tilemap != null)
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

            _player.Update(gameTime);
            
            if (_tilemap != null)
            {
                CollisionSystem.ResolveCollision(_player, _tilemap, deltaTime);
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

            if (_tilemap != null)
            {
                _tilemap.Draw(_graphicsManager.SpriteBatch, _camera);
            }

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
            _particles?.Dispose();
            _postProcessing?.Dispose();
            _graphicsManager?.Dispose();
            base.UnloadContent();
        }
    }
}








