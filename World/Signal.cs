namespace videojuego.World;

public enum SignalAspect
{
    Green,  // Vía libre
    Yellow, // Precaución (límite 40 km/h)
    Red     // Parada absoluta
}

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