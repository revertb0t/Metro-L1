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
        // [BLOQUE: Avance del tren precedente (fantasma)]
        _trafficTimer += dt;
        float cycle = _trafficTimer % 42f;

        // El tren precedente circula durante 28s y hace parada de 14s en estaciones
        if (cycle < 28f && _ghostTrainPosition < 20400f)
        {
            _ghostTrainPosition += 16.5f * dt;
        }

        // [BLOQUE: Reinserción del tren fantasma por distancia excesiva]
        // Si el tren precedente se aleja demasiado (> 1400m), simulamos que entra otro tren en bloque
        if (_ghostTrainPosition - playerTrain.PositionMeters > 1400f)
        {
            _ghostTrainPosition = playerTrain.PositionMeters + 850f;
        }

        // [BLOQUE: Cálculo de aspectos de señales por cantón]
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

            // [BLOQUE: Protección ATP por rebase de señal en rojo]
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
