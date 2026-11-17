// Designed & Developed by Revert_b0t
using System;
using System.Collections.Generic;
using videojuego.Entities;
using videojuego.World;

namespace videojuego.Systems;

/// <summary>
/// Sistema de gestión del servicio de la línea L1. Controla la puntuación, pasajeros, límites de velocidad, embarque/desembarque, supervisión ATP y mensajes de estado del CCM (Centro de Control de Movimiento).
/// </summary>
public class ServiceManager
{
    private static readonly Random _rng = new(42);

    public int Score { get; private set; } = 1000;
    public int PassengersOnBoard { get; private set; } = 42;
    public int CurrentSpeedLimitKmh { get; private set; } = 75;
    public string StatusMessage { get; private set; } = "EN ESTACION - Cierre puertas [ESPACIO] para salir";
    public float BoardingProgress { get; private set; } = 1f;
    public bool IsBoarding { get; private set; }

    public void Update(float dt, MetroTrain train, List<Station> stations, List<Signal> signals)
    {
        int activeLimit = 75;
        for (int i = 0; i < signals.Count; i++)
        {
            var sig = signals[i];
            if (train.PositionMeters >= sig.PositionMeters)
                activeLimit = sig.Aspect == SignalAspect.Yellow ? Math.Min(sig.SpeedLimitKmh, 40) : sig.SpeedLimitKmh;
        }
        CurrentSpeedLimitKmh = activeLimit;

        if (train.VelocityKmh > CurrentSpeedLimitKmh + 8f && !train.EmergencyBrakeTriggered)
        {
            train.TriggerEmergencyBrake();
            Score = Math.Max(0, Score - 150);
            StatusMessage = $"FRENO ATP: Exceso de velocidad ({CurrentSpeedLimitKmh} km/h max)";
            return;
        }

        if (train.EmergencyBrakeTriggered)
        {
            StatusMessage = "EMERGENCIA ACTIVA - Detenga el tren y pulse [R]";
            return;
        }

        Station nearest = null;
        float minDistance = float.MaxValue;
        for (int i = 0; i < stations.Count; i++)
        {
            float dist = Math.Abs(train.PositionMeters - stations[i].PositionMeters);
            if (dist < minDistance) { minDistance = dist; nearest = stations[i]; }
        }

        if (nearest != null && minDistance <= 12f)
        {
            float signedError = train.PositionMeters - nearest.PositionMeters;

            if (Math.Abs(train.VelocityMps) < 0.05f)
            {
                if (!train.DoorsOpen && !nearest.Visited && nearest.PositionMeters > 60f)
                    StatusMessage = $"Detenido ({signedError:+0.0;-0.0;0.0}m). Pulse [ESPACIO] para abrir puertas";
                else if (train.DoorsOpen)
                {
                    if (!nearest.Visited && nearest.PositionMeters > 60f)
                    {
                        if (!IsBoarding)
                        {
                            IsBoarding = true;
                            BoardingProgress = 0f;
                            Score += Math.Max(0, 100 - (int)(Math.Abs(signedError) * 15));
                        }

                        BoardingProgress += dt / 3.5f;
                        StatusMessage = $"EMBARCANDO VIAJEROS en {nearest.Name}... {(int)(Math.Min(1f, BoardingProgress) * 100)}%";

                        if (BoardingProgress >= 1f)
                        {
                            BoardingProgress = 1f;
                            IsBoarding = false;
                            nearest.Visited = true;
                            PassengersOnBoard = Math.Clamp(PassengersOnBoard + _rng.Next(-15, 35), 10, 450);
                            StatusMessage = $"Embarque completado en {nearest.Name}. Pulse [ESPACIO] para cerrar";
                        }
                    }
                    else
                        StatusMessage = $"Listo para salir de {nearest.Name}. Pulse [ESPACIO] para cerrar puertas";
                }
            }
            else
                StatusMessage = $"Entrando en {nearest.Name} - Distancia a marca: {(nearest.PositionMeters - train.PositionMeters):0.0}m";
        }
        else
        {
            if (IsBoarding) { IsBoarding = false; Score = Math.Max(0, Score - 100); }

            string nextStationName = "FIN DE LINEA";
            float distToNext = 0f;
            for (int i = 0; i < stations.Count; i++)
            {
                if (stations[i].PositionMeters > train.PositionMeters + 12f)
                {
                    nextStationName = stations[i].Name;
                    distToNext = stations[i].PositionMeters - train.PositionMeters;
                    break;
                }
            }

            StatusMessage = train.VelocityKmh > CurrentSpeedLimitKmh
                ? $"PRECAUCION: Reduzca velocidad a {CurrentSpeedLimitKmh} km/h"
                : $"Proxima estacion: {nextStationName} ({distToNext:0} m)";
        }
    }
}
