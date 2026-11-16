using System.Collections.Generic;
using videojuego.Entities;
using videojuego.World;

namespace videojuego.Systems;

public class SignalingAI
{
    private float _trafficTimer = 0f;
    private float _ghostTrainPosition = 950f;

    public void Update(float dt, MetroTrain playerTrain, List<Signal> signals, List<Station> stations)
    {
        _trafficTimer += dt;
        float cycle = _trafficTimer % 42f;

        // El tren precedente circula durante 28s y hace parada de 14s en estaciones
        if (cycle < 28f && _ghostTrainPosition < 20400f)
        {
            _ghostTrainPosition += 16.5f * dt;
        }

        // Si el tren precedente se aleja demasiado (> 1400m), simulamos que entra otro tren en bloque
        if (_ghostTrainPosition - playerTrain.PositionMeters > 1400f)
        {
            _ghostTrainPosition = playerTrain.PositionMeters + 850f;
        }

        foreach (var signal in signals)
        {
            // Mantener siempre en rojo el tope final de Fondo
            if (signal.SignText == "TOPE FIN L1")
            {
                signal.Aspect = SignalAspect.Red;
                continue;
            }

            float distanceToGhost = _ghostTrainPosition - signal.PositionMeters;

            if (distanceToGhost > 0 && distanceToGhost < 230f)
            {
                signal.Aspect = SignalAspect.Red;
            }
            else if (distanceToGhost >= 230f && distanceToGhost < 500f)
            {
                signal.Aspect = SignalAspect.Yellow;
            }
            else
            {
                signal.Aspect = SignalAspect.Green;
            }

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