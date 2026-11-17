using System;
using System.Collections.Generic;
using System.IO;
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
        // [BLOQUE: Inicialización y carga de configuración]
        SetDefaults();
        Load();
    }

    public void SetDefaults()
    {
        // [BLOQUE: Mapeo predeterminado de teclas]
        Bindings[GameAction.Accelerate] = Keys.W;
        Bindings[GameAction.Brake] = Keys.S;
        Bindings[GameAction.Doors] = Keys.Space;
        Bindings[GameAction.ResetATP] = Keys.R;
    }

    public void Save()
    {
        // [BLOQUE: Serialización de controles]
        var lines = new List<string>();
        foreach (var pair in Bindings)
        {
            lines.Add($"{pair.Key}={pair.Value}");
        }

        // [BLOQUE: Escritura protegida en archivo]
        try
        {
            File.WriteAllLines(ConfigFile, lines);
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }

    public void Load()
    {
        // [BLOQUE: Verificación de existencia del archivo]
        if (!File.Exists(ConfigFile)) return;

        // [BLOQUE: Lectura protegida y deserialización]
        try
        {
            foreach (var line in File.ReadAllLines(ConfigFile))
            {
                var parts = line.Split('=');
                if (parts.Length == 2 &&
                    Enum.TryParse<GameAction>(parts[0], out var action) &&
                    Enum.TryParse<Keys>(parts[1], out var key))
                {
                    Bindings[action] = key;
                }
            }
        }
        catch (Exception)
        {
            // Silently ignore
        }
    }
}
