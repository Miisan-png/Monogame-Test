using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using Snow.Game;

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
        private Player _player;

        private Color[] _modulateColors = new Color[]
        {
            Color.White,
            new Color(255, 200, 200),
            new Color(200, 255, 200),
            new Color(200, 200, 255),
            new Color(255, 180, 255),
            new Color(180, 255, 255),
        };
        private int _currentModulateIndex = 0;

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
            _particles = new ParticleSystem(GraphicsDevice, 2000);

            _player = new Player(new Vector2(160, 50), GraphicsDevice, _input, _graphicsManager, _particles);
        }

        protected override void Update(GameTime gameTime)
        {
            _input.Update();

            if (Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            if (_input.IsKeyPressed(Keys.R))
            {
                _currentModulateIndex = (_currentModulateIndex + 1) % _modulateColors.Length;
                _postProcessing.CanvasModulate = _modulateColors[_currentModulateIndex];
            }

            _player.Update(gameTime);
            _camera.Follow(_player.Position);
            
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            _particles.Update(deltaTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _postProcessing.BeginGameRender();

            _graphicsManager.SpriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetTransformMatrix()
            );
            _player.Draw(_graphicsManager.SpriteBatch, gameTime);
            _graphicsManager.SpriteBatch.End();

            _particles.Draw(_graphicsManager.SpriteBatch, _camera.GetTransformMatrix());

            _postProcessing.EndGameRender();

            _postProcessing.ApplyPostProcessing();
            _postProcessing.DrawFinal();

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _particles?.Dispose();
            _postProcessing?.Dispose();
            _graphicsManager?.Dispose();
            base.UnloadContent();
        }
    }
}
