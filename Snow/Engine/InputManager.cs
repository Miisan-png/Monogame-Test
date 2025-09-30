using Microsoft.Xna.Framework.Input;

namespace Snow.Engine
{
    public class InputManager
    {
        private KeyboardState _currentKeyboard;
        private KeyboardState _previousKeyboard;

        public void Update()
        {
            _previousKeyboard = _currentKeyboard;
            _currentKeyboard = Keyboard.GetState();
        }

        public bool IsKeyDown(Keys key)
        {
            return _currentKeyboard.IsKeyDown(key);
        }

        public bool IsKeyPressed(Keys key)
        {
            return _currentKeyboard.IsKeyDown(key) && !_previousKeyboard.IsKeyDown(key);
        }

        public bool IsKeyReleased(Keys key)
        {
            return !_currentKeyboard.IsKeyDown(key) && _previousKeyboard.IsKeyDown(key);
        }

        public float GetAxisHorizontal()
        {
            float axis = 0f;
            if (IsKeyDown(Keys.A) || IsKeyDown(Keys.Left)) axis -= 1f;
            if (IsKeyDown(Keys.D) || IsKeyDown(Keys.Right)) axis += 1f;
            return axis;
        }

        public float GetAxisVertical()
        {
            float axis = 0f;
            if (IsKeyDown(Keys.W) || IsKeyDown(Keys.Up)) axis -= 1f;
            if (IsKeyDown(Keys.S) || IsKeyDown(Keys.Down)) axis += 1f;
            return axis;
        }
    }
}
