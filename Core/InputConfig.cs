// Designed & Developed by Revert_b0t
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework.Input;

namespace videojuego.Core;

/// <summary>
/// Acciones del jugador configurables desde el menú de opciones.
/// </summary>
public enum GameAction
{
    Accelerate,
    Brake,
    Doors,
    ResetATP
}

/// <summary>
/// Gestiona la configuración de controles del jugador. Lee y escribe el archivo controls.cfg junto al ejecutable, con protección contra errores de E/S.
/// </summary>
public class InputConfig
{
    private static readonly string ConfigFile = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "controls.cfg");
    public Dictionary<GameAction, Keys> Bindings { get; } = new();

    public InputConfig()
    {
        SetDefaults();
        Load();
    }

    public void SetDefaults()
    {
        Bindings[GameAction.Accelerate] = Keys.W;
        Bindings[GameAction.Brake] = Keys.S;
        Bindings[GameAction.Doors] = Keys.Space;
        Bindings[GameAction.ResetATP] = Keys.R;
    }

    public void Save()
    {
        try { File.WriteAllLines(ConfigFile, Bindings.Select(pair => $"{pair.Key}={pair.Value}")); }
        catch (Exception) { }
    }

    public void Load()
    {
        if (!File.Exists(ConfigFile)) return;

        try
        {
            foreach (var parts in File.ReadAllLines(ConfigFile).Select(line => line.Split('=')))
                if (parts.Length == 2 &&
                    Enum.TryParse<GameAction>(parts[0], out var action) &&
                    Enum.TryParse<Keys>(parts[1], out var key))
                    Bindings[action] = key;
        }
        catch (Exception) { }
    }
}
