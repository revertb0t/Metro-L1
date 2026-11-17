// Designed & Developed by Revert_b0t
namespace videojuego.World;

/// <summary>
/// Representa una estación de la Línea 1 del Metro de Barcelona. Contiene nombre oficial, posición kilométrica, longitud de andén, correspondencias con otras líneas, archivo de megafonía asociado y estado de visita durante la partida.
/// </summary>
public class Station(string name, float positionMeters, string connections = "", float platformLength = 95f, int assignedTrack = 1, string audioFile = "")
{
    public string Name { get; } = name;
    public string Connections { get; } = connections;
    public float PositionMeters { get; } = positionMeters;
    public float PlatformLength { get; } = platformLength;
    public int AssignedTrack { get; set; } = assignedTrack;
    public bool Visited { get; set; }
    public string AudioFile { get; } = audioFile;
    public bool AnnouncementPlayed { get; set; }
}
