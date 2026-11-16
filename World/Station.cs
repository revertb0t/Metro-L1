namespace videojuego.World;

public class Station
{
    public string Name { get; }
    public string Connections { get; } // Ej: "FGC L8" o "Rodalies"
    public float PositionMeters { get; }
    public float PlatformLength { get; }
    public int AssignedTrack { get; set; }
    public bool Visited { get; set; } = false;

    public Station(string name, float positionMeters, string connections = "", float platformLength = 95f, int assignedTrack = 1)
    {
        Name = name;
        Connections = connections;
        PositionMeters = positionMeters;
        PlatformLength = platformLength;
        AssignedTrack = assignedTrack;
    }
}