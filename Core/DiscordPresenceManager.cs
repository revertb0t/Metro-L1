// Designed & Developed by Revert_b0t
using System;
using System.Collections.Generic;
using DiscordRPC;
using videojuego.Entities;
using videojuego.World;

namespace videojuego.Core;

/// <summary>
/// Gestiona la integración con Discord Rich Presence (IPC local) para mostrar
/// el estado de conducción, estación actual y velocidad en el perfil del jugador.
/// </summary>
public class DiscordPresenceManager : IDisposable
{
    // Pega aquí tu Application ID del Discord Developer Portal
    private const string DiscordAppId = "TU_APPLICATION_ID_AQUI";

    private DiscordRpcClient _client;
    private readonly Timestamps _sessionStart;
    private float _updateTimer = 0f;
    private bool _isInitialized = false;

    public DiscordPresenceManager()
    {
        _sessionStart = Timestamps.Now;

        try
        {
            if (string.IsNullOrWhiteSpace(DiscordAppId) || DiscordAppId == "TU_APPLICATION_ID_AQUI")
                return;

            _client = new DiscordRpcClient(DiscordAppId);
            _client.Initialize();
            _isInitialized = true;

            SetMenuPresence("En el menú principal");
        }
        catch
        {
            // Si Discord no está instalado o bloquea IPC, el simulador continúa sin fallos
            _isInitialized = false;
        }
    }

    public void SetMenuPresence(string statusText)
    {
        if (!_isInitialized || _client == null || _client.IsDisposed) return;

        _client.SetPresence(new RichPresence()
        {
            Details = "L1: Hospital de Bellvitge ↔ Fondo",
            State = statusText,
            Timestamps = _sessionStart,
            Assets = new Assets()
            {
                LargeImageKey = "logo_l1",
                LargeImageText = "Metro L1 by Revert_b0t"
            }
        });
    }

    public void UpdateGameplay(float dt, ScreenState screen, MetroTrain train, List<Station> stations)
    {
        if (!_isInitialized || _client == null || _client.IsDisposed) return;

        _updateTimer += dt;
        // Discord permite actualizar cada ~15s, pero cada 3s mantiene la cola fluida sin spam
        if (_updateTimer < 3.0f) return;
        _updateTimer = 0f;

        if (screen == ScreenState.Paused)
        {
            SetMenuPresence("Servicio en pausa");
            return;
        }

        // Buscar la estación actual (si está parado en andén) o la próxima parada
        Station nextStation = null;
        bool atPlatform = false;

        foreach (var st in stations)
        {
            float distToStop = st.PositionMeters - train.PositionMeters;
            if (Math.Abs(distToStop) <= 12f && train.VelocityKmh < 1f)
            {
                nextStation = st;
                atPlatform = true;
                break;
            }
            if (distToStop > 0f)
            {
                nextStation = st;
                break;
            }
        }

        string detailsText;
        string stateText;

        if (nextStation == null)
        {
            detailsText = "Fin de trayecto en Fondo";
            stateText = "Servicio L1 completado";
        }
        else if (atPlatform)
        {
            detailsText = $"En estación: {nextStation.Name}";
            stateText = train.DoorsOpen ? "Embarque de viajeros (Puertas abiertas)" : "Listo para salir";
        }
        else
        {
            detailsText = $"Próxima parada: {nextStation.Name}";
            stateText = $"Conduciendo Serie 6000 · {train.VelocityKmh:0} km/h";
        }

        _client.SetPresence(new RichPresence()
        {
            Details = detailsText,
            State = stateText,
            Timestamps = _sessionStart,
            Assets = new Assets()
            {
                LargeImageKey = "logo_l1",
                LargeImageText = "Metro de Barcelona · Línea 1",
                SmallImageKey = "tren_s6000",
                SmallImageText = train.EmergencyBrakeTriggered ? "Freno de emergencia ATP" : "ATP Activo"
            }
        });
    }

    public void Dispose()
    {
        if (_client != null && !_client.IsDisposed)
        {
            _client.ClearPresence();
            _client.Dispose();
        }
    }
}