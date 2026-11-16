using System;

namespace videojuego.Entities;

public class MetroTrain
{
    public float PositionMeters { get; set; } = 50f;
    public float VelocityMps { get; private set; } = 0f; // Metros por segundo
    public float VelocityKmh => VelocityMps * 3.6f;

    // Muescas de control: -4 (Emergencia), -3 a -1 (Freno), 0 (Deriva), +1 a +4 (Tracción)
    public int Notch { get; private set; } = 0;
    public bool DoorsOpen { get; private set; } = true;
    public bool EmergencyBrakeTriggered { get; private set; } = false;

    private const float MaxSpeedMps = 22.22f; // 80 km/h velocidad máxima comercial L1
    private const float TrainLengthMeters = 85f;
    public float Length => TrainLengthMeters;

    public void IncreaseNotch()
    {
        if (DoorsOpen || EmergencyBrakeTriggered) return;
        Notch = Math.Clamp(Notch + 1, -4, 4);
    }

    public void DecreaseNotch()
    {
        if (EmergencyBrakeTriggered) return;
        Notch = Math.Clamp(Notch - 1, -4, 4);
    }

    public void ToggleDoors()
    {
        // Solo se pueden abrir puertas si el tren está prácticamente detenido
        if (Math.Abs(VelocityMps) < 0.05f)
        {
            DoorsOpen = !DoorsOpen;
            if (DoorsOpen) Notch = -2; // Aplicar freno de estacionamiento al abrir puertas
        }
    }

    public void TriggerEmergencyBrake()
    {
        EmergencyBrakeTriggered = true;
        Notch = -4;
    }

    public void ResetEmergencyBrake()
    {
        if (Math.Abs(VelocityMps) < 0.01f)
        {
            EmergencyBrakeTriggered = false;
            Notch = -2;
        }
    }

    public void Update(float dt)
    {
        if (DoorsOpen && Notch > 0)
            Notch = 0;

        float acceleration = 0f;

        if (Notch > 0)
        {
            // Aceleración progresiva según muesca de tracción (máx ~1.1 m/s²)
            acceleration = (Notch / 4f) * 1.1f;
        }
        else if (Notch < 0)
        {
            // Frenado progresivo (emergencia frena a 1.6 m/s²)
            acceleration = (Notch / 4f) * 1.6f;
        }
        else
        {
            // Resistencia aerodinámica y de rodadura suave en deriva
            acceleration = -0.04f;
        }

        VelocityMps += acceleration * dt;
        VelocityMps = Math.Clamp(VelocityMps, 0f, MaxSpeedMps);
        PositionMeters += VelocityMps * dt;
    }
}