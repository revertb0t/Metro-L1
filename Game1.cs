using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using videojuego.Core;
using videojuego.Entities;
using videojuego.Systems;
using videojuego.World;

namespace videojuego;

public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;

    private ScreenState _currentScreen = ScreenState.MainMenu;
    private InputConfig _inputConfig;
    private MenuUI _menuUI;

    private MetroTrain _train;
    private SignalingAI _signalingAI;
    private ServiceManager _serviceManager;
    private TrainAudio _audio;
    private MetroRenderer _renderer;

    private List<Station> _stations;
    private List<Signal> _signals;
    private KeyboardState _prevKeyboard;

    public Game1()
    {
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 1920;
        _graphics.PreferredBackBufferHeight = 1080;
        _graphics.SynchronizeWithVerticalRetrace = true;
    }

    protected override void Initialize()
    {
        _inputConfig = new InputConfig();
        StartNewSession();
        base.Initialize();
    }

    private void StartNewSession()
    {
        _train = new MetroTrain();
        _signalingAI = new SignalingAI();
        _serviceManager = new ServiceManager();

        _stations = new List<Station>
        {
            new Station("HOSPITAL DE BELLVITGE", 50f, "TERMINAL L1", 95f, 1),
            new Station("BELLVITGE", 680f, "", 95f, 1),
            new Station("AV. CARRILET", 1360f, "FGC L8", 95f, 2),
            new Station("RAMBLA JUST OLIVERAS", 2040f, "RODALIES", 95f, 1),
            new Station("CAN SERRA", 2700f, "", 95f, 1)
        };

        _signals = new List<Signal>
        {
            new Signal(155f, SignalAspect.Green, "SALIDA 75", 75),
            new Signal(450f, SignalAspect.Green, "CANTON 02", 75),
            new Signal(660f, SignalAspect.Green, "VIA 1", 65),
            new Signal(890f, SignalAspect.Green, "RECTA 80", 80),
            new Signal(1160f, SignalAspect.Green, "AGUJAS 45", 45),
            new Signal(1340f, SignalAspect.Green, "VIA 2", 45),
            new Signal(1580f, SignalAspect.Green, "SALIDA 75", 75),
            new Signal(1860f, SignalAspect.Green, "CANTON 06", 70),
            new Signal(2020f, SignalAspect.Green, "VIA 1", 65)
        };
    }

    protected override void LoadContent()
    {
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("MainFont");

        _menuUI = new MenuUI(GraphicsDevice, _font, _inputConfig);
        _audio = new TrainAudio();
        _renderer = new MetroRenderer(GraphicsDevice, _font);
    }

    protected override void Update(GameTime gameTime)
    {
        var kb = Keyboard.GetState();
        var mouse = Mouse.GetState();
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        switch (_currentScreen)
        {
            case ScreenState.MainMenu:
                _audio.Update(dt, 0f, 0, false, false);
                _currentScreen = _menuUI.UpdateMainMenu(dt, kb, mouse, out bool startNew, out bool exitGame);
                if (startNew)
                    StartNewSession();
                if (exitGame)
                    Exit();
                break;

            case ScreenState.Options:
                _currentScreen = _menuUI.UpdateOptions(dt, kb, mouse);
                break;

            case ScreenState.Playing:
                if (IsKeyPressed(kb, Keys.Escape))
                {
                    _currentScreen = ScreenState.MainMenu;
                    break;
                }

                UpdateGameplay(dt, kb);
                break;
        }

        _prevKeyboard = kb;
        base.Update(gameTime);
    }

    private void UpdateGameplay(float dt, KeyboardState kb)
    {
        Keys accelKey = _inputConfig.Bindings[GameAction.Accelerate];
        Keys brakeKey = _inputConfig.Bindings[GameAction.Brake];
        Keys doorsKey = _inputConfig.Bindings[GameAction.Doors];
        Keys resetKey = _inputConfig.Bindings[GameAction.ResetATP];

        if (IsKeyPressed(kb, accelKey))
        {
            int prevNotch = _train.Notch;
            _train.IncreaseNotch();
            if (_train.Notch != prevNotch) _audio.PlayNotchClick();
        }

        if (IsKeyPressed(kb, brakeKey))
        {
            int prevNotch = _train.Notch;
            _train.DecreaseNotch();
            if (_train.Notch != prevNotch) _audio.PlayNotchClick();
        }

        if (IsKeyPressed(kb, doorsKey))
        {
            bool wereOpen = _train.DoorsOpen;
            _train.ToggleDoors();
            if (wereOpen && !_train.DoorsOpen)
                _audio.PlayDoorChime();
        }

        if (IsKeyPressed(kb, resetKey))
            _train.ResetEmergencyBrake();

        _train.Update(dt);
        _signalingAI.Update(dt, _train, _signals, _stations);
        _serviceManager.Update(dt, _train, _stations, _signals);
        _renderer.Update(dt, _train);

        bool isOverspeed = _train.VelocityKmh > _serviceManager.CurrentSpeedLimitKmh;
        _audio.Update(dt, _train.VelocityKmh, _train.Notch, isOverspeed, _train.EmergencyBrakeTriggered);
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_prevKeyboard.IsKeyDown(key);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(14, 16, 21));
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend);

        switch (_currentScreen)
        {
            case ScreenState.MainMenu:
                _menuUI.DrawMainMenu(_spriteBatch);
                break;

            case ScreenState.Options:
                _menuUI.DrawOptions(_spriteBatch);
                break;

            case ScreenState.Playing:
                _renderer.DrawWorld(_spriteBatch, _train, _serviceManager, _stations, _signals, _inputConfig);
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        _audio?.Dispose();
        base.UnloadContent();
    }
}