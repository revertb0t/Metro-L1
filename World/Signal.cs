// Designed & Developed by Revert_b0t
namespace videojuego.World;

/// <summary>
/// Define los tres aspectos posibles de una señal ferroviaria según el sistema de señalización del Metro de Barcelona.
/// </summary>
public enum SignalAspect
{
    Green,  // Vía libre
    Yellow, // Precaución (límite 40 km/h)
    Red     // Parada absoluta
}

/// <summary>
/// Representa una señal luminosa del sistema de enclavamiento CCM. Contiene su posición kilométrica, aspecto actual, texto del cartel luminoso y límite de velocidad asociado.
/// </summary>
public class Signal
{
    public float PositionMeters { get; }
    public SignalAspect Aspect { get; set; }
    public string SignText { get; set; }
    public int SpeedLimitKmh { get; set; }

    public Signal(float positionMeters, SignalAspect aspect = SignalAspect.Green, string signText = "", int speedLimitKmh = 75)
    {
        PositionMeters = positionMeters;
        Aspect = aspect;
        SignText = signText;
        SpeedLimitKmh = speedLimitKmh;
    }
}
