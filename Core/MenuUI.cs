// Designed & Developed by Revert_b0t
using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace videojuego.Core;

public enum ScreenState
{
    MainMenu,
    Options,
    Playing,
    Paused
}

/// <summary>
/// Gestiona la interfaz de usuario de los menús del simulador (principal, opciones, pausa). Dibuja tarjetas y botones con la paleta visual TMB oficial. Implementa IDisposable para liberar la textura auxiliar.
/// </summary>
public class MenuUI : IDisposable
{
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly InputConfig _inputConfig;

    private readonly Color _tmbRed = new Color(226, 35, 26);
    private readonly Color _bgDark = new Color(15, 17, 22);
    private readonly Color _cardBg = new Color(24, 28, 36);
    private readonly Color _cardHover = new Color(34, 40, 52);
    private readonly Color _borderSubtle = new Color(48, 55, 70);

    private int _menuIndex = 0;
    private readonly string[] _menuOptions = { "INICIAR SERVICIO L1", "CONFIGURACION DE CABINA", "SALIR DEL JUEGO" };

    private int _optionIndex = 0;
    private readonly GameAction[] _configurableActions =
    {
        GameAction.Accelerate,
        GameAction.Brake,
        GameAction.Doors,
        GameAction.ResetATP
    };

    private int _pauseIndex = 0;
    private readonly string[] _pauseOptions = { "REANUDAR SERVICIO", "REINICIAR RUTA (L1)", "VOLVER AL MENU PRINCIPAL" };

    private bool _waitingForKey = false;
    private float _transitionCooldown = 0f;
    private KeyboardState _prevKb;
    private MouseState _prevMouse;

    public MenuUI(GraphicsDevice gd, SpriteFont font, InputConfig inputConfig)
    {
        _font = font;
        _inputConfig = inputConfig;
        _pixel = new Texture2D(gd, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Dispose() => _pixel?.Dispose();

    public void ResetPauseSelection()
    {
        _pauseIndex = 0;
        _transitionCooldown = 0.20f;
    }

    public ScreenState UpdateMainMenu(float dt, float virtualWidth, KeyboardState kb, MouseState mouse, out bool startNewRoute, out bool exitGame)
    {
        startNewRoute = false;
        exitGame = false;

        if (_transitionCooldown > 0f)
        {
            _transitionCooldown -= dt;
            _prevKb = kb;
            _prevMouse = mouse;
            return ScreenState.MainMenu;
        }

        if (IsKeyPressed(kb, Keys.Up) || IsKeyPressed(kb, Keys.W))
            _menuIndex = (_menuIndex - 1 + _menuOptions.Length) % _menuOptions.Length;
        if (IsKeyPressed(kb, Keys.Down) || IsKeyPressed(kb, Keys.S))
            _menuIndex = (_menuIndex + 1) % _menuOptions.Length;

        int startY = 500;
        int btnWidth = 540;
        int btnX = (int)((virtualWidth - btnWidth) / 2f);

        for (int i = 0; i < _menuOptions.Length; i++)
        {
            if (new Rectangle(btnX, startY + i * 82, btnWidth, 62).Contains(mouse.Position))
            {
                _menuIndex = i;
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    return ExecuteMainMenuAction(i, kb, mouse, out startNewRoute, out exitGame);
            }
        }

        if (IsKeyPressed(kb, Keys.Enter) || IsKeyPressed(kb, Keys.Space))
            return ExecuteMainMenuAction(_menuIndex, kb, mouse, out startNewRoute, out exitGame);

        _prevKb = kb;
        _prevMouse = mouse;
        return ScreenState.MainMenu;
    }

    private ScreenState ExecuteMainMenuAction(int index, KeyboardState kb, MouseState mouse, out bool startNewRoute, out bool exitGame)
    {
        startNewRoute = false;
        exitGame = false;
        _prevKb = kb;
        _prevMouse = mouse;
        _transitionCooldown = 0.20f;

        switch (index)
        {
            case 0: startNewRoute = true; return ScreenState.Playing;
            case 1: _optionIndex = 0; _waitingForKey = false; return ScreenState.Options;
            case 2: exitGame = true; return ScreenState.MainMenu;
            default: return ScreenState.MainMenu;
        }
    }

    public ScreenState UpdateOptions(float dt, float virtualWidth, KeyboardState kb, MouseState mouse)
    {
        if (_transitionCooldown > 0f)
        {
            _transitionCooldown -= dt;
            _prevKb = kb;
            _prevMouse = mouse;
            return ScreenState.Options;
        }

        if (_waitingForKey)
        {
            foreach (Keys k in kb.GetPressedKeys())
                if (!_prevKb.IsKeyDown(k))
                {
                    if (k != Keys.Escape) { _inputConfig.Bindings[_configurableActions[_optionIndex]] = k; _inputConfig.Save(); }
                    _waitingForKey = false;
                    _transitionCooldown = 0.15f;
                    break;
                }

            _prevKb = kb;
            _prevMouse = mouse;
            return ScreenState.Options;
        }

        int totalItems = _configurableActions.Length + 2;

        if (IsKeyPressed(kb, Keys.Up) || IsKeyPressed(kb, Keys.W))
            _optionIndex = (_optionIndex - 1 + totalItems) % totalItems;
        if (IsKeyPressed(kb, Keys.Down) || IsKeyPressed(kb, Keys.S))
            _optionIndex = (_optionIndex + 1) % totalItems;

        if (IsKeyPressed(kb, Keys.Escape))
        {
            _prevKb = kb;
            _prevMouse = mouse;
            _transitionCooldown = 0.20f;
            return ScreenState.MainMenu;
        }

        int startY = 330;
        int rowWidth = 780;
        int rowX = (int)((virtualWidth - rowWidth) / 2f);

        for (int i = 0; i < totalItems; i++)
        {
            if (new Rectangle(rowX, startY + i * 74, rowWidth, 56).Contains(mouse.Position))
            {
                _optionIndex = i;
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    return ExecuteOptionItem(i, kb, mouse);
            }
        }

        if (IsKeyPressed(kb, Keys.Enter) || IsKeyPressed(kb, Keys.Space))
            return ExecuteOptionItem(_optionIndex, kb, mouse);

        _prevKb = kb;
        _prevMouse = mouse;
        return ScreenState.Options;
    }

    private ScreenState ExecuteOptionItem(int index, KeyboardState kb, MouseState mouse)
    {
        _prevKb = kb;
        _prevMouse = mouse;
        _transitionCooldown = 0.20f;

        if (index < _configurableActions.Length) { _waitingForKey = true; return ScreenState.Options; }
        if (index == _configurableActions.Length) { _inputConfig.SetDefaults(); _inputConfig.Save(); return ScreenState.Options; }
        return ScreenState.MainMenu;
    }

    public ScreenState UpdatePause(float dt, float virtualWidth, KeyboardState kb, MouseState mouse, out bool restartRoute, out bool backToMenu)
    {
        restartRoute = false;
        backToMenu = false;

        if (_transitionCooldown > 0f)
        {
            _transitionCooldown -= dt;
            _prevKb = kb;
            _prevMouse = mouse;
            return ScreenState.Paused;
        }

        if (IsKeyPressed(kb, Keys.Escape))
        {
            _prevKb = kb;
            _prevMouse = mouse;
            _transitionCooldown = 0.20f;
            return ScreenState.Playing;
        }

        if (IsKeyPressed(kb, Keys.Up) || IsKeyPressed(kb, Keys.W))
            _pauseIndex = (_pauseIndex - 1 + _pauseOptions.Length) % _pauseOptions.Length;
        if (IsKeyPressed(kb, Keys.Down) || IsKeyPressed(kb, Keys.S))
            _pauseIndex = (_pauseIndex + 1) % _pauseOptions.Length;

        int btnWidth = 520;
        int btnX = (int)((virtualWidth - btnWidth) / 2f);
        int startY = 470;

        for (int i = 0; i < _pauseOptions.Length; i++)
        {
            if (new Rectangle(btnX, startY + i * 78, btnWidth, 58).Contains(mouse.Position))
            {
                _pauseIndex = i;
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                    return ExecutePauseAction(i, kb, mouse, out restartRoute, out backToMenu);
            }
        }

        if (IsKeyPressed(kb, Keys.Enter) || IsKeyPressed(kb, Keys.Space))
            return ExecutePauseAction(_pauseIndex, kb, mouse, out restartRoute, out backToMenu);

        _prevKb = kb;
        _prevMouse = mouse;
        return ScreenState.Paused;
    }

    private ScreenState ExecutePauseAction(int index, KeyboardState kb, MouseState mouse, out bool restartRoute, out bool backToMenu)
    {
        restartRoute = false;
        backToMenu = false;
        _prevKb = kb;
        _prevMouse = mouse;
        _transitionCooldown = 0.20f;

        switch (index)
        {
            case 0: return ScreenState.Playing;
            case 1: restartRoute = true; return ScreenState.Playing;
            case 2: backToMenu = true; return ScreenState.MainMenu;
            default: return ScreenState.Paused;
        }
    }

    public void DrawMainMenu(SpriteBatch sb, float virtualWidth)
    {
        int vw = (int)Math.Ceiling(virtualWidth);
        DrawRect(sb, 0, 0, vw, 1080, _bgDark);
        DrawRect(sb, 0, 0, vw, 8, _tmbRed);
        DrawRect(sb, 0, 1072, vw, 8, _tmbRed);

        int cardW = 920;
        int cardH = 260;
        int cardX = (vw - cardW) / 2;
        int cardY = 165;

        DrawRect(sb, cardX + 8, cardY + 10, cardW, cardH, Color.Black * 0.45f);
        DrawModernCard(sb, cardX, cardY, cardW, cardH, _cardBg, _borderSubtle);
        DrawRect(sb, cardX, cardY, 10, cardH, _tmbRed);

        int badgeX = cardX + 52;
        int badgeY = cardY + 65;
        DrawRect(sb, badgeX, badgeY, 130, 130, _tmbRed);
        DrawCenteredText(sb, "L1", new Rectangle(badgeX, badgeY, 130, 130), Color.White, 2.3f);

        int textX = badgeX + 165;
        sb.DrawString(_font, "Metro de Barcelona · L1", new Vector2(textX, cardY + 52), Color.White, 0f, Vector2.Zero, 1.45f, SpriteEffects.None, 0f);
        sb.DrawString(_font, "by Revert_b0t", new Vector2(textX, cardY + 98), new Color(226, 35, 26), 0f, Vector2.Zero, 0.95f, SpriteEffects.None, 0f);
        DrawRect(sb, textX, cardY + 138, 630, 2, _borderSubtle);
        sb.DrawString(_font, "Hospital de Bellvitge  —  Fondo  (30 estaciones)", new Vector2(textX, cardY + 156), Color.LightGray, 0f, Vector2.Zero, 0.95f, SpriteEffects.None, 0f);
        sb.DrawString(_font, "Unidad Serie 6000 · Enclavamiento CCM y Proteccion ATP", new Vector2(textX, cardY + 188), Color.Gray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        int startY = 500;
        int btnWidth = 540;
        int btnX = (vw - btnWidth) / 2;

        for (int i = 0; i < _menuOptions.Length; i++)
        {
            bool selected = i == _menuIndex;
            Rectangle rect = new Rectangle(btnX, startY + i * 82, btnWidth, 62);

            DrawModernCard(sb, rect.X, rect.Y, rect.Width, rect.Height, selected ? _tmbRed : _cardBg, selected ? Color.White : _borderSubtle);
            if (selected) DrawRect(sb, rect.X, rect.Y, 10, rect.Height, Color.White);
            DrawCenteredText(sb, _menuOptions[i], rect, selected ? Color.White : Color.LightGray, 1.0f);
        }

        DrawCenteredText(sb, "Flechas / W-S / Raton para navegar   ·   Enter / Clic para confirmar", new Rectangle(0, 990, vw, 40), Color.Gray, 0.85f);
    }

    public void DrawOptions(SpriteBatch sb, float virtualWidth)
    {
        int vw = (int)Math.Ceiling(virtualWidth);
        DrawRect(sb, 0, 0, vw, 1080, _bgDark);
        DrawRect(sb, 0, 0, vw, 8, _tmbRed);
        DrawRect(sb, 0, 1072, vw, 8, _tmbRed);

        DrawCenteredText(sb, "Configuracion de Controles de Cabina", new Rectangle(0, 165, vw, 50), Color.White, 1.4f);
        DrawCenteredText(sb, "Seleccione una accion y pulse la nueva tecla (ESC para cancelar)", new Rectangle(0, 222, vw, 40), Color.LightGray, 0.9f);

        int startY = 330;
        int rowWidth = 780;
        int rowX = (vw - rowWidth) / 2;

        for (int i = 0; i < _configurableActions.Length; i++)
        {
            GameAction action = _configurableActions[i];
            bool selected = i == _optionIndex;
            Rectangle row = new Rectangle(rowX, startY + i * 74, rowWidth, 56);

            DrawModernCard(sb, row.X, row.Y, row.Width, row.Height, selected ? _cardHover : _cardBg, selected ? _tmbRed : _borderSubtle);
            if (selected) DrawRect(sb, row.X, row.Y, 8, row.Height, _tmbRed);

            string actionLabel = action switch
            {
                GameAction.Accelerate => "Subir Muesca / Acelerar",
                GameAction.Brake => "Bajar Muesca / Frenar",
                GameAction.Doors => "Abrir / Cerrar Puertas",
                GameAction.ResetATP => "Rearmar Freno Emergencia ATP",
                _ => action.ToString()
            };

            string keyText = (_waitingForKey && selected) ? "PULSE UNA TECLA..." : $"{_inputConfig.Bindings[action]}";
            Color keyColor = (_waitingForKey && selected) ? Color.Gold : Color.White;

            sb.DrawString(_font, actionLabel, new Vector2(row.X + 28, row.Y + 14), Color.White, 0f, Vector2.Zero, 0.92f, SpriteEffects.None, 0f);

            Vector2 keySize = _font.MeasureString(keyText) * 0.9f;
            int pillW = Math.Max(90, (int)keySize.X + 32);
            int pillX = row.Right - pillW - 22;
            DrawModernCard(sb, pillX, row.Y + 10, pillW, 36, selected ? _tmbRed : new Color(16, 19, 25), _borderSubtle);
            DrawCenteredText(sb, keyText, new Rectangle(pillX, row.Y + 10, pillW, 36), keyColor, 0.88f);
        }

        int resetIdx = _configurableActions.Length;
        bool resetSel = _optionIndex == resetIdx;
        Rectangle resetRect = new Rectangle(rowX, startY + resetIdx * 74, rowWidth, 56);
        DrawModernCard(sb, resetRect.X, resetRect.Y, resetRect.Width, resetRect.Height, resetSel ? new Color(75, 38, 38) : _cardBg, resetSel ? Color.White : _borderSubtle);
        DrawCenteredText(sb, "RESTAURAR TECLAS POR DEFECTO", resetRect, resetSel ? Color.White : Color.LightGray, 0.92f);

        int backIdx = _configurableActions.Length + 1;
        bool backSel = _optionIndex == backIdx;
        Rectangle backRect = new Rectangle(rowX, startY + backIdx * 74, rowWidth, 56);
        DrawModernCard(sb, backRect.X, backRect.Y, backRect.Width, backRect.Height, backSel ? _tmbRed : _cardBg, backSel ? Color.White : _borderSubtle);
        DrawCenteredText(sb, "VOLVER AL MENU PRINCIPAL", backRect, Color.White, 0.95f);
    }

    public void DrawPause(SpriteBatch sb, float virtualWidth)
    {
        int vw = (int)Math.Ceiling(virtualWidth);
        DrawRect(sb, 0, 0, vw, 1080, Color.Black * 0.72f);

        int panelW = 640;
        int panelH = 440;
        int panelX = (vw - panelW) / 2;
        int panelY = 310;

        DrawModernCard(sb, panelX, panelY, panelW, panelH, _cardBg, _borderSubtle);
        DrawRect(sb, panelX, panelY, panelW, 8, _tmbRed);
        DrawCenteredText(sb, "Servicio en Pausa", new Rectangle(panelX, panelY + 35, panelW, 45), Color.White, 1.35f);
        DrawCenteredText(sb, "Pulse [ESC] para reanudar la marcha", new Rectangle(panelX, panelY + 85, panelW, 30), Color.LightGray, 0.88f);

        int btnWidth = 520;
        int btnX = (vw - btnWidth) / 2;
        int startY = 470;

        for (int i = 0; i < _pauseOptions.Length; i++)
        {
            bool selected = i == _pauseIndex;
            Rectangle rect = new Rectangle(btnX, startY + i * 78, btnWidth, 58);

            DrawModernCard(sb, rect.X, rect.Y, rect.Width, rect.Height, selected ? _tmbRed : _cardHover, selected ? Color.White : _borderSubtle);
            if (selected) DrawRect(sb, rect.X, rect.Y, 10, rect.Height, Color.White);
            DrawCenteredText(sb, _pauseOptions[i], rect, selected ? Color.White : Color.LightGray, 0.95f);
        }
    }

    private void DrawCenteredText(SpriteBatch sb, string text, Rectangle bounds, Color color, float scale)
    {
        Vector2 size = _font.MeasureString(text) * scale;
        Vector2 pos = new Vector2(bounds.X + (bounds.Width - size.X) / 2f, bounds.Y + (bounds.Height - size.Y) / 2f);
        sb.DrawString(_font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawModernCard(SpriteBatch sb, int x, int y, int w, int h, Color fill, Color border)
    {
        DrawRect(sb, x, y, w, h, border);
        DrawRect(sb, x + 1, y + 1, w - 2, h - 2, fill);
    }

    private bool IsKeyPressed(KeyboardState current, Keys key) => current.IsKeyDown(key) && !_prevKb.IsKeyDown(key);

    private void DrawRect(SpriteBatch sb, int x, int y, int width, int height, Color color) => sb.Draw(_pixel, new Rectangle(x, y, width, height), color);
}
