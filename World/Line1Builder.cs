using System;
using System.Collections.Generic;

namespace videojuego.World;

public static class Line1Builder
{
    public static (List<Station> Stations, List<Signal> Signals) BuildFullLine1()
    {
        // Las 30 estaciones oficiales de la L1 (Hospital de Bellvitge -> Fondo)
        // Distancias escaladas (~600m a 850m entre estaciones) para un recorrido jugable y fluido
        var rawStations = new (string Name, float DistanceFromPrev, string Connections, int Track)[]
        {
            ("HOSPITAL DE BELLVITGE", 50f, "TERMINAL L1", 1),
            ("BELLVITGE", 650f, "", 1),
            ("AV. CARRILET", 700f, "FGC L8", 2),
            ("RAMBLA JUST OLIVERAS", 680f, "RODALIES", 1),
            ("CAN SERRA", 650f, "", 1),
            ("FLORIDA", 620f, "", 1),
            ("TORRASSA", 680f, "L9S · L10S", 2),
            ("SANTA EULALIA", 650f, "", 1),
            ("MERCAT NOU", 600f, "", 1),
            ("PLACA DE SANTS", 640f, "L5", 1),
            ("HOSTAFRANCS", 580f, "", 1),
            ("ESPANYA", 660f, "L3 · FGC", 2),
            ("ROCAFORT", 620f, "", 1),
            ("URGELL", 600f, "", 1),
            ("UNIVERSITAT", 640f, "L2", 1),
            ("CATALUNYA", 650f, "L3 · FGC · RODALIES", 2),
            ("URQUINAONA", 600f, "L4", 1),
            ("ARC DE TRIOMF", 680f, "RODALIES · BUS NORD", 1),
            ("MARINA", 660f, "TRAM T4", 1),
            ("GLORIES", 680f, "TRAM T4·T5·T6", 2),
            ("CLOT", 720f, "L2 · RODALIES", 1),
            ("NAVAS", 650f, "", 1),
            ("LA SAGRERA", 700f, "L5 · L9N · L10N · ROD", 2),
            ("FABRA I PUIG", 740f, "RODALIES · BUS", 1),
            ("SANT ANDREU", 720f, "RODALIES", 1),
            ("TORRAS I BAGES", 700f, "", 1),
            ("TRINITAT VELLA", 780f, "", 1),
            ("BARO DE VIVER", 720f, "", 1),
            ("SANTA COLOMA", 760f, "", 1),
            ("FONDO", 740f, "L9N · FIN DE LINEA", 1)
        };

        var stations = new List<Station>();
        var signals = new List<Signal>();

        float currentPos = 0f;

        for (int i = 0; i < rawStations.Length; i++)
        {
            var data = rawStations[i];
            currentPos += data.DistanceFromPrev;

            stations.Add(new Station(data.Name, currentPos, data.Connections, 95f, data.Track));

            // Generar señalización automática hacia la siguiente estación
            if (i < rawStations.Length - 1)
            {
                float nextDist = rawStations[i + 1].DistanceFromPrev;
                int nextTrack = rawStations[i + 1].Track;

                // 1. Señal de salida de estación (a 105m del punto de parada)
                signals.Add(new Signal(
                    currentPos + 105f,
                    SignalAspect.Green,
                    $"SALIDA C{i + 1:00}",
                    75
                ));

                // 2. Señal intermedia de cantón (a mitad de túnel)
                float midPos = currentPos + (nextDist * 0.52f);
                int midSpeed = nextDist >= 720f ? 80 : 75;
                signals.Add(new Signal(
                    midPos,
                    SignalAspect.Green,
                    nextDist >= 720f ? "RECTA 80" : $"CANTON {i + 1:00}B",
                    midSpeed
                ));

                // 3. Señal de entrada a la siguiente estación (con límite de agujas si cambia de vía)
                float entryPos = currentPos + nextDist - 120f;
                if (nextTrack == 2)
                {
                    signals.Add(new Signal(entryPos, SignalAspect.Green, "AGUJAS 45", 45));
                }
                else
                {
                    signals.Add(new Signal(entryPos, SignalAspect.Green, "ENTRADA 65", 65));
                }
            }
            else
            {
                // Tope de vía en la estación terminal de FONDO
                signals.Add(new Signal(currentPos + 25f, SignalAspect.Red, "TOPE FIN L1", 15));
            }
        }

        return (stations, signals);
    }
}