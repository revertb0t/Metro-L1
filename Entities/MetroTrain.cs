// Designed & Developed by Revert_b0t
using System;

namespace videojuego.Entities;

/// <summary>
/// Modelo físico del tren Serie 6000 de la L1 de Barcelona. Gestiona posición, velocidad, muescas de tracción/frenado (-4 a +4), apertura de puertas y freno de emergencia ATP. Velocidad máxima comercial: 80 km/h.
/// </summary>
public class MetroTrain
{
    public float PositionMeters { get; set; } = 50f;
    public float VelocityMps { get; private set; }
    public float VelocityKmh => VelocityMps * 3.6f;

    public int Notch { get; private set; }
    public bool DoorsOpen { get; private set; } = true;
    public bool EmergencyBrakeTriggered { get; private set; }

    private const float MaxSpeedMps = 22.22f;
    private const float TrainLengthMeters = 85f;
    public float Length => TrainLengthMeters;

    public void IncreaseNotch()
    {
        if (!DoorsOpen && !EmergencyBrakeTriggered) Notch = Math.Clamp(Notch + 1, -4, 4);
    }

    public void DecreaseNotch()
    {
        if (!EmergencyBrakeTriggered) Notch = Math.Clamp(Notch - 1, -4, 4);
    }

    public void ToggleDoors()
    {
        if (Math.Abs(VelocityMps) < 0.05f) { DoorsOpen = !DoorsOpen; if (DoorsOpen) Notch = -2; }
    }

    public void TriggerEmergencyBrake()
    {
        EmergencyBrakeTriggered = true;
        Notch = -4;
    }

    public void ResetEmergencyBrake()
    {
        if (Math.Abs(VelocityMps) < 0.01f) { EmergencyBrakeTriggered = false; Notch = -2; }
    }

    public void Update(float dt)
    {
        if (DoorsOpen && Notch > 0) Notch = 0;

        float acceleration = Notch > 0 ? (Notch / 4f) * 1.1f : Notch < 0 ? (Notch / 4f) * 1.6f : -0.04f;

        VelocityMps = Math.Clamp(VelocityMps + acceleration * dt, 0f, MaxSpeedMps);
        PositionMeters += VelocityMps * dt;
    }
}
