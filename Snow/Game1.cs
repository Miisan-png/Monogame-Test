using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Editor;
using System;

namespace Snow
{
    public class Game1 : Microsoft.Xna.Framework.Game
    {
        private const bool FULLSCREEN_ENABLED = false;

        private GraphicsDeviceManager _graphics;
        private MonoGame.ImGuiNet.ImGuiRenderer _imGuiRenderer;
        private GameRenderer _gameRenderer;
        private EditorMain _editor;
        private IntPtr _gameTexturePtr;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
        }

        protected override void Initialize()
        {
            _graphics.IsFullScreen = FULLSCREEN_ENABLED;
            _graphics.PreferredBackBufferWidth = 1920;
            _graphics.PreferredBackBufferHeight = 1080;
            _graphics.ApplyChanges();

            Window.AllowUserResizing = true;

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _imGuiRenderer = new MonoGame.ImGuiNet.ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();

            var io = ImGuiNET.ImGui.GetIO();
            io.ConfigFlags |= ImGuiNET.ImGuiConfigFlags.DockingEnable;

            _gameRenderer = new GameRenderer(GraphicsDevice, 1280, 720);
            _editor = new EditorMain(_gameRenderer, GraphicsDevice, _imGuiRenderer);

            _gameTexturePtr = _imGuiRenderer.BindTexture(_gameRenderer.GameRenderTarget);
            _editor.SetGameTexturePtr(_gameTexturePtr);
        }

        protected override void Update(GameTime gameTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            if (keyboard.IsKeyDown(Keys.Escape))
                Exit();

            _gameRenderer.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            _gameRenderer.Draw(gameTime);

            GraphicsDevice.Clear(new Color(45, 45, 48));

            _imGuiRenderer.BeginLayout(gameTime);

            _editor.Render();

            _imGuiRenderer.EndLayout();

            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _gameRenderer?.Dispose();
            base.UnloadContent();
        }
    }
}
