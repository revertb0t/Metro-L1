// Designed & Developed by Revert_b0t
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using videojuego.Core;
using videojuego.Entities;
using videojuego.Systems;
using videojuego.World;

namespace videojuego;

/// <summary>
/// Clase principal del simulador Metro L1. Orquesta el bucle de juego (Update/Draw a 60 FPS), gestiona los estados de pantalla (menú, opciones, juego, pausa) y coordina los subsistemas de audio, renderizado, señalización y servicio.
/// </summary>
public class Game1 : Game
{
    private GraphicsDeviceManager _graphics;
    private SpriteBatch _spriteBatch;
    private SpriteFont _font;

    private ScreenState _currentScreen = ScreenState.MainMenu;
    private ViewportManager _viewport;
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
        // [BLOQUE: Configuración inicial de ventana y gráficos]
        _graphics = new GraphicsDeviceManager(this);
        Content.RootDirectory = "Content";
        IsMouseVisible = true;

        _graphics.PreferredBackBufferWidth = 1280;
        _graphics.PreferredBackBufferHeight = 720;
        _graphics.SynchronizeWithVerticalRetrace = true;
    }

    protected override void Initialize()
    {
        // [BLOQUE: Inicialización de título, viewport y sesión]
        Window.Title = "Metro L1 by Revert_b0t";
        Window.AllowUserResizing = true;

        _viewport = new ViewportManager();
        _inputConfig = new InputConfig();
        StartNewSession();
        base.Initialize();
    }

    private void StartNewSession()
    {
        // [BLOQUE: Reinicio de estado del simulador]
        _audio?.StopAnnouncement();
        _train = new MetroTrain();
        _signalingAI = new SignalingAI();
        _serviceManager = new ServiceManager();

        var routeData = Line1Builder.BuildFullLine1();
        _stations = routeData.Stations;
        _signals = routeData.Signals;
    }

    protected override void LoadContent()
    {
        // [BLOQUE: Carga de recursos gráficos y de audio]
        _spriteBatch = new SpriteBatch(GraphicsDevice);
        _font = Content.Load<SpriteFont>("MainFont");

        _menuUI = new MenuUI(GraphicsDevice, _font, _inputConfig);
        _audio = new TrainAudio();
        _renderer = new MetroRenderer(GraphicsDevice, _font);
    }

    protected override void Update(GameTime gameTime)
    {
        // [BLOQUE: Actualización de viewport e input]
        _viewport.Update(GraphicsDevice);
        var kb = Keyboard.GetState();
        var mouse = _viewport.GetVirtualMouseState(Mouse.GetState());
        float dt = (float)gameTime.ElapsedGameTime.TotalSeconds;

        // [BLOQUE: Gestión de pantalla completa con F11]
        if (IsKeyPressed(kb, Keys.F11))
        {
            _graphics.IsFullScreen = !_graphics.IsFullScreen;
            if (_graphics.IsFullScreen)
            {
                _graphics.PreferredBackBufferWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
                _graphics.PreferredBackBufferHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;
            }
            else
            {
                _graphics.PreferredBackBufferWidth = 1280;
                _graphics.PreferredBackBufferHeight = 720;
            }
            _graphics.ApplyChanges();
        }

        float vw = _viewport.VirtualWidth;

        // [BLOQUE: Máquina de estados de pantalla]
        switch (_currentScreen)
        {
            case ScreenState.MainMenu:
                _audio.Update(dt, 0f, 0, false, false);
                _currentScreen = _menuUI.UpdateMainMenu(dt, vw, kb, mouse, out bool startNew, out bool exitGame);
                if (startNew)
                    StartNewSession();
                if (exitGame)
                    Exit();
                break;

            case ScreenState.Options:
                _currentScreen = _menuUI.UpdateOptions(dt, vw, kb, mouse);
                break;

            case ScreenState.Playing:
                if (IsKeyPressed(kb, Keys.Escape))
                {
                    _audio.PauseAll();
                    _menuUI.ResetPauseSelection();
                    _currentScreen = ScreenState.Paused;
                    break;
                }

                UpdateGameplay(dt, kb);
                break;

            case ScreenState.Paused:
                _currentScreen = _menuUI.UpdatePause(dt, vw, kb, mouse, out bool restartRoute, out bool backToMenu);
                if (_currentScreen == ScreenState.Playing)
                {
                    if (restartRoute)
                        StartNewSession();
                    else
                        _audio.ResumeAll();
                }
                else if (backToMenu)
                {
                    _audio.StopAnnouncement();
                    _audio.ResumeAll();
                }
                break;
        }

        _prevKeyboard = kb;
        base.Update(gameTime);
    }

    private void UpdateGameplay(float dt, KeyboardState kb)
    {
        // [BLOQUE: Lectura de controles configurados]
        Keys accelKey = _inputConfig.Bindings[GameAction.Accelerate];
        Keys brakeKey = _inputConfig.Bindings[GameAction.Brake];
        Keys doorsKey = _inputConfig.Bindings[GameAction.Doors];
        Keys resetKey = _inputConfig.Bindings[GameAction.ResetATP];

        // [BLOQUE: Gestión de entrada de usuario]
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

        // [BLOQUE: Actualización de sistemas de simulación]
        _train.Update(dt);
        _signalingAI.Update(dt, _train, _signals, _stations);
        _serviceManager.Update(dt, _train, _stations, _signals);
        _renderer.Update(dt, _train);

        // [BLOQUE: Disparo de megafonía entre estaciones]
        for (int i = 1; i < _stations.Count; i++)
        {
            var prevSt = _stations[i - 1];
            var nextSt = _stations[i];

            if (!nextSt.AnnouncementPlayed)
            {
                float midPoint = (prevSt.PositionMeters + nextSt.PositionMeters) * 0.5f;
                if (_train.PositionMeters >= midPoint && _train.PositionMeters < nextSt.PositionMeters)
                {
                    nextSt.AnnouncementPlayed = true;
                    _audio.PlayStationAnnouncement(nextSt.AudioFile);
                    break;
                }
            }
        }

        // [BLOQUE: Actualización del audio según velocidad]
        bool isOverspeed = _train.VelocityKmh > _serviceManager.CurrentSpeedLimitKmh;
        _audio.Update(dt, _train.VelocityKmh, _train.Notch, isOverspeed, _train.EmergencyBrakeTriggered);
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_prevKeyboard.IsKeyDown(key);
    }

    protected override void Draw(GameTime gameTime)
    {
        GraphicsDevice.Clear(new Color(15, 17, 22));
        _spriteBatch.Begin(blendState: BlendState.AlphaBlend, transformMatrix: _viewport.TransformMatrix);

        float vw = _viewport.VirtualWidth;

        // [BLOQUE: Renderizado según estado de pantalla]
        switch (_currentScreen)
        {
            case ScreenState.MainMenu:
                _menuUI.DrawMainMenu(_spriteBatch, vw);
                break;

            case ScreenState.Options:
                _menuUI.DrawOptions(_spriteBatch, vw);
                break;

            case ScreenState.Playing:
                _renderer.DrawWorld(_spriteBatch, vw, _train, _serviceManager, _stations, _signals, _inputConfig);
                break;

            case ScreenState.Paused:
                _renderer.DrawWorld(_spriteBatch, vw, _train, _serviceManager, _stations, _signals, _inputConfig);
                _menuUI.DrawPause(_spriteBatch, vw);
                break;
        }

        _spriteBatch.End();
        base.Draw(gameTime);
    }

    protected override void UnloadContent()
    {
        // [BLOQUE: Liberación de recursos de audio]
        _audio?.Dispose();
        base.UnloadContent();
    }
}
