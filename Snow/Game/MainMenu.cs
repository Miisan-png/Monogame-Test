using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Snow.Engine;
using System;

namespace Snow.Game
{
    public enum MenuState
    {
        MainMenu,
        Settings,
        InGame
    }

    public class MainMenu
    {
        private UIManager _uiManager;
        private MenuState _currentState;
        private ParticleSystem _particles;
        private Camera _camera;
        private TransitionManager _transitionManager;
        private float _buttonAnimTimer;
        private bool _buttonsVisible;
        private Action _onStartGame;
        private Random _random;
        private float _ambientParticleTimer;

        public MenuState CurrentState => _currentState;

        public MainMenu(GraphicsDevice device, Camera camera, ParticleSystem particles, TransitionManager transitionManager, Action onStartGame)
        {
            _camera = camera;
            _particles = particles;
            _transitionManager = transitionManager;
            _onStartGame = onStartGame;
            _uiManager = new UIManager(device, camera, particles);
            _currentState = MenuState.MainMenu;
            _buttonAnimTimer = 0f;
            _buttonsVisible = false;
            _random = new Random();
            _ambientParticleTimer = 0f;

            ShowMainMenu();
        }

        private void ShowMainMenu()
        {
            _uiManager.ClearButtons();
            _buttonsVisible = true;
            _buttonAnimTimer = 0f;

            Vector2 center = new Vector2(160, 90);
            float spacing = 35f;

            _uiManager.AddButton("START", center + new Vector2(0, -spacing), OnStartGame);
            _uiManager.AddButton("SETTINGS", center + new Vector2(0, 0), OnSettings);
            _uiManager.AddButton("QUIT", center + new Vector2(0, spacing), OnQuit);
        }

        private void OnStartGame()
        {
            _buttonsVisible = false;
            _transitionManager.StartTransition(TransitionType.CircleClose, 2.0f, new Vector2(160, 90), 0.0f, () =>
            {
                _currentState = MenuState.InGame;
                _onStartGame?.Invoke();
            });
        }

        private void OnSettings()
        {
            _currentState = MenuState.Settings;
        }

        private void OnQuit()
        {
            Environment.Exit(0);
        }

        public void Update(float deltaTime)
        {
            _ambientParticleTimer += deltaTime;
            if (_ambientParticleTimer > 0.1f)
            {
                float x = (float)_random.NextDouble() * 320f;
                float y = (float)_random.NextDouble() * 180f;
                Vector2 pos = new Vector2(x, y);
                Vector2 vel = new Vector2(
                    (float)(_random.NextDouble() - 0.5) * 15f,
                    (float)(_random.NextDouble() - 0.5) * 15f
                );
                
                _particles.Emit(
                    pos,
                    vel,
                    new Color(100, 100, 150, 80),
                    2.0f,
                    0.8f
                );
                _ambientParticleTimer = 0f;
            }

            if (_buttonsVisible)
            {
                _uiManager.Update(deltaTime);
            }

            _buttonAnimTimer += deltaTime;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            if (_currentState != MenuState.MainMenu)
                return;

            if (_buttonsVisible)
            {
                _uiManager.Draw(spriteBatch);
            }
        }
    }
}
