using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using Snow.Game;
using System;

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
        private SceneManager _sceneManager;

        private CanvasUI _canvasUI;

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

            _canvasUI = new CanvasUI(GraphicsDevice);
            _graphicsManager = new GraphicsManager(GraphicsDevice);
            _input = new InputManager();
            _postProcessing = new PostProcessing(GraphicsDevice, 320, 180);
            _camera = new Camera(320, 180, 320, 180);
            _particles = new ParticleSystem(GraphicsDevice, 5000);
            _debug = new DebugOverlay(GraphicsDevice);
            _random = new Random();
            _windTimer = 0f;

            _sceneManager = new SceneManager(GraphicsDevice, _graphicsManager);

            _console.Log("Snow Engine initialized");

            try
            {
                var factoryContext = new EntityFactoryContext
                {
                    Input = _input,
                    Particles = _particles
                };
                _sceneManager.SetFactoryContext(factoryContext);

                var scene = _sceneManager.LoadScene("scenes/forest_1.scene");
                _console.LogSuccess($"Scene loaded: {scene.Name}");

                _player = new Player(scene.PlayerSpawnPosition, GraphicsDevice, _input, _graphicsManager, _particles);
                _console.Log($"Player spawned at ({scene.PlayerSpawnPosition.X}, {scene.PlayerSpawnPosition.Y})");

                factoryContext.Tilemap = scene.Tilemap;
            }
            catch (Exception ex)
            {
                _console.LogError($"Failed to load scene: {ex.Message}");
                _console.Log("Stack trace: " + ex.StackTrace);
            }
            
            _debug.Enabled = true;
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
            
            if (_sceneManager.CurrentScene != null)
            {
                CollisionSystem.ResolveCollision(_player, _sceneManager.CurrentScene.Tilemap, deltaTime);
                _sceneManager.Update(gameTime);
            }

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

            if (_sceneManager.CurrentScene != null)
            {
                GraphicsDevice.Clear(_sceneManager.CurrentScene.BackgroundColor);
            }
            else
            {
                GraphicsDevice.Clear(Color.Black);
            }

            _graphicsManager.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetTransformMatrix()
            );

            _sceneManager.Draw(_graphicsManager.SpriteBatch, _camera, gameTime);

            _graphicsManager.SpriteBatch.End();

            _particles.Draw(_graphicsManager.SpriteBatch, _camera.GetTransformMatrix());

            _graphicsManager.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetTransformMatrix()
            );

            _sceneManager.DrawEntities(_graphicsManager.SpriteBatch, gameTime);
            _player.Draw(_graphicsManager.SpriteBatch, gameTime);

            _graphicsManager.SpriteBatch.End();

            _debug.DrawCollisionBoxes(_graphicsManager.SpriteBatch, _player, _camera);

            _postProcessing.EndGameRender();

            _postProcessing.ApplyPostProcessing();
            _postProcessing.DrawFinal();

            _debug.Draw(_graphicsManager.SpriteBatch, _player, _camera);


            _canvasUI.Draw(_graphicsManager.SpriteBatch, _player);


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
