using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using videojuego.Entities;
using videojuego.Systems;
using videojuego.World;

namespace videojuego.Core;

/// <summary>
/// Motor de renderizado 2D del simulador. Dibuja el mundo (túnel, estaciones, señales, tren Serie 6000), la consola de cabina (HUD) y el termómetro de ruta. Implementa IDisposable para liberar la textura auxiliar.
/// </summary>
public class MetroRenderer : IDisposable
{
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;
    private readonly List<(string Label, Color Bg, Color Fg)> _badgeBuffer = new();

    private const float PixelsPerMeter = 12f; // Escala Full HD
    private float _doorAnimProgress = 1f;
    private float _wheelRotation = 0f;

    // Paleta oficial TMB Barcelona
    private readonly Color _tmbRed = new Color(218, 41, 28);
    private readonly Color _tmbDarkRed = new Color(150, 22, 14);
    private readonly Color _trainBody = new Color(238, 241, 245);
    private readonly Color _trainShadow = new Color(192, 198, 210);
    private readonly Color _tunnelBg = new Color(12, 14, 19);

    public MetroRenderer(GraphicsDevice graphicsDevice, SpriteFont font)
    {
        // [BLOQUE: Inicialización de tipografía y textura base de 1x1 píxel]
        _font = font;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Dispose()
    {
        // [BLOQUE: Liberación de recursos gráficos no administrados]
        _pixel?.Dispose();
    }

    public void Update(float dt, MetroTrain train)
    {
        // [BLOQUE: Actualización de animación de puertas y rotación cinemática de ruedas]
        float targetDoor = train.DoorsOpen ? 1f : 0f;
        _doorAnimProgress = MathHelper.Lerp(_doorAnimProgress, targetDoor, dt * 6f);
        _wheelRotation += train.VelocityMps * dt * 2.5f;
    }

    public void DrawWorld(
        SpriteBatch sb,
        float virtualWidth,
        MetroTrain train,
        ServiceManager service,
        List<Station> stations,
        List<Signal> signals,
        InputConfig config)
    {
        // [BLOQUE: Cálculo de cámara, temblor por velocidad y coordenadas de referencia]
        int vw = (int)Math.Ceiling(virtualWidth);
        int groundY = 740;

        float shakeY = 0f;
        if (train.VelocityKmh > 15f)
        {
            float intensity = (train.VelocityKmh / 80f) * 1.8f;
            shakeY = (float)(Math.Sin(_wheelRotation * 3.0) * intensity);
        }

        // En pantallas anchas (21:9), la cabina mantiene margen izquierdo amplio y muestra mucho más túnel por delante
        float cameraAnchorX = Math.Min(760f, virtualWidth * 0.35f);
        float cameraOffsetX = cameraAnchorX - (train.PositionMeters * PixelsPerMeter);
        int baseGroundY = groundY + (int)shakeY;

        // [BLOQUE: Renderizado de bóveda y fondo del túnel]
        // 1. Fondo del túnel y bóveda superior a todo el ancho real (vw)
        DrawRect(sb, 0, 185, vw, baseGroundY - 185, _tunnelBg);
        DrawRect(sb, 0, 185, vw, 18, new Color(24, 28, 36));

        int minVisibleMeter = Math.Max(0, (int)(train.PositionMeters - (cameraAnchorX / PixelsPerMeter) - 30));
        int maxVisibleMeter = (int)(train.PositionMeters + ((vw - cameraAnchorX) / PixelsPerMeter) + 40);

        int firstPillar = (minVisibleMeter / 26) * 26;
        for (int m = firstPillar; m <= maxVisibleMeter; m += 26)
        {
            float colX = cameraOffsetX + m * PixelsPerMeter;
            DrawRect(sb, (int)colX, 203, 22, baseGroundY - 203, new Color(21, 25, 33));
            DrawRect(sb, (int)colX + 17, 203, 5, baseGroundY - 203, new Color(15, 17, 23));

            DrawRect(sb, (int)colX - 160, 245, 320, 4, new Color(36, 42, 52));
            DrawRect(sb, (int)colX - 160, 257, 320, 3, new Color(62, 44, 34));

            int lampX = (int)colX + 120;
            DrawRect(sb, lampX - 45, 209, 90, 460, new Color(170, 200, 240) * 0.035f);
            DrawRect(sb, lampX - 24, 209, 48, 340, new Color(190, 215, 250) * 0.05f);
            DrawRect(sb, lampX - 18, 203, 36, 6, new Color(220, 235, 255));
        }

        // [BLOQUE: Renderizado de estaciones de la L1]
        // 2. Estaciones de la L1
        foreach (var st in stations)
        {
            DrawStation(sb, st, cameraOffsetX, baseGroundY);
        }

        // [BLOQUE: Renderizado de catenaria rígida suspendida]
        // 3. Catenaria rígida superior a todo el ancho (vw)
        int catenaryY = baseGroundY - 102;
        DrawRect(sb, 0, catenaryY, vw, 4, new Color(155, 118, 86));
        int firstHanger = (minVisibleMeter / 14) * 14;
        for (int m = firstHanger; m <= maxVisibleMeter; m += 14)
        {
            float hangerX = cameraOffsetX + m * PixelsPerMeter;
            DrawRect(sb, (int)hangerX, 203, 3, catenaryY - 203, new Color(48, 54, 65));
        }

        // [BLOQUE: Renderizado de vías, balasto, traviesas y carriles]
        // 4. Vías (balasto, traviesas y carril) a todo el ancho (vw)
        DrawRect(sb, 0, baseGroundY + 10, vw, 62, new Color(32, 28, 26));
        for (float m = minVisibleMeter; m <= maxVisibleMeter; m += 0.8f)
        {
            float tieX = cameraOffsetX + m * PixelsPerMeter;
            DrawRect(sb, (int)tieX, baseGroundY + 6, 5, 10, new Color(88, 92, 100));
        }
        DrawRect(sb, 0, baseGroundY, vw, 6, new Color(168, 175, 186));
        DrawRect(sb, 0, baseGroundY + 6, vw, 4, new Color(78, 84, 92));

        // [BLOQUE: Renderizado de señales luminosas y balizas ATP]
        // 5. Señalización y balizas ATP
        foreach (var sig in signals)
        {
            DrawSignal(sb, sig, cameraOffsetX, baseGroundY);
        }

        // [BLOQUE: Renderizado del tren Serie 6000]
        // 6. Tren Serie 6000 de 5 coches
        DrawTrainSeries6000(sb, train, cameraOffsetX, baseGroundY, catenaryY);

        // [BLOQUE: Renderizado de la consola de cabina e instrumentos (HUD)]
        // 7. Consola de cabina inferior centrada/adaptada
        DrawCabinHUD(sb, vw, train, service, config);

        // [BLOQUE: Renderizado del termómetro oficial de la L1]
        // 8. Termómetro superior oficial de la L1 (TMB)
        DrawRouteThermometer(sb, vw, train, stations);
    }

    private void DrawStation(SpriteBatch sb, Station st, float cameraOffsetX, int groundY)
    {
        // [BLOQUE: Verificación de visibilidad en pantalla]
        float stX = cameraOffsetX + (st.PositionMeters - st.PlatformLength) * PixelsPerMeter;
        int stWidth = (int)(st.PlatformLength * PixelsPerMeter);

        if (stX + stWidth < -150 || stX > 2070) return;

        // [BLOQUE: Estructura del andén, muro, franja roja L1 y borde de seguridad]
        // Muro iluminado de estación y cornisa
        DrawRect(sb, (int)stX, 190, stWidth, groundY - 190, new Color(58, 64, 76));
        DrawRect(sb, (int)stX, 190, stWidth, 20, new Color(40, 45, 55));

        // Franja roja L1 con filetes blancos
        DrawRect(sb, (int)stX, 310, stWidth, 34, Color.White);
        DrawRect(sb, (int)stX, 313, stWidth, 28, _tmbRed);

        // Zócalo y borde amarillo de andén
        DrawRect(sb, (int)stX, groundY - 32, stWidth, 32, new Color(46, 50, 58));
        DrawRect(sb, (int)stX, groundY - 12, stWidth, 12, new Color(235, 195, 35));

        // [BLOQUE: Cartelería corporativa TMB, correspondencias y teleindicadores LED]
        // Carteles autoajustables al tamaño exacto del texto
        Vector2 nameSize = _font.MeasureString(st.Name);
        int signW = Math.Max(220, (int)nameSize.X + 36);
        int signH = 40;

        for (int offset = 110; offset < stWidth - signW - 80; offset += signW + 140)
        {
            int bx = (int)stX + offset;

            // Cartel rojo oficial TMB centrado con el nombre
            DrawRect(sb, bx, 255, signW, signH, Color.White);
            DrawRect(sb, bx + 3, 258, signW - 6, signH - 6, _tmbRed);
            sb.DrawString(_font, st.Name, new Vector2(bx + (signW - nameSize.X) / 2f, 255 + (signH - nameSize.Y) / 2f), Color.White);

            // Cartel de correspondencia (FGC / Rodalies) ajustado a su texto
            if (!string.IsNullOrEmpty(st.Connections))
            {
                Vector2 connSize = _font.MeasureString(st.Connections);
                int connW = (int)connSize.X + 24;
                DrawRect(sb, bx, 354, connW, 28, new Color(22, 26, 34));
                sb.DrawString(_font, st.Connections, new Vector2(bx + 12, 354 + (28 - connSize.Y) / 2f), Color.LightGray);
            }

            // Teleindicador LED colgante autoajustado
            string etaText = st.Visited ? "L1 FONDO 3 min" : "L1 FONDO ARA";
            Vector2 etaSize = _font.MeasureString(etaText);
            int etaW = (int)etaSize.X + 24;
            int etaX = bx + (signW - etaW) / 2;

            DrawRect(sb, etaX + etaW / 2 - 2, 190, 4, 24, Color.DarkGray);
            DrawRect(sb, etaX, 214, etaW, 26, new Color(12, 14, 18));
            sb.DrawString(_font, etaText, new Vector2(etaX + 12, 214 + (26 - etaSize.Y) / 2f), Color.Orange);

            // Banco de andén
            DrawRect(sb, bx + 20, groundY - 40, 84, 10, new Color(135, 142, 155));
            DrawRect(sb, bx + 30, groundY - 30, 5, 18, Color.Gray);
            DrawRect(sb, bx + 88, groundY - 30, 5, 18, Color.Gray);
        }

        // [BLOQUE: Pasajeros en espera en el andén]
        // Pasajeros en el andén
        if (!st.Visited)
        {
            Color[] coatColors = { Color.CornflowerBlue, Color.IndianRed, Color.Teal, Color.Goldenrod, Color.SlateGray };
            for (int p = 80; p < stWidth - 100; p += 75)
            {
                int px = (int)stX + p;
                Color coat = coatColors[(p / 75) % coatColors.Length];
                DrawRect(sb, px, groundY - 38, 10, 20, coat);
                DrawRect(sb, px + 1, groundY - 47, 8, 8, new Color(235, 195, 165));
                DrawRect(sb, px + 1, groundY - 18, 4, 6, new Color(28, 32, 42));
                DrawRect(sb, px + 5, groundY - 18, 4, 6, new Color(28, 32, 42));
            }
        }

        // [BLOQUE: Poste de parada exacta (PAR)]
        // Poste de parada exacta ("PAR")
        float stopX = cameraOffsetX + st.PositionMeters * PixelsPerMeter;
        string parText = "PAR";
        Vector2 parSize = _font.MeasureString(parText);
        int parW = (int)parSize.X + 18;
        int parH = 28;

        DrawRect(sb, (int)stopX - 2, groundY - 130, 4, 130, Color.LightGray);
        DrawRect(sb, (int)stopX - parW / 2, groundY - 130 - parH, parW, parH, new Color(0, 90, 180));
        sb.DrawString(_font, parText, new Vector2(stopX - parSize.X / 2f, groundY - 130 - parH + (parH - parSize.Y) / 2f), Color.White);
    }

    private void DrawSignal(SpriteBatch sb, Signal sig, float cameraOffsetX, int groundY)
    {
        // [BLOQUE: Verificación de visibilidad de la señal]
        float sigX = cameraOffsetX + sig.PositionMeters * PixelsPerMeter;
        if (sigX < -150 || sigX > 2070) return;

        // [BLOQUE: Baliza inductiva ATP de vía]
        // Baliza ATP
        DrawRect(sb, (int)sigX - 10, groundY - 4, 20, 6, Color.Gold);

        // [BLOQUE: Mástil, cabeza de señal y óptica activa con halo]
        // Mástil y pantalla alta
        DrawRect(sb, (int)sigX - 2, groundY - 190, 4, 190, new Color(110, 115, 125));
        DrawRect(sb, (int)sigX - 14, groundY - 260, 28, 76, new Color(12, 14, 18));

        DrawRect(sb, (int)sigX - 7, groundY - 250, 14, 14, new Color(40, 15, 15));
        DrawRect(sb, (int)sigX - 7, groundY - 228, 14, 14, new Color(40, 35, 10));
        DrawRect(sb, (int)sigX - 7, groundY - 206, 14, 14, new Color(15, 40, 15));

        Color activeColor = sig.Aspect switch
        {
            SignalAspect.Green => Color.LimeGreen,
            SignalAspect.Yellow => Color.Gold,
            _ => Color.Red
        };
        int lightY = sig.Aspect switch
        {
            SignalAspect.Red => groundY - 250,
            SignalAspect.Yellow => groundY - 228,
            _ => groundY - 206
        };

        // Halo suave translúcido
        DrawRect(sb, (int)sigX - 14, lightY - 7, 28, 28, activeColor * 0.25f);
        DrawRect(sb, (int)sigX - 7, lightY, 14, 14, activeColor);

        // [BLOQUE: Cartel alfanumérico luminoso del CCM]
        // Cartel luminoso del CCM autoajustado al ancho de su texto
        Vector2 textSize = _font.MeasureString(sig.SignText);
        int boxW = Math.Max(90, (int)textSize.X + 20);
        int boxH = 26;
        int boxX = (int)sigX - boxW / 2;
        int boxY = groundY - 176;

        DrawRect(sb, boxX, boxY, boxW, boxH, new Color(12, 28, 62));
        sb.DrawString(_font, sig.SignText, new Vector2(boxX + (boxW - textSize.X) / 2f, boxY + (boxH - textSize.Y) / 2f), Color.Cyan);
    }

    private void DrawTrainSeries6000(SpriteBatch sb, MetroTrain train, float cameraOffsetX, int groundY, int catenaryY)
    {
        // [BLOQUE: Dimensiones generales del convoy y faros frontales]
        int totalWidthPx = (int)(train.Length * PixelsPerMeter);
        int trainStartX = (int)(cameraOffsetX + (train.PositionMeters - train.Length) * PixelsPerMeter);
        int carHeight = 80;
        int trainY = groundY - carHeight - 10;

        // Haz de luz cónico translúcido de los faros frontales (sin bloques opacos)
        int noseX = trainStartX + totalWidthPx;
        for (int beam = 0; beam < 260; beam += 10)
        {
            int beamHeight = 16 + (beam / 4);
            float alpha = Math.Max(0.01f, 0.14f * (1f - (beam / 260f)));
            DrawRect(sb, noseX + beam, trainY + 52 - (beamHeight / 3), 10, beamHeight, new Color(255, 248, 210) * alpha);
        }

        // [BLOQUE: Renderizado iterativo de coches, fuelles, bogies y carrocería]
        int carCount = 5;
        int gap = 8;
        int carWidth = (totalWidthPx - (gap * (carCount - 1))) / carCount;

        for (int c = 0; c < carCount; c++)
        {
            int cx = trainStartX + c * (carWidth + gap);
            bool isFrontCab = (c == carCount - 1);
            bool isRearCab = (c == 0);

            if (c < carCount - 1)
                DrawRect(sb, cx + carWidth, trainY + 8, gap, carHeight - 14, new Color(26, 28, 34));

            DrawRect(sb, cx + 14, groundY - 14, carWidth - 28, 10, new Color(34, 38, 45));
            DrawBogie(sb, cx + 32, groundY - 7);
            DrawBogie(sb, cx + carWidth - 32, groundY - 7);

            DrawRect(sb, cx, trainY, carWidth, carHeight, _trainBody);
            DrawRect(sb, cx, trainY + carHeight - 8, carWidth, 8, _trainShadow);

            // Techo y equipos de climatización
            DrawRect(sb, cx, trainY, carWidth, 11, new Color(92, 98, 108));
            DrawRect(sb, cx + 30, trainY - 5, 42, 5, new Color(128, 134, 146));
            DrawRect(sb, cx + carWidth - 72, trainY - 5, 42, 5, new Color(128, 134, 146));

            // Pantógrafos
            if (c == 1 || c == 3)
            {
                int px = cx + carWidth / 2;
                int pantTop = catenaryY + 4;
                DrawRect(sb, px - 14, pantTop, 28, 4, new Color(200, 150, 90));
                DrawRect(sb, px - 2, pantTop + 4, 4, trainY - pantTop - 4, Color.Gray);
            }

            // Franja roja L1
            DrawRect(sb, cx, trainY + 56, carWidth, 16, _tmbRed);
            DrawRect(sb, cx, trainY + 54, carWidth, 2, _tmbDarkRed);

            // 3 puertas correderas por coche
            int doorSpacing = carWidth / 3;
            for (int d = 0; d < 3; d++)
            {
                int dx = cx + 16 + d * doorSpacing;
                int doorW = 28;
                int doorH = 46;
                int dy = trainY + 18;

                DrawRect(sb, dx, dy, doorW, doorH, new Color(255, 234, 175));

                int leafW = (int)((doorW / 2f) * (1f - _doorAnimProgress * 0.85f));
                DrawRect(sb, dx, dy, leafW, doorH, new Color(192, 198, 210));
                DrawRect(sb, dx + doorW - leafW, dy, leafW, doorH, new Color(192, 198, 210));

                if (d < 2)
                    DrawRect(sb, dx + doorW + 8, trainY + 20, doorSpacing - doorW - 16, 24, new Color(32, 40, 52));
            }

            // [BLOQUE: Cabinas de conducción e iluminación de testeros]
            if (isFrontCab)
            {
                DrawRect(sb, cx + carWidth - 18, trainY + 10, 18, 46, new Color(26, 30, 38));
                DrawRect(sb, cx + carWidth - 20, trainY + 12, 16, 12, Color.Black);
                DrawRect(sb, cx + carWidth - 18, trainY + 14, 12, 8, _tmbRed);
                DrawRect(sb, cx + carWidth - 5, trainY + 58, 6, 7, Color.White);
            }
            else if (isRearCab)
            {
                DrawRect(sb, cx, trainY + 58, 5, 7, Color.Red);
            }
        }
    }

    private void DrawBogie(SpriteBatch sb, int centerX, int wheelCenterY)
    {
        // [BLOQUE: Bastidor del bogie y montaje de pares de ruedas]
        DrawRect(sb, centerX - 18, wheelCenterY - 6, 36, 7, new Color(55, 60, 68));
        DrawWheel(sb, centerX - 12, wheelCenterY);
        DrawWheel(sb, centerX + 12, wheelCenterY);
    }

    private void DrawWheel(SpriteBatch sb, int cx, int cy)
    {
        // [BLOQUE: Llanta, cubo y punto rotatorio de rueda]
        DrawRect(sb, cx - 6, cy - 6, 12, 12, new Color(120, 128, 138));
        DrawRect(sb, cx - 3, cy - 3, 6, 6, new Color(65, 70, 78));
        int dotX = cx + (int)(Math.Cos(_wheelRotation) * 3.0);
        int dotY = cy + (int)(Math.Sin(_wheelRotation) * 3.0);
        DrawRect(sb, dotX, dotY, 2, 2, Color.White);
    }

    private void DrawPanel(SpriteBatch sb, int x, int y, int w, int h)
    {
        // [BLOQUE: Marco y fondo con relieve para panel instrumental]
        DrawRect(sb, x, y, w, h, new Color(44, 50, 62));
        DrawRect(sb, x + 2, y + 2, w - 4, h - 4, new Color(24, 28, 36));
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int width, int height, Color color)
    {
        // [BLOQUE: Dibujado de rectángulo coloreado mediante textura de 1x1]
        sb.Draw(_pixel, new Rectangle(x, y, width, height), color);
    }

    private void DrawRouteThermometer(SpriteBatch sb, int vw, MetroTrain train, List<Station> stations)
    {
        // [BLOQUE: Validación y fondo del panel del termómetro de línea]
        if (stations == null || stations.Count < 2) return;

        DrawRect(sb, 0, 0, vw, 185, new Color(246, 247, 249));
        DrawRect(sb, 0, 181, vw, 4, _tmbRed);

        int lineY = 122;
        int startX = 115;
        int endX = vw - 115;
        int totalLineW = endX - startX;

        // [BLOQUE: Logotipos oficiales de línea L1 en extremos]
        DrawRect(sb, 42, lineY - 22, 44, 44, _tmbRed);
        Vector2 l1Size = _font.MeasureString("L1") * 0.85f;
        sb.DrawString(_font, "L1", new Vector2(42 + (44 - l1Size.X) / 2f, lineY - l1Size.Y / 2f), Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        DrawRect(sb, vw - 86, lineY - 22, 44, 44, _tmbRed);
        sb.DrawString(_font, "L1", new Vector2(vw - 86 + (44 - l1Size.X) / 2f, lineY - l1Size.Y / 2f), Color.White, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        // [BLOQUE: Trazo de vía y barra de progreso del convoy]
        float firstPos = stations[0].PositionMeters;
        float lastPos = stations[^1].PositionMeters;
        float progressRatio = Math.Clamp((train.PositionMeters - firstPos) / (lastPos - firstPos), 0f, 1f);
        int trainMarkerX = startX + (int)(progressRatio * totalLineW);

        DrawRect(sb, startX, lineY - 4, totalLineW, 8, new Color(235, 175, 170));
        DrawRect(sb, startX, lineY - 4, trainMarkerX - startX, 8, _tmbRed);

        // [BLOQUE: Estaciones intermedias, rotulación a 45 grados y enlaces]
        float stepX = (float)totalLineW / (stations.Count - 1);

        for (int i = 0; i < stations.Count; i++)
        {
            var st = stations[i];
            int sx = startX + (int)Math.Round(i * stepX);
            bool passed = train.PositionMeters >= st.PositionMeters - 5f;
            bool hasConnections = !string.IsNullOrEmpty(st.Connections) && st.Connections != "TERMINAL L1";

            float textScale = 0.56f;
            Color nameColor = passed ? Color.Black : new Color(75, 80, 90);
            string displayName = st.Name
                .Replace("HOSPITAL DE BELLVITGE", "Hospital de Bellvitge")
                .Replace("RAMBLA JUST OLIVERAS", "Rbla. Just Oliveras");

            sb.DrawString(_font, displayName, new Vector2(sx - 4, lineY - 14), nameColor, -MathHelper.PiOver4, Vector2.Zero, textScale, SpriteEffects.None, 0f);

            if (hasConnections)
            {
                DrawRect(sb, sx - 8, lineY - 8, 16, 16, Color.Black);
                DrawRect(sb, sx - 5, lineY - 5, 10, 10, passed ? Color.Gold : Color.White);
            }
            else
            {
                DrawRect(sb, sx - 2, lineY - 9, 4, 14, passed ? _tmbRed : new Color(210, 120, 115));
            }

            DrawConnectionBadges(sb, st.Name, sx, lineY + 13);
        }

        // [BLOQUE: Marcador gráfico de posición en tiempo real del tren]
        DrawRect(sb, trainMarkerX - 9, lineY - 11, 18, 22, Color.Black);
        DrawRect(sb, trainMarkerX - 7, lineY - 9, 14, 18, Color.Cyan);
        DrawRect(sb, trainMarkerX - 3, lineY - 5, 6, 10, Color.White);
    }

    private void DrawCabinHUD(SpriteBatch sb, int vw, MetroTrain train, ServiceManager service, InputConfig config)
    {
        // [BLOQUE: Fondo de consola y franja superior de cabina]
        int hudTop = 812;
        int hudHeight = 1080 - hudTop;

        DrawRect(sb, 0, hudTop, vw, hudHeight, new Color(15, 17, 22));
        DrawRect(sb, 0, hudTop, vw, 5, _tmbRed);

        // Centramos el bloque instrumental de 1840px (o lo adaptamos si la pantalla es más ancha)
        int hudContentW = Math.Min(vw - 80, 1840);
        int offsetX = (vw - hudContentW) / 2;

        // [BLOQUE: Panel de mensajes del Centro de Control (CCM)]
        // 1. Barra de mensajes del Centro de Control (CCM)
        DrawPanel(sb, offsetX, hudTop + 20, hudContentW, 42);
        Color msgColor = train.EmergencyBrakeTriggered ? Color.Red : Color.White;
        sb.DrawString(_font, $"CCM L1  >  {service.StatusMessage}", new Vector2(offsetX + 18, hudTop + 28), msgColor, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        int cardsY = hudTop + 76;
        int cardH = 124;

        // [BLOQUE: Tarjeta 1 - Velocímetro analógico y limitación de velocidad ATP]
        // TARJETA 1: VELOCÍMETRO Y ATP
        DrawPanel(sb, offsetX, cardsY, 540, cardH);
        sb.DrawString(_font, $"VELOCIDAD: {train.VelocityKmh:00.0} km/h", new Vector2(offsetX + 20, cardsY + 14), Color.Cyan, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);
        string limitStr = $"MAX ATP: {service.CurrentSpeedLimitKmh} km/h";
        Vector2 limitSize = _font.MeasureString(limitStr) * 0.85f;
        sb.DrawString(_font, limitStr, new Vector2(offsetX + 540 - limitSize.X - 20, cardsY + 14), Color.OrangeRed, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        DrawRect(sb, offsetX + 20, cardsY + 56, 500, 32, new Color(32, 36, 44));
        int speedBarWidth = (int)(Math.Min(1f, train.VelocityKmh / 80f) * 500f);
        Color speedColor = train.VelocityKmh > service.CurrentSpeedLimitKmh ? Color.OrangeRed : Color.Cyan;
        DrawRect(sb, offsetX + 20, cardsY + 56, speedBarWidth, 32, speedColor);

        int limitMarkerX = offsetX + 20 + (int)((service.CurrentSpeedLimitKmh / 80f) * 500f);
        DrawRect(sb, limitMarkerX - 2, cardsY + 50, 4, 44, Color.Red);

        // [BLOQUE: Tarjeta 2 - Regulador y muescas de tracción y freno]
        // TARJETA 2: REGULADOR DE TRACCIÓN/FRENO
        Keys accelKey = config.Bindings[GameAction.Accelerate];
        Keys brakeKey = config.Bindings[GameAction.Brake];
        int card2X = offsetX + 565;
        DrawPanel(sb, card2X, cardsY, 490, cardH);

        string notchLabel = train.Notch switch
        {
            -4 => "EMERGENCIA (-4)",
            < 0 => $"FRENO ({train.Notch})",
            0 => "DERIVA (0)",
            > 0 => $"TRACCION (+{train.Notch})"
        };
        sb.DrawString(_font, $"REGULADOR [{accelKey}/{brakeKey}]: {notchLabel}", new Vector2(card2X + 20, cardsY + 14), Color.LightGray, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        DrawRect(sb, card2X + 20, cardsY + 56, 450, 32, new Color(32, 36, 44));
        int notchX = card2X + 20 + (train.Notch + 4) * 50;
        Color notchColor = train.Notch > 0 ? Color.LimeGreen : (train.Notch < 0 ? Color.OrangeRed : Color.White);
        DrawRect(sb, notchX, cardsY + 52, 50, 40, notchColor);

        // [BLOQUE: Tarjeta 3 - Estado de puertas y barra de progreso de embarque]
        // TARJETA 3: PUERTAS Y EMBARQUE
        Keys doorsKey = config.Bindings[GameAction.Doors];
        int card3X = offsetX + 1080;
        DrawPanel(sb, card3X, cardsY, 420, cardH);
        sb.DrawString(_font, $"PUERTAS [{doorsKey}]: {(train.DoorsOpen ? "ABIERTAS" : "CERRADAS")}", new Vector2(card3X + 20, cardsY + 14), Color.Gold, 0f, Vector2.Zero, 0.85f, SpriteEffects.None, 0f);

        DrawRect(sb, card3X + 20, cardsY + 56, 380, 32, new Color(32, 36, 44));
        DrawRect(sb, card3X + 20, cardsY + 56, (int)(service.BoardingProgress * 380f), 32, train.DoorsOpen ? Color.Gold : Color.DarkSlateGray);

        // [BLOQUE: Tarjeta 4 - Telemetría de pasaje a bordo y puntuación]
        // TARJETA 4: TELEMETRÍA DE SERVICIO
        int card4X = offsetX + 1525;
        DrawPanel(sb, card4X, cardsY, 315, cardH);
        sb.DrawString(_font, $"VIAJEROS: {service.PassengersOnBoard}", new Vector2(card4X + 20, cardsY + 20), Color.White, 0f, Vector2.Zero, 0.88f, SpriteEffects.None, 0f);
        sb.DrawString(_font, $"PUNTUACION: {service.Score}", new Vector2(card4X + 20, cardsY + 62), Color.LimeGreen, 0f, Vector2.Zero, 0.88f, SpriteEffects.None, 0f);

        // [BLOQUE: Barra inferior de asignación de teclas y ayuda]
        Keys resetKey = config.Bindings[GameAction.ResetATP];
        string helpText = $"[{accelKey}] Subir Muesca   ·   [{brakeKey}] Bajar Muesca   ·   [{doorsKey}] Puertas   ·   [{resetKey}] Rearmar ATP   ·   [ESC] Pausa   ·   [F11] Pantalla Completa";
        sb.DrawString(_font, helpText, new Vector2(offsetX + 4, hudTop + 218), Color.Gray, 0f, Vector2.Zero, 0.80f, SpriteEffects.None, 0f);
    }

    private void DrawConnectionBadges(SpriteBatch sb, string stationName, int centerX, int startY)
    {
        // [BLOQUE: Determinación y almacenamiento de líneas en el buffer de correspondencias]
        // Colores oficiales de líneas de Metro / FGC / Rodalies / Tram en Barcelona
        _badgeBuffer.Clear();

        switch (stationName)
        {
            case "AV. CARRILET":
                _badgeBuffer.Add(("L8", new Color(225, 130, 180), Color.White));
                break;
            case "RAMBLA JUST OLIVERAS":
                _badgeBuffer.Add(("R", new Color(235, 135, 20), Color.White));
                break;
            case "TORRASSA":
                _badgeBuffer.Add(("L9", new Color(240, 125, 25), Color.White));
                _badgeBuffer.Add(("L10", new Color(30, 155, 220), Color.White));
                break;
            case "PLACA DE SANTS":
                _badgeBuffer.Add(("L5", new Color(15, 105, 180), Color.White));
                break;
            case "ESPANYA":
                _badgeBuffer.Add(("L3", new Color(45, 165, 75), Color.White));
                _badgeBuffer.Add(("L8", new Color(225, 130, 180), Color.White));
                break;
            case "UNIVERSITAT":
                _badgeBuffer.Add(("L2", new Color(150, 45, 145), Color.White));
                break;
            case "CATALUNYA":
                _badgeBuffer.Add(("L3", new Color(45, 165, 75), Color.White));
                _badgeBuffer.Add(("L6", new Color(115, 105, 175), Color.White));
                _badgeBuffer.Add(("R", new Color(235, 135, 20), Color.White));
                break;
            case "URQUINAONA":
                _badgeBuffer.Add(("L4", new Color(245, 185, 15), Color.Black));
                break;
            case "ARC DE TRIOMF":
                _badgeBuffer.Add(("R", new Color(235, 135, 20), Color.White));
                break;
            case "MARINA":
            case "GLORIES":
                _badgeBuffer.Add(("T", new Color(0, 145, 130), Color.White));
                break;
            case "CLOT":
                _badgeBuffer.Add(("L2", new Color(150, 45, 145), Color.White));
                _badgeBuffer.Add(("R", new Color(235, 135, 20), Color.White));
                break;
            case "LA SAGRERA":
                _badgeBuffer.Add(("L5", new Color(15, 105, 180), Color.White));
                _badgeBuffer.Add(("L9", new Color(240, 125, 25), Color.White));
                _badgeBuffer.Add(("R", new Color(235, 135, 20), Color.White));
                break;
            case "FABRA I PUIG":
            case "SANT ANDREU":
                _badgeBuffer.Add(("R", new Color(235, 135, 20), Color.White));
                break;
            case "FONDO":
                _badgeBuffer.Add(("L9", new Color(240, 125, 25), Color.White));
                break;
        }

        // [BLOQUE: Renderizado iterativo de distintivos cromáticos]
        for (int b = 0; b < _badgeBuffer.Count; b++)
        {
            int by = startY + b * 14;
            int bw = 22;
            int bh = 12;
            DrawRect(sb, centerX - bw / 2, by, bw, bh, _badgeBuffer[b].Bg);

            float scale = 0.45f;
            Vector2 sz = _font.MeasureString(_badgeBuffer[b].Label) * scale;
            sb.DrawString(
                _font,
                _badgeBuffer[b].Label,
                new Vector2(centerX - sz.X / 2f, by + (bh - sz.Y) / 2f),
                _badgeBuffer[b].Fg,
                0f,
                Vector2.Zero,
                scale,
                SpriteEffects.None,
                0f
            );
        }
    }
}
