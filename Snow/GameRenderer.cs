using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using Snow.Game;
using System;
using System.Reflection;

namespace Snow
{
    public class GameRenderer
    {
        private GraphicsDevice _graphicsDevice;
        private GraphicsManager _graphicsManager;
        private InputManager _input;
        private PostProcessing _postProcessing;
        private Camera _camera;
        private ParticleSystem _particles;
        private DebugUI _debugUI;
        private DebugConsole _console;
        private Player _player;
        private SceneManager _sceneManager;
        private CanvasUI _canvasUI;
        private Texture2D _pixel;
        private Texture2D _glowTexture;
        private TransitionManager _transitionManager;
        private MainMenu _mainMenu;

        private Random _random;
        private float _physicsParticleSpawnTimer;
        
        private class WindParticle
        {
            public Vector2 Position;
            public Vector2 Velocity;
            public float Size;
            public float Depth;
            public Color Color;
            public bool Active;
            public float Rotation;
            public float RotationSpeed;
        }
        
        private WindParticle[] _windParticles;
        private const int MAX_WIND_PARTICLES = 30;
        private float _windSpawnTimer;
        private float _windSpawnRate;

        private GameHUD _gameHUD;
        private int _collectedShards;
        private int _totalShards;
        private KeyboardState _previousKeyboard;

        private Color _canvasModulate = new Color(0xc5, 0x00, 0xa8, 0xff);

        private bool _gameStarted = false;
        private bool _isPaused = false;
        private string _currentScenePath = "scenes/forest_1.scene";

        private float _respawnTimer = 0f;
        private const float RESPAWN_DELAY = 2.0f;

        public RenderTarget2D GameRenderTarget { get; private set; }
        public bool IsGameStarted => _gameStarted;
        public bool IsPaused { get => _isPaused; set => _isPaused = value; }

        public void ReloadLevel()
        {
            if (!_gameStarted) return;

            try
            {
                var factoryContext = new EntityFactoryContext
                {
                    Input = _input,
                    Particles = _particles
                };
                _sceneManager.SetFactoryContext(factoryContext);

                var scene = _sceneManager.LoadScene(_currentScenePath);
                _console.LogSuccess($"Level reloaded: {scene.Name}");

                var sceneData = SceneParser.ParseScene(_currentScenePath);
                EntityData playerSpawnData = null;
                foreach (var entity in sceneData.Entities)
                {
                    if (entity.Id == "playerspawn_1" || entity.Type == "PlayerSpawn")
                    {
                        playerSpawnData = entity;
                        break;
                    }
                }

                Vector2 spawnPos = scene.PlayerSpawnPosition;
                _player = new Player(spawnPos, _graphicsDevice, _input, _graphicsManager, _particles, _camera);
                _player.SetRespawnPosition(spawnPos);

                if (playerSpawnData != null)
                {
                    _player.ApplyCollisionShape(playerSpawnData.CollisionShape);
                    _player.ApplySpriteData(playerSpawnData.SpriteData);
                }

                factoryContext.Tilemap = scene.Tilemap;
                InitializeWindParticles();
            }
            catch (Exception ex)
            {
                _console.LogError($"Failed to reload level: {ex.Message}");
            }
        }

        public GameRenderer(GraphicsDevice graphicsDevice, int renderWidth, int renderHeight)
        {
            _graphicsDevice = graphicsDevice;
            GameRenderTarget = new RenderTarget2D(graphicsDevice, renderWidth, renderHeight);
            Initialize();
        }

        private RenderTarget2D GetPostProcessingTexture(string fieldName)
        {
            var field = typeof(PostProcessing).GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
            return field?.GetValue(_postProcessing) as RenderTarget2D;
        }

        private void Initialize()
        {
            _console = new DebugConsole();
            _console.Open();

            _pixel = new Texture2D(_graphicsDevice, 1, 1);
            _pixel.SetData(new[] { Color.White });

            CreateGlowTexture();

            _canvasUI = new CanvasUI(_graphicsDevice);
            _debugUI = new DebugUI(_graphicsDevice);
            _gameHUD = new GameHUD(_graphicsDevice);
            _graphicsManager = new GraphicsManager(_graphicsDevice);
            _input = new InputManager();
            _postProcessing = new PostProcessing(_graphicsDevice, 320, 180);
            _camera = new Camera(320, 180, 320, 180);
            _particles = new ParticleSystem(_graphicsDevice, 5000);
            _transitionManager = new TransitionManager(_graphicsDevice);
            _random = new Random();
            _physicsParticleSpawnTimer = 0f;
            _collectedShards = 0;
            _totalShards = 0;
            
            _windParticles = new WindParticle[MAX_WIND_PARTICLES];
            for (int i = 0; i < MAX_WIND_PARTICLES; i++)
            {
                _windParticles[i] = new WindParticle
                {
                    Active = false,
                    Velocity = Vector2.Zero,
                    Rotation = 0f,
                    RotationSpeed = 0f
                };
            }
            _windSpawnTimer = 0f;
            _windSpawnRate = 0.15f;

            _sceneManager = new SceneManager(_graphicsDevice, _graphicsManager);

            _console.Log("Snow Engine initialized");
            _console.LogSuccess($"Bloom enabled: {_postProcessing.BloomEnabled}");
            _console.Log("Controls:");
            _console.Log("  Q/A - Bloom Threshold");
            _console.Log("  W/S - Bloom Intensity");
            _console.Log("  E/D - Canvas Modulate");
            _console.Log("  R   - Reset to defaults");
            _console.Log("  T   - Toggle Camera Mode");

            _mainMenu = new MainMenu(_graphicsDevice, _camera, _particles, _transitionManager, StartGame);

            _debugUI.Enabled = false;
        }

        private void InitializeWindParticles()
        {
            for (int i = 0; i < MAX_WIND_PARTICLES; i++)
            {
                _windParticles[i].Active = false;
            }
        }

        private void SpawnWindParticle()
        {
            for (int i = 0; i < MAX_WIND_PARTICLES; i++)
            {
                if (!_windParticles[i].Active)
                {
                    float depth = (float)_random.NextDouble();
                    float depthScale = 0.3f + depth * 0.7f;
                    
                    _windParticles[i].Position = new Vector2(
                        _camera.Position.X + _camera.ViewportWidth + 20,
                        _camera.Position.Y + (float)_random.NextDouble() * _camera.ViewportHeight
                    );
                    
                    float baseSpeed = -20f - (float)_random.NextDouble() * 30f;
                    float verticalDrift = (float)(_random.NextDouble() * 15f - 2f);
                    
                    _windParticles[i].Velocity = new Vector2(baseSpeed, verticalDrift);
                    _windParticles[i].Size = _random.Next(1, 4);
                    _windParticles[i].Depth = depth;
                    _windParticles[i].Rotation = (float)(_random.NextDouble() * Math.PI * 2);
                    _windParticles[i].RotationSpeed = (float)(_random.NextDouble() * 2f - 1f);
                    
                    int alpha = (int)(40 + depth * 120);
                    _windParticles[i].Color = new Color(190, 195, 215, alpha);
                    _windParticles[i].Active = true;
                    break;
                }
            }
        }

        private void UpdateWindParticles(float deltaTime)
        {
            _windSpawnTimer += deltaTime;
            if (_windSpawnTimer >= _windSpawnRate)
            {
                SpawnWindParticle();
                _windSpawnTimer = 0f;
            }

            for (int i = 0; i < MAX_WIND_PARTICLES; i++)
            {
                if (_windParticles[i].Active)
                {
                    float parallaxFactor = 0.4f + _windParticles[i].Depth * 0.6f;
                    
                    _windParticles[i].Position.X += _windParticles[i].Velocity.X * deltaTime * parallaxFactor;
                    _windParticles[i].Position.Y += _windParticles[i].Velocity.Y * deltaTime * parallaxFactor;
                    
                    _windParticles[i].Rotation += _windParticles[i].RotationSpeed * deltaTime;
                    
                    if (_windParticles[i].Position.X < _camera.Position.X - 20 ||
                        _windParticles[i].Position.Y > _camera.Position.Y + _camera.ViewportHeight + 20)
                    {
                        _windParticles[i].Active = false;
                    }
                }
            }
        }

        private void DrawWindParticles(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetTransformMatrix()
            );

            for (int i = 0; i < MAX_WIND_PARTICLES; i++)
            {
                if (_windParticles[i].Active)
                {
                    int size = (int)_windParticles[i].Size;
                    Vector2 origin = new Vector2(size / 2f, size / 2f);
                    
                    spriteBatch.Draw(
                        _pixel,
                        _windParticles[i].Position,
                        null,
                        _windParticles[i].Color,
                        _windParticles[i].Rotation,
                        origin,
                        new Vector2(size, size),
                        SpriteEffects.None,
                        0f
                    );
                }
            }

            spriteBatch.End();
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

                var scene = _sceneManager.LoadScene(_currentScenePath);
                _console.LogSuccess($"Scene loaded: {scene.Name}");

                if (scene.Tilemap != null)
                {
                    _camera.SetBounds(scene.Tilemap.Width, scene.Tilemap.Height);
                    _console.Log($"Camera bounds set: {scene.Tilemap.Width}x{scene.Tilemap.Height}");
                }

                var sceneData = SceneParser.ParseScene(_currentScenePath);

                if (sceneData.CameraSettings != null)
                {
                    _camera.SetRoomSize(sceneData.CameraSettings.RoomWidth, sceneData.CameraSettings.RoomHeight);

                    if (sceneData.CameraSettings.FollowMode == "follow")
                    {
                        _camera.Mode = CameraMode.Follow;
                        _console.Log("Camera mode: Follow");
                    }
                    else
                    {
                        _camera.Mode = CameraMode.Room;
                        _console.Log($"Camera mode: Room ({sceneData.CameraSettings.RoomWidth}x{sceneData.CameraSettings.RoomHeight})");
                    }
                }
                else
                {
                    _camera.SetRoomSize(320, 180);
                    _camera.Mode = CameraMode.Room;
                    _console.Log("Camera mode: Room (320x180) - Default");
                }

                EntityData playerSpawnData = null;
                foreach (var entity in sceneData.Entities)
                {
                    if (entity.Id == "playerspawn_1" || entity.Type == "PlayerSpawn")
                    {
                        playerSpawnData = entity;
                        break;
                    }
                }

                _player = new Player(scene.PlayerSpawnPosition, _graphicsDevice, _input, _graphicsManager, _particles, _camera);
                _player.SetRespawnPosition(scene.PlayerSpawnPosition);

                if (playerSpawnData != null)
                {
                    _player.ApplyCollisionShape(playerSpawnData.CollisionShape);
                    _player.ApplySpriteData(playerSpawnData.SpriteData);
                }

                _console.Log($"Player spawned at ({scene.PlayerSpawnPosition.X}, {scene.PlayerSpawnPosition.Y})");

                factoryContext.Tilemap = scene.Tilemap;

                _camera.Follow(_player.Position);
                _camera.Update(0f);

                Vector2 playerCenter = new Vector2(160, 90);
                _transitionManager.StartTransition(TransitionType.CircleReveal, 3.5f, playerCenter, 2f);

                _gameStarted = true;
                _debugUI.Enabled = true;
                SpawnInitialFireflies();
                InitializeWindParticles();
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
            _glowTexture = new Texture2D(_graphicsDevice, size, size);
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

        public void Update(GameTime gameTime)
        {
            if (_isPaused && _gameStarted) return;

            _input.Update();
            KeyboardState keyboard = Keyboard.GetState();

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
                return;
            }

            HandleBloomControls(keyboard, gameTime);

            if (!_player.IsDead)
            {
                _player.Update(gameTime);

                if (_sceneManager.CurrentScene != null && _sceneManager.CurrentScene.Tilemap != null)
                {
                    Rectangle playerBounds = _player.GetCollisionBox();
                    
                    if (_sceneManager.CurrentScene.Tilemap.CheckSpikeCollision(playerBounds))
                    {
                        _player.Die();
                        _respawnTimer = RESPAWN_DELAY;
                    }
                }
            }
            else
            {
                _respawnTimer -= deltaTime;
                if (_respawnTimer <= 0f)
                {
                    _player.Respawn();
                }
            }

            if (_sceneManager.CurrentScene != null)
            {
                Rectangle playerBounds = _player.GetCollisionBox();

                foreach (var entity in _sceneManager.CurrentScene.Entities)
                {
                    entity.Update(gameTime);

                    if (entity is Shard shard && !shard.IsFullyCollected)
                    {
                        if (shard.CheckCollision(playerBounds))
                        {
                            shard.Collect(_player.Position + new Vector2(8, 12));
                        }

                        if (shard.IsFullyCollected && shard.IsActive)
                        {
                            shard.IsActive = false;
                            _collectedShards++;
                        }
                    }
                }

                _totalShards = 0;
                foreach (var entity in _sceneManager.CurrentScene.Entities)
                {
                    if (entity is Shard)
                        _totalShards++;
                }
            }

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

            if (_sceneManager.CurrentScene != null && !_player.IsDead)
            {
                CollisionSystem.ResolveCollision(_player, _sceneManager.CurrentScene.Tilemap, deltaTime);
            }

            UpdateWindParticles(deltaTime);

            _camera.Follow(_player.Position);
            _camera.Update(deltaTime);
            _debugUI.Update(gameTime);
            _canvasUI.Update(gameTime);
            _gameHUD.Update(deltaTime);

            _previousKeyboard = keyboard;
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

            if (keyboard.IsKeyDown(Keys.T) && !_previousKeyboard.IsKeyDown(Keys.T))
            {
                if (_camera.Mode == CameraMode.Room)
                {
                    _camera.Mode = CameraMode.Follow;
                    _console.LogSuccess("Camera Mode: FOLLOW");
                }
                else
                {
                    _camera.Mode = CameraMode.Room;
                    _console.LogSuccess("Camera Mode: ROOM");
                }
            }

            if (keyboard.IsKeyDown(Keys.R) && !_previousKeyboard.IsKeyDown(Keys.R))
            {
                _postProcessing.BloomThreshold = 0.5f;
                _postProcessing.BloomIntensity = 0.7f;
                _canvasModulate = new Color(0xc5, 0x00, 0xa8, 0xff);
                _console.LogSuccess("Reset to defaults!");
            }
        }
        
        public void Draw(GameTime gameTime)
        {
            _postProcessing.BeginGameRender();

            _graphicsDevice.Clear(new Color(24, 22, 43));

            if (_gameStarted)
            {
                _graphicsManager.SpriteBatch.Begin(
                    samplerState: SamplerState.PointClamp,
                    transformMatrix: _camera.GetTransformMatrix()
                );

                _sceneManager.Draw(_graphicsManager.SpriteBatch, _camera, gameTime);

                _graphicsManager.SpriteBatch.End();

                DrawWindParticles(_graphicsManager.SpriteBatch);

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

            _graphicsDevice.SetRenderTarget(GameRenderTarget);
            _graphicsDevice.Clear(Color.Black);

            Rectangle destRect = new Rectangle(0, 0, GameRenderTarget.Width, GameRenderTarget.Height);

            var gameTexture = GetPostProcessingTexture("_gameRenderTarget");
            var bloomTexture = GetPostProcessingTexture("_bloomBlurVTarget");

            _graphicsManager.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.AlphaBlend, SamplerState.PointClamp);
            _graphicsManager.SpriteBatch.Draw(gameTexture, destRect, _canvasModulate);
            _graphicsManager.SpriteBatch.End();

            if (_postProcessing.BloomEnabled && bloomTexture != null)
            {
                _graphicsManager.SpriteBatch.Begin(SpriteSortMode.Immediate, BlendState.Additive, SamplerState.PointClamp);
                _graphicsManager.SpriteBatch.Draw(bloomTexture, destRect, Color.White);
                _graphicsManager.SpriteBatch.End();
            }

            _transitionManager.Draw();

            if (!_gameStarted || _mainMenu.CurrentState == MenuState.MainMenu)
            {
                _mainMenu.Draw(_graphicsManager.SpriteBatch);
            }

            if (_gameStarted)
            {
                _gameHUD.Draw(_graphicsManager.SpriteBatch, _player, _collectedShards, _totalShards);

                int entityCount = _sceneManager.CurrentScene?.Entities.Count ?? 0;
                _debugUI.Draw(_graphicsManager.SpriteBatch, _player, _camera, entityCount);
                _debugUI.DrawCollisionBoxes(_graphicsManager.SpriteBatch, _player, _camera, _pixel);
            }

            _canvasUI.Draw(_graphicsManager.SpriteBatch);

            _graphicsDevice.SetRenderTarget(null);
        }

        public void Dispose()
        {
            GameRenderTarget?.Dispose();
            _particles?.Dispose();
            _postProcessing?.Dispose();
            _graphicsManager?.Dispose();
            _transitionManager?.Dispose();
            _pixel?.Dispose();
            _glowTexture?.Dispose();
            _console?.Dispose();
        }

        public void SetCanvasModulateColor(Microsoft.Xna.Framework.Color color)
        {
            _canvasModulate = color;
        }
    }
}
