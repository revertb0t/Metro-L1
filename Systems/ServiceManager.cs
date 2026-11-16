using System;
using System.Collections.Generic;
using videojuego.Entities;
using videojuego.World;

namespace videojuego.Systems;

public class ServiceManager
{
    public int Score { get; private set; } = 1000;
    public int PassengersOnBoard { get; private set; } = 42;
    public int CurrentSpeedLimitKmh { get; private set; } = 75;
    public string StatusMessage { get; private set; } = "EN ESTACION - Cierre puertas [ESPACIO] para salir";
    public float BoardingProgress { get; private set; } = 1f; // 0.0 a 1.0
    public bool IsBoarding { get; private set; } = false;

    private Station _activeStation = null;

    public void Update(float dt, MetroTrain train, List<Station> stations, List<Signal> signals)
    {
        // 1. Determinar el límite de velocidad vigente según la última señal rebasada
        int activeLimit = 75;
        foreach (var sig in signals)
        {
            if (train.PositionMeters >= sig.PositionMeters)
            {
                activeLimit = sig.Aspect == SignalAspect.Yellow ? Math.Min(sig.SpeedLimitKmh, 40) : sig.SpeedLimitKmh;
            }
        }
        CurrentSpeedLimitKmh = activeLimit;

        // Supervisión ATP por exceso de velocidad (+8 km/h de margen sobre el límite)
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

        // 2. Comprobar proximidad a estaciones
        Station nearest = null;
        float minDistance = float.MaxValue;

        foreach (var st in stations)
        {
            float dist = Math.Abs(train.PositionMeters - st.PositionMeters);
            if (dist < minDistance)
            {
                minDistance = dist;
                nearest = st;
            }
        }

        if (nearest != null && minDistance <= 12f)
        {
            float signedError = train.PositionMeters - nearest.PositionMeters; // Negativo = corto, Positivo = pasado

            if (Math.Abs(train.VelocityMps) < 0.05f)
            {
                if (!train.DoorsOpen && !nearest.Visited && nearest.PositionMeters > 60f)
                {
                    StatusMessage = $"Detenido ({signedError:+0.0;-0.0;0.0}m). Pulse [ESPACIO] para abrir puertas";
                }
                else if (train.DoorsOpen)
                {
                    if (!nearest.Visited && nearest.PositionMeters > 60f)
                    {
                        if (!IsBoarding)
                        {
                            IsBoarding = true;
                            BoardingProgress = 0f;
                            int precisionBonus = Math.Max(0, 100 - (int)(Math.Abs(signedError) * 15));
                            Score += precisionBonus;
                        }

                        BoardingProgress += dt / 3.5f; // 3.5 segundos de intercambio de viajeros
                        StatusMessage = $"EMBARCANDO VIAJEROS en {nearest.Name}... {(int)(Math.Min(1f, BoardingProgress) * 100)}%";

                        if (BoardingProgress >= 1f)
                        {
                            BoardingProgress = 1f;
                            IsBoarding = false;
                            nearest.Visited = true;
                            PassengersOnBoard = Math.Clamp(PassengersOnBoard + new Random().Next(-15, 35), 10, 450);
                            StatusMessage = $"Embarque completado en {nearest.Name}. Pulse [ESPACIO] para cerrar";
                        }
                    }
                    else
                    {
                        StatusMessage = $"Listo para salir de {nearest.Name}. Pulse [ESPACIO] para cerrar puertas";
                    }
                }
            }
            else
            {
                StatusMessage = $"Entrando en {nearest.Name} - Distancia a marca: {(nearest.PositionMeters - train.PositionMeters):0.0}m";
            }
        }
        else
        {
            // En pleno túnel entre estaciones
            if (IsBoarding)
            {
                IsBoarding = false;
                Score = Math.Max(0, Score - 100);
            }

            string nextStationName = "FIN DE LINEA";
            float distToNext = 0f;
            foreach (var st in stations)
            {
                if (st.PositionMeters > train.PositionMeters + 12f)
                {
                    nextStationName = st.Name;
                    distToNext = st.PositionMeters - train.PositionMeters;
                    break;
                }
            }

            if (train.VelocityKmh > CurrentSpeedLimitKmh)
                StatusMessage = $"PRECAUCION: Reduzca velocidad a {CurrentSpeedLimitKmh} km/h";
            else
                StatusMessage = $"Proxima estacion: {nextStationName} ({distToNext:0} m)";
        }
    }
}