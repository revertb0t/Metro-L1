using System.Collections.Generic;

namespace videojuego.World;

/// <summary>
/// Constructor estático de la ruta completa de la Línea 1 del Metro de Barcelona (Hospital de Bellvitge — Fondo). Genera las 30 estaciones con sus posiciones kilométricas, correspondencias y archivos de megafonía, así como la señalización automática de cantones.
/// </summary>
public static class Line1Builder
{
    public static (List<Station> Stations, List<Signal> Signals) BuildFullLine1()
    {
        // [BLOQUE: Definición de las 30 estaciones con distancias reales y correspondencias]
        var rawStations = new (string Name, float DistanceFromPrev, string Connections, int Track, string AudioFile)[]
        {
            ("HOSPITAL DE BELLVITGE", 50f, "TERMINAL L1", 1, ""),
            ("BELLVITGE", 650f, "", 1, "EstacioBellvitge.mp3"),
            ("AV. CARRILET", 700f, "FGC L8", 2, "EstacioCarrilet.mp3"),
            ("RAMBLA JUST OLIVERAS", 680f, "RODALIES", 1, "EstacioJustOliveras.mp3"),
            ("CAN SERRA", 650f, "", 1, "EstacioCanSerra.mp3"),
            ("FLORIDA", 620f, "", 1, "EstacioFlorida.mp3"),
            ("TORRASSA", 680f, "L9S · L10S", 2, "EstacioTorrassa.mp3"),
            ("SANTA EULALIA", 650f, "", 1, "EstacioSantaEulalia.mp3"),
            ("MERCAT NOU", 600f, "", 1, "EstacioMercatNou.mp3"),
            ("PLACA DE SANTS", 640f, "L5", 1, "EstacioPlSants.mp3"),
            ("HOSTAFRANCS", 580f, "", 1, "EstacioHostafrancs.mp3"),
            ("ESPANYA", 660f, "L3 · FGC", 2, "EstacioEspanya.mp3"),
            ("ROCAFORT", 620f, "", 1, "EstacioRocafort.mp3"),
            ("URGELL", 600f, "", 1, "EstacioUrgell.mp3"),
            ("UNIVERSITAT", 640f, "L2", 1, "EstacioUniversitat.mp3"),
            ("CATALUNYA", 650f, "L3 · FGC · RODALIES", 2, "EstacioCatalunya.mp3"),
            ("URQUINAONA", 600f, "L4", 1, "EstacioUrquinaona.mp3"),
            ("ARC DE TRIOMF", 680f, "RODALIES · BUS NORD", 1, "EstacioArcTriomf.mp3"),
            ("MARINA", 660f, "TRAM T4", 1, "EstacioMarina.mp3"),
            ("GLORIES", 680f, "TRAM T4·T5·T6", 2, "EstacioGlories.mp3"),
            ("CLOT", 720f, "L2 · RODALIES", 1, "EstacioClot.mp3"),
            ("NAVAS", 650f, "", 1, "EstacioNavas.mp3"),
            ("LA SAGRERA", 700f, "L5 · L9N · L10N · ROD", 2, "EstacioSagrera.mp3"),
            ("FABRA I PUIG", 740f, "RODALIES · BUS", 1, "EstacioFabraPuig.mp3"),
            ("SANT ANDREU", 720f, "RODALIES", 1, "EstacioSantAndreu.mp3"),
            ("TORRAS I BAGES", 700f, "", 1, "EstacioTorresBages.mp3"),
            ("TRINITAT VELLA", 780f, "", 1, "EstacioTrinitatVella.mp3"),
            ("BARO DE VIVER", 720f, "", 1, "EstacioBaroViver.mp3"),
            ("SANTA COLOMA", 760f, "", 1, "EstacioSantaColoma.mp3"),
            ("FONDO", 740f, "L9N · FIN DE LINEA", 1, "EstacioFondo.mp3")
        };

        // [BLOQUE: Generación secuencial de estaciones y posiciones kilométricas]
        var stations = new List<Station>();
        var signals = new List<Signal>();
        float currentPos = 0f;

        for (int i = 0; i < rawStations.Length; i++)
        {
            var data = rawStations[i];
            currentPos += data.DistanceFromPrev;

            stations.Add(new Station(data.Name, currentPos, data.Connections, 95f, data.Track, data.AudioFile));

            // [BLOQUE: Generación automática de señales por cantón (salida, intermedia, entrada)]
            if (i < rawStations.Length - 1)
            {
                float nextDist = rawStations[i + 1].DistanceFromPrev;
                int nextTrack = rawStations[i + 1].Track;

                signals.Add(new Signal(currentPos + 105f, SignalAspect.Green, $"SALIDA C{i + 1:00}", 75));

                float midPos = currentPos + (nextDist * 0.52f);
                int midSpeed = nextDist >= 720f ? 80 : 75;
                signals.Add(new Signal(midPos, SignalAspect.Green, nextDist >= 720f ? "RECTA 80" : $"CANTON {i + 1:00}B", midSpeed));

                float entryPos = currentPos + nextDist - 120f;
                signals.Add(nextTrack == 2
                    ? new Signal(entryPos, SignalAspect.Green, "AGUJAS 45", 45)
                    : new Signal(entryPos, SignalAspect.Green, "ENTRADA 65", 65));
            }
            else
            {
                signals.Add(new Signal(currentPos + 25f, SignalAspect.Red, "TOPE FIN L1", 15));
            }
        }

        return (stations, signals);
    }
}
