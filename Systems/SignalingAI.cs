using System.Collections.Generic;
using videojuego.Entities;
using videojuego.World;

namespace videojuego.Systems;

public class SignalingAI
{
    private float _trafficTimer = 0f;
    private float _ghostTrainPosition = 950f; // Tren IA circulando por delante de ti

    public void Update(float dt, MetroTrain playerTrain, List<Signal> signals, List<Station> stations)
    {
        // Simulamos un tren IA que circula por delante a ~55 km/h (15.2 m/s) y para brevemente
        _trafficTimer += dt;
        float cycle = _trafficTimer % 45f;
        if (cycle < 30f)
        {
            _ghostTrainPosition += 15.5f * dt;
        }

        // El algoritmo evalúa cada semáforo según la distancia al tren que va delante
        foreach (var signal in signals)
        {
            float distanceToGhost = _ghostTrainPosition - signal.PositionMeters;

            if (distanceToGhost > 0 && distanceToGhost < 220f)
            {
                signal.Aspect = SignalAspect.Red;
            }
            else if (distanceToGhost >= 220f && distanceToGhost < 480f)
            {
                signal.Aspect = SignalAspect.Yellow;
            }
            else
            {
                signal.Aspect = SignalAspect.Green;
            }

            // Sistema ATP: si el jugador rebasa un semáforo en rojo, salta freno de emergencia
            if (signal.Aspect == SignalAspect.Red &&
                playerTrain.PositionMeters > signal.PositionMeters &&
                playerTrain.PositionMeters < signal.PositionMeters + 5f &&
                playerTrain.VelocityMps > 0.5f)
            {
                playerTrain.TriggerEmergencyBrake();
            }
        }
    }
}