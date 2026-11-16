using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace videojuego.Core;

public enum ScreenState
{
    MainMenu,
    Options,
    Playing
}

public class MenuUI
{
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly InputConfig _inputConfig;

    private readonly Color _tmbRed = new Color(218, 41, 28);
    private readonly Color _tmbDarkRed = new Color(145, 22, 14);
    private readonly Color _bgDark = new Color(12, 14, 19);
    private readonly Color _panelBg = new Color(22, 26, 34);
    private readonly Color _panelBorder = new Color(46, 52, 66);

    private int _menuIndex = 0;
    private readonly string[] _menuOptions = { "INICIAR SERVICIO L1", "CONFIGURACION DE CABINA", "SALIR AL ESCRITORIO" };

    private int _optionIndex = 0;
    private readonly GameAction[] _configurableActions =
    {
        GameAction.Accelerate,
        GameAction.Brake,
        GameAction.Doors,
        GameAction.ResetATP
    };

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

    public ScreenState UpdateMainMenu(float dt, KeyboardState kb, MouseState mouse, out bool startNewRoute, out bool exitGame)
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

        int startY = 510;
        int btnWidth = 540;
        int btnX = (1920 - btnWidth) / 2;

        for (int i = 0; i < _menuOptions.Length; i++)
        {
            Rectangle btnRect = new Rectangle(btnX, startY + i * 84, btnWidth, 64);
            if (btnRect.Contains(mouse.Position))
            {
                _menuIndex = i;
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                {
                    return ExecuteMainMenuAction(i, kb, mouse, out startNewRoute, out exitGame);
                }
            }
        }

        if (IsKeyPressed(kb, Keys.Enter) || IsKeyPressed(kb, Keys.Space))
        {
            return ExecuteMainMenuAction(_menuIndex, kb, mouse, out startNewRoute, out exitGame);
        }

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
            case 0:
                startNewRoute = true;
                return ScreenState.Playing;
            case 1:
                _optionIndex = 0;
                _waitingForKey = false;
                return ScreenState.Options;
            case 2:
                exitGame = true;
                return ScreenState.MainMenu;
            default:
                return ScreenState.MainMenu;
        }
    }

    public ScreenState UpdateOptions(float dt, KeyboardState kb, MouseState mouse)
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
            {
                if (!_prevKb.IsKeyDown(k))
                {
                    if (k != Keys.Escape)
                    {
                        GameAction action = _configurableActions[_optionIndex];
                        _inputConfig.Bindings[action] = k;
                        _inputConfig.Save();
                    }
                    _waitingForKey = false;
                    _transitionCooldown = 0.15f;
                    break;
                }
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

        int startY = 340;
        int rowWidth = 760;
        int rowX = (1920 - rowWidth) / 2;

        for (int i = 0; i < totalItems; i++)
        {
            Rectangle rowRect = new Rectangle(rowX, startY + i * 74, rowWidth, 56);
            if (rowRect.Contains(mouse.Position))
            {
                _optionIndex = i;
                if (mouse.LeftButton == ButtonState.Pressed && _prevMouse.LeftButton == ButtonState.Released)
                {
                    return ExecuteOptionItem(i, kb, mouse);
                }
            }
        }

        if (IsKeyPressed(kb, Keys.Enter) || IsKeyPressed(kb, Keys.Space))
        {
            return ExecuteOptionItem(_optionIndex, kb, mouse);
        }

        _prevKb = kb;
        _prevMouse = mouse;
        return ScreenState.Options;
    }

    private ScreenState ExecuteOptionItem(int index, KeyboardState kb, MouseState mouse)
    {
        _prevKb = kb;
        _prevMouse = mouse;
        _transitionCooldown = 0.20f;

        if (index < _configurableActions.Length)
        {
            _waitingForKey = true;
            return ScreenState.Options;
        }
        else if (index == _configurableActions.Length)
        {
            _inputConfig.SetDefaults();
            _inputConfig.Save();
            return ScreenState.Options;
        }
        else
        {
            return ScreenState.MainMenu;
        }
    }

    public void DrawMainMenu(SpriteBatch sb)
    {
        DrawRect(sb, 0, 0, 1920, 1080, _bgDark);

        // Retícula sutil de fondo estilo plano técnico ferroviario
        for (int x = 0; x < 1920; x += 80)
            DrawRect(sb, x, 0, 1, 1080, Color.White * 0.02f);
        for (int y = 0; y < 1080; y += 80)
            DrawRect(sb, 0, y, 1920, 1, Color.White * 0.02f);

        // Franja superior e inferior corporativa L1
        DrawRect(sb, 0, 0, 1920, 10, _tmbRed);
        DrawRect(sb, 0, 1070, 1920, 10, _tmbRed);

        // Tarjeta principal de cabecera centrada (860px de ancho)
        int cardW = 860;
        int cardH = 250;
        int cardX = (1920 - cardW) / 2;
        int cardY = 170;

        DrawBoxWithBorder(sb, cardX, cardY, cardW, cardH, _panelBg, _panelBorder);
        DrawRect(sb, cardX, cardY, cardW, 8, _tmbRed);

        // Distintivo L1 estilo señalética de estación
        int badgeX = cardX + 48;
        int badgeY = cardY + 52;
        DrawBoxWithBorder(sb, badgeX, badgeY, 110, 110, _tmbRed, Color.White);
        DrawCenteredText(sb, "L1", new Rectangle(badgeX, badgeY, 110, 110), Color.White, 2.2f);

        // Títulos alineados dentro de la tarjeta
        int textX = badgeX + 145;
        sb.DrawString(_font, "METRO DE BARCELONA", new Vector2(textX, cardY + 52), Color.White, 0f, Vector2.Zero, 1.5f, SpriteEffects.None, 0f);
        sb.DrawString(_font, "SIMULADOR DE CONDUCCION FERROVIARIA - SERIE 6000", new Vector2(textX, cardY + 102), Color.LightGray);
        DrawRect(sb, textX, cardY + 135, 500, 2, _panelBorder);
        sb.DrawString(_font, "SERVICIO: HOSPITAL DE BELLVITGE <-> CAN SERRA", new Vector2(textX, cardY + 150), Color.Cyan);
        sb.DrawString(_font, "SISTEMA DE PROTECCION ATP Y ENCLAVAMIENTO CCM ACTIVO", new Vector2(textX, cardY + 178), Color.Gray);

        // Botones del menú principal
        int startY = 510;
        int btnWidth = 540;
        int btnX = (1920 - btnWidth) / 2;

        for (int i = 0; i < _menuOptions.Length; i++)
        {
            bool selected = (i == _menuIndex);
            Rectangle rect = new Rectangle(btnX, startY + i * 84, btnWidth, 64);

            DrawBoxWithBorder(sb, rect.X, rect.Y, rect.Width, rect.Height, selected ? _tmbRed : _panelBg, selected ? Color.White : _panelBorder);
            if (selected)
                DrawRect(sb, rect.X, rect.Y, 10, rect.Height, Color.White);

            DrawCenteredText(sb, _menuOptions[i], rect, selected ? Color.White : Color.LightGray, 1.15f);
        }

        // Pie de página centrado
        DrawCenteredText(sb, "Navegacion: [Flechas / W-S / Raton]   |   Confirmar: [Enter / Clic Izquierdo]", new Rectangle(0, 990, 1920, 40), Color.Gray, 1.0f);
    }

    public void DrawOptions(SpriteBatch sb)
    {
        DrawRect(sb, 0, 0, 1920, 1080, _bgDark);
        DrawRect(sb, 0, 0, 1920, 10, _tmbRed);
        DrawRect(sb, 0, 1070, 1920, 10, _tmbRed);

        DrawCenteredText(sb, "CONFIGURACION DE CONTROLES DE CABINA", new Rectangle(0, 170, 1920, 50), Color.White, 1.5f);
        DrawCenteredText(sb, "Seleccione una accion y pulse la nueva tecla deseada (ESC para cancelar)", new Rectangle(0, 225, 1920, 40), Color.LightGray, 1.0f);

        int startY = 340;
        int rowWidth = 760;
        int rowX = (1920 - rowWidth) / 2;

        for (int i = 0; i < _configurableActions.Length; i++)
        {
            GameAction action = _configurableActions[i];
            bool selected = (i == _optionIndex);
            Rectangle row = new Rectangle(rowX, startY + i * 74, rowWidth, 56);

            DrawBoxWithBorder(sb, row.X, row.Y, row.Width, row.Height, selected ? new Color(36, 42, 54) : _panelBg, selected ? _tmbRed : _panelBorder);
            if (selected)
                DrawRect(sb, row.X, row.Y, 8, row.Height, _tmbRed);

            string actionLabel = action switch
            {
                GameAction.Accelerate => "Subir Muesca / Acelerar",
                GameAction.Brake => "Bajar Muesca / Frenar",
                GameAction.Doors => "Abrir / Cerrar Puertas",
                GameAction.ResetATP => "Rearmar Freno Emergencia ATP",
                _ => action.ToString()
            };

            string keyText = (_waitingForKey && selected)
                ? "[ PULSE UNA TECLA... ]"
                : $"[ {_inputConfig.Bindings[action]} ]";

            Color keyColor = (_waitingForKey && selected) ? Color.Gold : Color.Cyan;

            // Etiqueta a la izquierda y tecla alineada limpiamente a la derecha
            sb.DrawString(_font, actionLabel, new Vector2(row.X + 28, row.Y + 17), Color.White);
            Vector2 keySize = _font.MeasureString(keyText);
            sb.DrawString(_font, keyText, new Vector2(row.Right - keySize.X - 28, row.Y + 17), keyColor);
        }

        int resetIdx = _configurableActions.Length;
        bool resetSel = (_optionIndex == resetIdx);
        Rectangle resetRect = new Rectangle(rowX, startY + resetIdx * 74, rowWidth, 56);
        DrawBoxWithBorder(sb, resetRect.X, resetRect.Y, resetRect.Width, resetRect.Height, resetSel ? new Color(75, 38, 38) : _panelBg, resetSel ? Color.White : _panelBorder);
        DrawCenteredText(sb, "RESTAURAR TECLAS POR DEFECTO", resetRect, resetSel ? Color.White : Color.LightGray, 1.0f);

        int backIdx = _configurableActions.Length + 1;
        bool backSel = (_optionIndex == backIdx);
        Rectangle backRect = new Rectangle(rowX, startY + backIdx * 74, rowWidth, 56);
        DrawBoxWithBorder(sb, backRect.X, backRect.Y, backRect.Width, backRect.Height, backSel ? _tmbRed : _panelBg, backSel ? Color.White : _panelBorder);
        DrawCenteredText(sb, "VOLVER AL MENU PRINCIPAL", backRect, Color.White, 1.05f);
    }

    private void DrawCenteredText(SpriteBatch sb, string text, Rectangle bounds, Color color, float scale)
    {
        Vector2 size = _font.MeasureString(text) * scale;
        Vector2 pos = new Vector2(
            bounds.X + (bounds.Width - size.X) / 2f,
            bounds.Y + (bounds.Height - size.Y) / 2f
        );
        sb.DrawString(_font, text, pos, color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
    }

    private void DrawBoxWithBorder(SpriteBatch sb, int x, int y, int w, int h, Color fill, Color border)
    {
        DrawRect(sb, x, y, w, h, border);
        DrawRect(sb, x + 2, y + 2, w - 4, h - 4, fill);
    }

    private bool IsKeyPressed(KeyboardState current, Keys key)
    {
        return current.IsKeyDown(key) && !_prevKb.IsKeyDown(key);
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int width, int height, Color color)
    {
        sb.Draw(_pixel, new Rectangle(x, y, width, height), color);
    }
}