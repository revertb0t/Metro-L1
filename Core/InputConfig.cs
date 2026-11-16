using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Xna.Framework.Input;

namespace videojuego.Core;

public enum GameAction
{
    Accelerate,
    Brake,
    Doors,
    ResetATP
}

public class InputConfig
{
    private const string ConfigFile = "controls.cfg";
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
        var lines = new List<string>();
        foreach (var pair in Bindings)
        {
            lines.Add($"{pair.Key}={pair.Value}");
        }
        File.WriteAllLines(ConfigFile, lines);
    }

    public void Load()
    {
        if (!File.Exists(ConfigFile)) return;

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
}