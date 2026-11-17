namespace videojuego.World;

/// <summary>
/// Representa una estación de la Línea 1 del Metro de Barcelona. Contiene nombre oficial, posición kilométrica, longitud de andén, correspondencias con otras líneas, archivo de megafonía asociado y estado de visita durante la partida.
/// </summary>
public class Station
{
    public string Name { get; }
    public string Connections { get; }
    public float PositionMeters { get; }
    public float PlatformLength { get; }
    public int AssignedTrack { get; set; }
    public bool Visited { get; set; } = false;

    public string AudioFile { get; }
    public bool AnnouncementPlayed { get; set; } = false;

    public Station(string name, float positionMeters, string connections = "", float platformLength = 95f, int assignedTrack = 1, string audioFile = "")
    {
        Name = name;
        Connections = connections;
        PositionMeters = positionMeters;
        PlatformLength = platformLength;
        AssignedTrack = assignedTrack;
        AudioFile = audioFile;
    }
}
