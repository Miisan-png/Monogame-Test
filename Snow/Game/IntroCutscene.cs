using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Snow.Engine;
using System;
using System.Collections.Generic;

namespace Snow.Game
{
    public enum IntroState
    {
        Dialogue1,
        Dialogue2,
        Dialogue3,
        Dialogue4,
        Dialogue5,
        FadeOutDialogue,
        FadeInHouse,
        ShowHouse,
        SlideUpHouse,
        ShowFinalText,
        SlideCamera,
        Complete
    }

    public class IntroCutscene
    {
        private GraphicsDevice _graphicsDevice;
        private DialogueRenderer _dialogueRenderer;
        private Texture2D _houseIcon;
        private IntroState _currentState;
        private float _stateTimer;
        private Vector2 _housePosition;
        private Vector2Tween _housePositionTween;
        private Tween _houseAlphaTween;
        private float _houseAlpha;
        private KeyboardState _previousKeyboard;
        private Action _onComplete;
        private Tween _finalTextAlphaTween;
        private float _finalTextAlpha;
        private BitmapFont _font;
        private Vector2 _cameraOffset;
        private Vector2Tween _cameraOffsetTween;

        private List<string> _dialogues = new List<string>
        {
            "....",
            "…I don't remember how it started.",
            "One day… I was just here.",
            "Everyone else has somewhere to go.",
            "…still… I wonder.",
            "aw.. my orange fell..."
        };

        private int _currentDialogueIndex = 0;

        public bool IsComplete => _currentState == IntroState.Complete;
        public Vector2 CameraOffset => _cameraOffset;

        public IntroCutscene(GraphicsDevice device, Action onComplete)
        {
            _graphicsDevice = device;
            _dialogueRenderer = new DialogueRenderer(device, 0.04f);
            _font = new BitmapFont(device, "assets/Font/default/default_font_data.txt");
            _onComplete = onComplete;
            _currentState = IntroState.Dialogue1;
            _stateTimer = 0f;
            _houseAlpha = 0f;
            _finalTextAlpha = 0f;
            _cameraOffset = new Vector2(0, -720);
            
            LoadHouseIcon();
            
            _housePosition = new Vector2(640, -240);
            
            StartDialogue(0);
        }

        private void LoadHouseIcon()
        {
            try
            {
                using (var stream = System.IO.File.OpenRead("assets/intro/house_icon.png"))
                {
                    _houseIcon = Texture2D.FromStream(_graphicsDevice, stream);
                }
            }
            catch
            {
                _houseIcon = new Texture2D(_graphicsDevice, 16, 16);
                Color[] data = new Color[256];
                for (int i = 0; i < data.Length; i++)
                    data[i] = Color.White;
                _houseIcon.SetData(data);
            }
        }

        private void StartDialogue(int index)
        {
            if (index < _dialogues.Count)
            {
                _dialogueRenderer.SetText(_dialogues[index]);
                _currentDialogueIndex = index;
            }
        }

        public void Update(float deltaTime)
        {
            KeyboardState keyboard = Keyboard.GetState();

            _stateTimer += deltaTime;

            switch (_currentState)
            {
                case IntroState.Dialogue1:
                    _dialogueRenderer.Update(deltaTime);
                    if (!_dialogueRenderer.IsTyping && _stateTimer > 2.5f)
                    {
                        _currentState = IntroState.Dialogue2;
                        _stateTimer = 0f;
                        StartDialogue(1);
                    }
                    break;

                case IntroState.Dialogue2:
                    _dialogueRenderer.Update(deltaTime);
                    if (!_dialogueRenderer.IsTyping && _stateTimer > 3.0f)
                    {
                        _currentState = IntroState.Dialogue3;
                        _stateTimer = 0f;
                        StartDialogue(2);
                    }
                    break;

                case IntroState.Dialogue3:
                    _dialogueRenderer.Update(deltaTime);
                    if (!_dialogueRenderer.IsTyping && _stateTimer > 3.0f)
                    {
                        _currentState = IntroState.Dialogue4;
                        _stateTimer = 0f;
                        StartDialogue(3);
                    }
                    break;

                case IntroState.Dialogue4:
                    _dialogueRenderer.Update(deltaTime);
                    if (!_dialogueRenderer.IsTyping && _stateTimer > 2.5f)
                    {
                        StartDialogue(4);
                        _currentState = IntroState.FadeOutDialogue;
                        _stateTimer = 0f;
                    }
                    break;

                case IntroState.Dialogue5:
                    _dialogueRenderer.Update(deltaTime);
                    if (!_dialogueRenderer.IsTyping && _stateTimer > 2.5f)
                    {
                        StartDialogue(5);
                        _currentState = IntroState.FadeOutDialogue;
                        _stateTimer = 0f;
                    }
                    break;

                case IntroState.FadeOutDialogue:
                    _dialogueRenderer.Update(deltaTime);
                    if (!_dialogueRenderer.IsTyping && _stateTimer > 2.5f)
                    {
                        _dialogueRenderer.FadeOut(1.5f);
                        _currentState = IntroState.FadeInHouse;
                        _stateTimer = 0f;
                    }
                    break;

                case IntroState.FadeInHouse:
                    _dialogueRenderer.Update(deltaTime);
                    if (_dialogueRenderer.IsComplete)
                    {
                        _houseAlphaTween = new Tween(0f, 1f, 2.0f, EaseType.EaseInOutQuad);
                        _currentState = IntroState.ShowHouse;
                        _stateTimer = 0f;
                    }
                    break;

                case IntroState.ShowHouse:
                    if (_houseAlphaTween != null)
                    {
                        _houseAlphaTween.Update(deltaTime);
                        _houseAlpha = _houseAlphaTween.GetValue();
                        
                        if (_houseAlphaTween.IsComplete)
                        {
                            Vector2 startPos = new Vector2(640, -240);
                            Vector2 endPos = new Vector2(640, -440);
                            _housePositionTween = new Vector2Tween(startPos, endPos, 2.0f, EaseType.EaseOutQuad);
                            _finalTextAlphaTween = new Tween(0f, 1f, 2.0f, EaseType.Linear);
                            _currentState = IntroState.SlideUpHouse;
                            _stateTimer = 0f;
                        }
                    }
                    break;

                case IntroState.SlideUpHouse:
                    if (_housePositionTween != null)
                    {
                        _housePositionTween.Update(deltaTime);
                        _housePosition = _housePositionTween.GetValue();
                    }
                    
                    if (_finalTextAlphaTween != null)
                    {
                        _finalTextAlphaTween.Update(deltaTime);
                        _finalTextAlpha = _finalTextAlphaTween.GetValue();
                    }
                    
                    if (_housePositionTween != null && _housePositionTween.IsComplete)
                    {
                        _currentState = IntroState.ShowFinalText;
                        _stateTimer = 0f;
                    }
                    break;

                case IntroState.ShowFinalText:
                    if (_stateTimer > 2.5f)
                    {
                        _cameraOffsetTween = new Vector2Tween(new Vector2(0, -720), Vector2.Zero, 3.0f, EaseType.EaseInOutCubic);
                        _currentState = IntroState.SlideCamera;
                        _stateTimer = 0f;
                    }
                    break;

                case IntroState.SlideCamera:
                    if (_cameraOffsetTween != null)
                    {
                        _cameraOffsetTween.Update(deltaTime);
                        _cameraOffset = _cameraOffsetTween.GetValue();
                        
                        if (_cameraOffsetTween.IsComplete)
                        {
                            _currentState = IntroState.Complete;
                            _onComplete?.Invoke();
                        }
                    }
                    break;

                case IntroState.Complete:
                    break;
            }

            _previousKeyboard = keyboard;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            if (_currentState >= IntroState.Dialogue1 && _currentState <= IntroState.FadeInHouse)
            {
                Vector2 dialoguePos = new Vector2(640, -360) - _cameraOffset;
                _dialogueRenderer.Draw(spriteBatch, dialoguePos, 2.0f);
            }

            if (_currentState >= IntroState.ShowHouse)
            {
                Vector2 houseDrawPos = _housePosition - _cameraOffset;
                Vector2 houseOrigin = new Vector2(_houseIcon.Width / 2f, _houseIcon.Height / 2f);
                Color houseColor = Color.White * _houseAlpha;
                spriteBatch.Draw(_houseIcon, houseDrawPos, null, houseColor, 0f, houseOrigin, 4.0f, SpriteEffects.None, 0f);
            }

            if (_currentState >= IntroState.SlideUpHouse)
            {
                Vector2 finalTextPos = new Vector2(640, -160) - _cameraOffset;
                Color textColor = Color.White * _finalTextAlpha;
                _font.DrawString(spriteBatch, "find your way home..", finalTextPos, textColor, 2.5f, TextAlignment.Center);
            }

            spriteBatch.End();
        }
    }
}
