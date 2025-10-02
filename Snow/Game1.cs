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
        private const bool FULLSCREEN_ENABLED = false;

        private GraphicsDeviceManager _graphics;
        private GraphicsManager _graphicsManager;
        private InputManager _input;
        private PostProcessing _postProcessing;
        private Camera _camera;
        private ParticleSystem _particles;
        private DebugOverlay _debugOverlay;
        private DebugUI _debugUI;
        private DebugConsole _console;
        private Player _player;
        private SceneManager _sceneManager;
        private CanvasUI _canvasUI;
        private Texture2D _pixel;
        private Texture2D _glowTexture;
        private TransitionManager _transitionManager;
        private MainMenu _mainMenu;
        private MonoGame.ImGuiNet.ImGuiRenderer _imGuiRenderer;

        private Random _random;
        private float _windTimer;
        private float _physicsParticleSpawnTimer;
        private KeyboardState _previousKeyboard;
        
        private Color _canvasModulate = new Color(0xc5, 0x00, 0xa8, 0xff);
        private bool _gameStarted = false;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.IsFullScreen = FULLSCREEN_ENABLED;
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

            _pixel = new Texture2D(GraphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            CreateGlowTexture();

            _canvasUI = new CanvasUI(GraphicsDevice);
            _debugUI = new DebugUI(GraphicsDevice);
            _graphicsManager = new GraphicsManager(GraphicsDevice);
            _input = new InputManager();
            _postProcessing = new PostProcessing(GraphicsDevice, 320, 180);
            _camera = new Camera(320, 180, 320, 180);
            _particles = new ParticleSystem(GraphicsDevice, 5000);
            _debugOverlay = new DebugOverlay(GraphicsDevice);
            _transitionManager = new TransitionManager(GraphicsDevice);
            _random = new Random();
            _windTimer = 0f;
            _physicsParticleSpawnTimer = 0f;

            // Initialize ImGui
            _imGuiRenderer = new MonoGame.ImGuiNet.ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            _sceneManager = new SceneManager(GraphicsDevice, _graphicsManager);

            _console.Log("Snow Engine initialized");
            _console.LogSuccess($"Bloom enabled: {_postProcessing.BloomEnabled}");
            _console.LogSuccess("ImGui initialized");
            _console.Log("Controls:");
            _console.Log("  Q/A - Bloom Threshold");
            _console.Log("  W/S - Bloom Intensity");
            _console.Log("  E/D - Canvas Modulate");
            _console.Log("  R   - Reset to defaults");

            _mainMenu = new MainMenu(GraphicsDevice, _camera, _particles, _transitionManager, StartGame);
            
            _debugUI.Enabled = false;
        }

        private void StartGame()
        {
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

                Vector2 playerCenter = new Vector2(143, 120);
                _transitionManager.StartTransition(TransitionType.CircleReveal, 3.5f, playerCenter, 2f);

                _gameStarted = true;
                _debugUI.Enabled = true;
                SpawnInitialFireflies();
            }
            catch (Exception ex)
            {
                _console.LogError($"Failed to load scene: {ex.Message}");
                _console.Log("Stack trace: " + ex.StackTrace);
            }
        }

        private void CreateGlowTexture()
        {
            int size = 32;
            _glowTexture = new Texture2D(GraphicsDevice, size, size);
            Color[] glowData = new Color[size * size];
            
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = (x - size / 2f) / (size / 2f);
                    float dy = (y - size / 2f) / (size / 2f);
                    float distance = (float)Math.Sqrt(dx * dx + dy * dy);
                    
                    float alpha = Math.Max(0, 1.0f - distance);
                    alpha = (float)Math.Pow(alpha, 1.5f);
                    
                    glowData[y * size + x] = new Color(1f, 1f, 1f, alpha);
                }
            }
            
            _glowTexture.SetData(glowData);
        }

        private void SpawnInitialFireflies()
        {
            Color[] pastelColors = new Color[]
            {
                new Color(255, 100, 150),
                new Color(100, 150, 255),
                new Color(150, 255, 100),
                new Color(255, 200, 100),
                new Color(200, 100, 255),
                new Color(100, 255, 200),
                new Color(255, 150, 100),
                new Color(150, 100, 255)
            };

            for (int i = 0; i < 20; i++)
            {
                float x = (float)_random.NextDouble() * 320f;
                float y = (float)_random.NextDouble() * 180f;
                Vector2 pos = new Vector2(x, y);
                Vector2 vel = new Vector2(
                    (float)(_random.NextDouble() - 0.5) * 12f,
                    (float)(_random.NextDouble() - 0.5) * 12f
                );
                
                Color color = pastelColors[_random.Next(pastelColors.Length)];
                
                _particles.EmitPhysicsParticle(pos, vel, color, 0.6f, 0.2f);
            }
        }

        protected override void Update(GameTime gameTime)
        {
            _input.Update();
            KeyboardState keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();

            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            _transitionManager.Update(deltaTime);

            if (!_gameStarted || _mainMenu.CurrentState == MenuState.MainMenu)
            {
                _mainMenu.Update(deltaTime);
                
                Rectangle worldBounds = new Rectangle(0, 0, 320, 180);
                _particles.Update(deltaTime, worldBounds, null);
                
                _camera.Update(deltaTime);
            }

            if (!_gameStarted)
            {
                _previousKeyboard = keyboard;
                base.Update(gameTime);
                return;
            }

            HandleBloomControls(keyboard, gameTime);

            _player.Update(gameTime);
            
            Tilemap tilemap = _sceneManager.CurrentScene?.Tilemap;
            Rectangle worldBounds2 = new Rectangle(0, 0, 320, 180);
            _particles.Update(deltaTime, worldBounds2, tilemap);

            Vector2 playerCenter = _player.Position + new Vector2(8, 12);
            float interactionRadius = 35f;
            float interactionStrength = 300f;
            _particles.ApplyRadialForce(playerCenter, interactionRadius, interactionStrength);

            if (_player.GetPhysics().IsDashing)
            {
                _particles.ApplyRadialForce(playerCenter, 60f, 600f);
            }

            _physicsParticleSpawnTimer += deltaTime;
            if (_physicsParticleSpawnTimer > 6f && _particles.GetPhysicsParticleCount() < 20)
            {
                Color[] pastelColors = new Color[]
                {
                    new Color(255, 100, 150),
                    new Color(100, 150, 255),
                    new Color(150, 255, 100),
                    new Color(255, 200, 100),
                    new Color(200, 100, 255),
                    new Color(100, 255, 200),
                    new Color(255, 150, 100),
                    new Color(150, 100, 255)
                };

                float x = (float)_random.NextDouble() * 320f;
                float y = (float)_random.NextDouble() < 0.5f ? -10f : 190f;
                Vector2 pos = new Vector2(x, y);
                Vector2 vel = new Vector2(
                    (float)(_random.NextDouble() - 0.5) * 10f,
                    y < 0 ? (float)_random.NextDouble() * 12f + 4f : -(float)_random.NextDouble() * 12f - 4f
                );
                
                Color color = pastelColors[_random.Next(pastelColors.Length)];
                
                _particles.EmitPhysicsParticle(pos, vel, color, 0.6f, 0.2f);
                _physicsParticleSpawnTimer = 0f;
            }
            
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
            _camera.Update(deltaTime);
            _debugUI.Update(gameTime);
            _canvasUI.Update(gameTime);

            _previousKeyboard = keyboard;
            base.Update(gameTime);
        }

        private void HandleBloomControls(KeyboardState keyboard, GameTime gameTime)
        {
            float adjustSpeed = (float)gameTime.ElapsedGameTime.TotalSeconds * 0.5f;

            if (keyboard.IsKeyDown(Keys.Q))
            {
                _postProcessing.BloomThreshold += adjustSpeed;
                _console.Log($"Bloom Threshold: {_postProcessing.BloomThreshold:F2}");
            }
            if (keyboard.IsKeyDown(Keys.A))
            {
                _postProcessing.BloomThreshold -= adjustSpeed;
                _postProcessing.BloomThreshold = Math.Max(0f, _postProcessing.BloomThreshold);
                _console.Log($"Bloom Threshold: {_postProcessing.BloomThreshold:F2}");
            }

            if (keyboard.IsKeyDown(Keys.W))
            {
                _postProcessing.BloomIntensity += adjustSpeed * 2f;
                _console.Log($"Bloom Intensity: {_postProcessing.BloomIntensity:F2}");
            }
            if (keyboard.IsKeyDown(Keys.S))
            {
                _postProcessing.BloomIntensity -= adjustSpeed * 2f;
                _postProcessing.BloomIntensity = Math.Max(0f, _postProcessing.BloomIntensity);
                _console.Log($"Bloom Intensity: {_postProcessing.BloomIntensity:F2}");
            }

            if (keyboard.IsKeyDown(Keys.E))
            {
                float r = _canvasModulate.R / 255f + adjustSpeed;
                float g = _canvasModulate.G / 255f + adjustSpeed;
                float b = _canvasModulate.B / 255f + adjustSpeed;
                _canvasModulate = new Color(
                    Math.Min(1f, r),
                    Math.Min(1f, g),
                    Math.Min(1f, b)
                );
                _console.Log($"Canvas Modulate: R={_canvasModulate.R}, G={_canvasModulate.G}, B={_canvasModulate.B}");
            }
            if (keyboard.IsKeyDown(Keys.D))
            {
                float r = _canvasModulate.R / 255f - adjustSpeed;
                float g = _canvasModulate.G / 255f - adjustSpeed;
                float b = _canvasModulate.B / 255f - adjustSpeed;
                _canvasModulate = new Color(
                    Math.Max(0f, r),
                    Math.Max(0f, g),
                    Math.Max(0f, b)
                );
                _console.Log($"Canvas Modulate: R={_canvasModulate.R}, G={_canvasModulate.G}, B={_canvasModulate.B}");
            }

            if (keyboard.IsKeyDown(Keys.R) && !_previousKeyboard.IsKeyDown(Keys.R))
            {
                _postProcessing.BloomThreshold = 0.5f;
                _postProcessing.BloomIntensity = 0.7f;
                _canvasModulate = new Color(0xc5, 0x00, 0xa8, 0xff);
                _console.LogSuccess("Reset to defaults!");
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            // Begin ImGui
            _imGuiRenderer.BeginLayout(gameTime);
            
            // Sample ImGui window
            ImGuiNET.ImGui.ShowDemoWindow();

            _postProcessing.BeginGameRender();

            GraphicsDevice.Clear(new Color(24, 22, 43));

            if (_gameStarted)
            {
                _graphicsManager.SpriteBatch.Begin(
                    samplerState: SamplerState.PointClamp,
                    transformMatrix: _camera.GetTransformMatrix()
                );

                _sceneManager.Draw(_graphicsManager.SpriteBatch, _camera, gameTime);

                _graphicsManager.SpriteBatch.End();

                _particles.DrawPhysicsParticlesGlow(_graphicsManager.SpriteBatch, _glowTexture, _camera.GetTransformMatrix());
                _particles.DrawPhysicsParticles(_graphicsManager.SpriteBatch, _camera.GetTransformMatrix());
                _particles.Draw(_graphicsManager.SpriteBatch, _camera.GetTransformMatrix());

                _graphicsManager.SpriteBatch.Begin(
                    samplerState: SamplerState.PointClamp,
                    transformMatrix: _camera.GetTransformMatrix()
                );

                _sceneManager.DrawEntities(_graphicsManager.SpriteBatch, gameTime);
                _player.Draw(_graphicsManager.SpriteBatch, gameTime);

                _graphicsManager.SpriteBatch.End();
            }
            else
            {
                _particles.Draw(_graphicsManager.SpriteBatch, null);
            }

            _postProcessing.EndGameRender();

            _postProcessing.ApplyPostProcessing();
            _postProcessing.DrawFinal(_canvasModulate);

            _transitionManager.Draw();

            if (!_gameStarted || _mainMenu.CurrentState == MenuState.MainMenu)
            {
                _mainMenu.Draw(_graphicsManager.SpriteBatch);
            }

            if (_gameStarted)
            {
                int entityCount = _sceneManager.CurrentScene?.Entities.Count ?? 0;
                _debugUI.Draw(_graphicsManager.SpriteBatch, _player, _camera, entityCount);
                _debugUI.DrawCollisionBoxes(_graphicsManager.SpriteBatch, _player, _camera, _pixel);
            }

            _canvasUI.Draw(_graphicsManager.SpriteBatch);

            // End ImGui
            _imGuiRenderer.EndLayout();

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _debugOverlay?.Dispose();
            _console?.Dispose();
            _particles?.Dispose();
            _postProcessing?.Dispose();
            _graphicsManager?.Dispose();
            _transitionManager?.Dispose();
            _pixel?.Dispose();
            _glowTexture?.Dispose();
            base.UnloadContent();
        }
    }
}
