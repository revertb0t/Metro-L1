// Designed & Developed by Revert_b0t
using System.Collections.Generic;
using videojuego.Entities;
using videojuego.World;

namespace videojuego.Systems;

/// <summary>
/// Inteligencia artificial de señalización ferroviaria. Simula un tren precedente (fantasma) que circula por delante del jugador y controla los aspectos de las señales (verde, amarillo, rojo) según distancia de bloqueo. Implementa protección ATP por rebase de señal en rojo.
/// </summary>
public class SignalingAI
{
    private float _trafficTimer = 0f;
    private float _ghostTrainPosition = 950f;

    public void Update(float dt, MetroTrain playerTrain, List<Signal> signals, List<Station> stations)
    {
        _trafficTimer += dt;
        if (_trafficTimer % 42f < 28f && _ghostTrainPosition < 20400f) _ghostTrainPosition += 16.5f * dt;

        if (_ghostTrainPosition - playerTrain.PositionMeters > 1400f)
            _ghostTrainPosition = playerTrain.PositionMeters + 850f;

        for (int i = 0; i < signals.Count; i++)
        {
            var signal = signals[i];
            if (signal.SignText == "TOPE FIN L1") { signal.Aspect = SignalAspect.Red; continue; }

            float distanceToGhost = _ghostTrainPosition - signal.PositionMeters;
            signal.Aspect = distanceToGhost > 0 && distanceToGhost < 230f
                ? SignalAspect.Red
                : distanceToGhost >= 230f && distanceToGhost < 500f ? SignalAspect.Yellow : SignalAspect.Green;

            if (signal.Aspect == SignalAspect.Red &&
                playerTrain.PositionMeters > signal.PositionMeters &&
                playerTrain.PositionMeters < signal.PositionMeters + 5f &&
                playerTrain.VelocityMps > 0.5f)
                playerTrain.TriggerEmergencyBrake();
        }
    }
}
