// Designed & Developed by Revert_b0t
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
            ("Hospital de Bellvitge", 50f, "TERMINAL L1", 1, ""),
            ("Bellvitge", 650f, "", 1, "EstacioBellvitge.mp3"),
            ("Av. Carrilet", 700f, "FGC L8", 2, "EstacioCarrilet.mp3"),
            ("Rambla Just Oliveras", 680f, "RODALIES", 1, "EstacioJustOliveras.mp3"),
            ("Can Serra", 650f, "", 1, "EstacioCanSerra.mp3"),
            ("Florida", 620f, "", 1, "EstacioFlorida.mp3"),
            ("Torrassa", 680f, "L9S · L10S", 2, "EstacioTorrassa.mp3"),
            ("Santa Eulàlia", 650f, "", 1, "EstacioSantaEulalia.mp3"),
            ("Mercat Nou", 600f, "", 1, "EstacioMercatNou.mp3"),
            ("Plaça de Sants", 640f, "L5", 1, "EstacioPlSants.mp3"),
            ("Hostafrancs", 580f, "", 1, "EstacioHostafrancs.mp3"),
            ("Espanya", 660f, "L3 · FGC", 2, "EstacioEspanya.mp3"),
            ("Rocafort", 620f, "", 1, "EstacioRocafort.mp3"),
            ("Urgell", 600f, "", 1, "EstacioUrgell.mp3"),
            ("Universitat", 640f, "L2", 1, "EstacioUniversitat.mp3"),
            ("Catalunya", 650f, "L3 · FGC · RODALIES", 2, "EstacioCatalunya.mp3"),
            ("Urquinaona", 600f, "L4", 1, "EstacioUrquinaona.mp3"),
            ("Arc de Triomf", 680f, "RODALIES · BUS NORD", 1, "EstacioArcTriomf.mp3"),
            ("Marina", 660f, "TRAM T4", 1, "EstacioMarina.mp3"),
            ("Glòries", 680f, "TRAM T4·T5·T6", 2, "EstacioGlories.mp3"),
            ("Clot", 720f, "L2 · RODALIES", 1, "EstacioClot.mp3"),
            ("Navas", 650f, "", 1, "EstacioNavas.mp3"),
            ("La Sagrera", 700f, "L5 · L9N · L10N · ROD", 2, "EstacioSagrera.mp3"),
            ("Fabra i Puig", 740f, "RODALIES · BUS", 1, "EstacioFabraPuig.mp3"),
            ("Sant Andreu", 720f, "RODALIES", 1, "EstacioSantAndreu.mp3"),
            ("Torras i Bages", 700f, "", 1, "EstacioTorresBages.mp3"),
            ("Trinitat Vella", 780f, "", 1, "EstacioTrinitatVella.mp3"),
            ("Baró de Viver", 720f, "", 1, "EstacioBaroViver.mp3"),
            ("Santa Coloma", 760f, "", 1, "EstacioSantaColoma.mp3"),
            ("Fondo", 740f, "L9N · FIN DE LINEA", 1, "EstacioFondo.mp3")
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
