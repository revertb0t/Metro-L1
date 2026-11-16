using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using videojuego.Entities;
using videojuego.Systems;
using videojuego.World;

namespace videojuego.Core;

public class MetroRenderer
{
    private readonly Texture2D _pixel;
    private readonly SpriteFont _font;

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
        _font = font;
        _pixel = new Texture2D(graphicsDevice, 1, 1);
        _pixel.SetData(new[] { Color.White });
    }

    public void Update(float dt, MetroTrain train)
    {
        float targetDoor = train.DoorsOpen ? 1f : 0f;
        _doorAnimProgress = MathHelper.Lerp(_doorAnimProgress, targetDoor, dt * 6f);
        _wheelRotation += train.VelocityMps * dt * 2.5f;
    }

    public void DrawWorld(
        SpriteBatch sb,
        MetroTrain train,
        ServiceManager service,
        List<Station> stations,
        List<Signal> signals,
        InputConfig config)
    {
        int groundY = 740;

        float shakeY = 0f;
        if (train.VelocityKmh > 15f)
        {
            float intensity = (train.VelocityKmh / 80f) * 1.8f;
            shakeY = (float)(Math.Sin(_wheelRotation * 3.0) * intensity);
        }

        // Posición de cabina centrada en el tercio izquierdo-central de 1920px
        float cameraOffsetX = 680f - (train.PositionMeters * PixelsPerMeter);
        int baseGroundY = groundY + (int)shakeY;

        // 1. Fondo del túnel y bóveda superior
        DrawRect(sb, 0, 0, 1920, baseGroundY, _tunnelBg);
        DrawRect(sb, 0, 90, 1920, 24, new Color(24, 28, 36));

        // Pilares, cableado de túnel y luminarias con degradado suave
        for (int m = 0; m < 3500; m += 26)
        {
            float colX = cameraOffsetX + m * PixelsPerMeter;
            if (colX < -120 || colX > 2040) continue;

            DrawRect(sb, (int)colX, 114, 22, baseGroundY - 114, new Color(21, 25, 33));
            DrawRect(sb, (int)colX + 17, 114, 5, baseGroundY - 114, new Color(15, 17, 23));

            // Bandejas de cables
            DrawRect(sb, (int)colX - 160, 210, 320, 4, new Color(36, 42, 52));
            DrawRect(sb, (int)colX - 160, 222, 320, 3, new Color(62, 44, 34));

            // Luminaria fluorescente con halo translúcido real (Alpha premultiplicado)
            int lampX = (int)colX + 120;
            DrawRect(sb, lampX - 45, 142, 90, 460, new Color(170, 200, 240) * 0.035f);
            DrawRect(sb, lampX - 24, 142, 48, 340, new Color(190, 215, 250) * 0.05f);
            DrawRect(sb, lampX - 18, 136, 36, 6, new Color(220, 235, 255));
        }

        // 2. Estaciones de la L1
        foreach (var st in stations)
        {
            DrawStation(sb, st, cameraOffsetX, baseGroundY);
        }

        // 3. Catenaria rígida superior
        int catenaryY = baseGroundY - 102;
        DrawRect(sb, 0, catenaryY, 1920, 4, new Color(155, 118, 86));
        for (int m = 0; m < 3500; m += 14)
        {
            float hangerX = cameraOffsetX + m * PixelsPerMeter;
            if (hangerX > -20 && hangerX < 1940)
                DrawRect(sb, (int)hangerX, 114, 3, catenaryY - 114, new Color(48, 54, 65));
        }

        // 4. Vías (balasto, traviesas y carril)
        DrawRect(sb, 0, baseGroundY + 10, 1920, 62, new Color(32, 28, 26));
        for (float m = 0; m < 3500; m += 0.8f)
        {
            float tieX = cameraOffsetX + m * PixelsPerMeter;
            if (tieX > -10 && tieX < 1930)
                DrawRect(sb, (int)tieX, baseGroundY + 6, 5, 10, new Color(88, 92, 100));
        }
        DrawRect(sb, 0, baseGroundY, 1920, 6, new Color(168, 175, 186));
        DrawRect(sb, 0, baseGroundY + 6, 1920, 4, new Color(78, 84, 92));

        // 5. Señalización y balizas ATP
        foreach (var sig in signals)
        {
            DrawSignal(sb, sig, cameraOffsetX, baseGroundY);
        }

        // 6. Tren Serie 6000 de 5 coches
        DrawTrainSeries6000(sb, train, cameraOffsetX, baseGroundY, catenaryY);

        // 7. Consola de cabina inferior (1920x1080)
        DrawCabinHUD(sb, train, service, config);
    }

    private void DrawStation(SpriteBatch sb, Station st, float cameraOffsetX, int groundY)
    {
        float stX = cameraOffsetX + (st.PositionMeters - st.PlatformLength) * PixelsPerMeter;
        int stWidth = (int)(st.PlatformLength * PixelsPerMeter);

        if (stX + stWidth < -150 || stX > 2070) return;

        // Muro iluminado de estación y cornisa
        DrawRect(sb, (int)stX, 190, stWidth, groundY - 190, new Color(58, 64, 76));
        DrawRect(sb, (int)stX, 190, stWidth, 20, new Color(40, 45, 55));

        // Franja roja L1 con filetes blancos
        DrawRect(sb, (int)stX, 310, stWidth, 34, Color.White);
        DrawRect(sb, (int)stX, 313, stWidth, 28, _tmbRed);

        // Zócalo y borde amarillo de andén
        DrawRect(sb, (int)stX, groundY - 32, stWidth, 32, new Color(46, 50, 58));
        DrawRect(sb, (int)stX, groundY - 12, stWidth, 12, new Color(235, 195, 35));

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
        float sigX = cameraOffsetX + sig.PositionMeters * PixelsPerMeter;
        if (sigX < -150 || sigX > 2070) return;

        // Baliza ATP
        DrawRect(sb, (int)sigX - 10, groundY - 4, 20, 6, Color.Gold);

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
        DrawRect(sb, centerX - 18, wheelCenterY - 6, 36, 7, new Color(55, 60, 68));
        DrawWheel(sb, centerX - 12, wheelCenterY);
        DrawWheel(sb, centerX + 12, wheelCenterY);
    }

    private void DrawWheel(SpriteBatch sb, int cx, int cy)
    {
        DrawRect(sb, cx - 6, cy - 6, 12, 12, new Color(120, 128, 138));
        DrawRect(sb, cx - 3, cy - 3, 6, 6, new Color(65, 70, 78));
        int dotX = cx + (int)(Math.Cos(_wheelRotation) * 3.0);
        int dotY = cy + (int)(Math.Sin(_wheelRotation) * 3.0);
        DrawRect(sb, dotX, dotY, 2, 2, Color.White);
    }

    private void DrawCabinHUD(SpriteBatch sb, MetroTrain train, ServiceManager service, InputConfig config)
    {
        int hudTop = 812;
        int hudHeight = 1080 - hudTop;

        // Fondo de consola y línea roja superior
        DrawRect(sb, 0, hudTop, 1920, hudHeight, new Color(16, 18, 24));
        DrawRect(sb, 0, hudTop, 1920, 6, _tmbRed);

        // 1. Barra de mensajes del Centro de Control (CCM)
        DrawPanel(sb, 40, hudTop + 20, 1840, 42);
        Color msgColor = train.EmergencyBrakeTriggered ? Color.Red : Color.White;
        sb.DrawString(_font, $"CCM L1 > {service.StatusMessage}", new Vector2(58, hudTop + 31), msgColor);

        // 2. Cuatro tarjetas instrumentales separadas con amplio margen (cero solapamiento)
        int cardsY = hudTop + 78;
        int cardH = 122;

        // TARJETA 1: VELOCÍMETRO Y ATP (X: 40, Ancho: 540)
        DrawPanel(sb, 40, cardsY, 540, cardH);
        sb.DrawString(_font, $"VELOCIDAD: {train.VelocityKmh:00.0} km/h", new Vector2(60, cardsY + 16), Color.Cyan);
        string limitStr = $"MAX ATP: {service.CurrentSpeedLimitKmh} km/h";
        Vector2 limitSize = _font.MeasureString(limitStr);
        sb.DrawString(_font, limitStr, new Vector2(40 + 540 - limitSize.X - 20, cardsY + 16), Color.OrangeRed);

        DrawRect(sb, 60, cardsY + 56, 500, 32, new Color(32, 36, 44));
        int speedBarWidth = (int)(Math.Min(1f, train.VelocityKmh / 80f) * 500f);
        Color speedColor = train.VelocityKmh > service.CurrentSpeedLimitKmh ? Color.OrangeRed : Color.Cyan;
        DrawRect(sb, 60, cardsY + 56, speedBarWidth, 32, speedColor);

        int limitMarkerX = 60 + (int)((service.CurrentSpeedLimitKmh / 80f) * 500f);
        DrawRect(sb, limitMarkerX - 2, cardsY + 50, 4, 44, Color.Red);

        // TARJETA 2: REGULADOR DE TRACCIÓN/FRENO (X: 605, Ancho: 490)
        Keys accelKey = config.Bindings[GameAction.Accelerate];
        Keys brakeKey = config.Bindings[GameAction.Brake];
        DrawPanel(sb, 605, cardsY, 490, cardH);

        string notchLabel = train.Notch switch
        {
            -4 => "EMERGENCIA (-4)",
            < 0 => $"FRENO ({train.Notch})",
            0 => "DERIVA (0)",
            > 0 => $"TRACCION (+{train.Notch})"
        };
        sb.DrawString(_font, $"REGULADOR [{accelKey}/{brakeKey}]: {notchLabel}", new Vector2(625, cardsY + 16), Color.LightGray);

        DrawRect(sb, 625, cardsY + 56, 450, 32, new Color(32, 36, 44));
        int notchX = 625 + (train.Notch + 4) * 50;
        Color notchColor = train.Notch > 0 ? Color.LimeGreen : (train.Notch < 0 ? Color.OrangeRed : Color.White);
        DrawRect(sb, notchX, cardsY + 52, 50, 40, notchColor);

        // TARJETA 3: PUERTAS Y EMBARQUE (X: 1120, Ancho: 420)
        Keys doorsKey = config.Bindings[GameAction.Doors];
        DrawPanel(sb, 1120, cardsY, 420, cardH);
        sb.DrawString(_font, $"PUERTAS [{doorsKey}]: {(train.DoorsOpen ? "ABIERTAS" : "CERRADAS")}", new Vector2(1140, cardsY + 16), Color.Gold);

        DrawRect(sb, 1140, cardsY + 56, 380, 32, new Color(32, 36, 44));
        DrawRect(sb, 1140, cardsY + 56, (int)(service.BoardingProgress * 380f), 32, train.DoorsOpen ? Color.Gold : Color.DarkSlateGray);

        // TARJETA 4: TELEMETRÍA DE SERVICIO (X: 1565, Ancho: 315)
        DrawPanel(sb, 1565, cardsY, 315, cardH);
        sb.DrawString(_font, $"VIAJEROS: {service.PassengersOnBoard}", new Vector2(1585, cardsY + 22), Color.White);
        sb.DrawString(_font, $"PUNTUACION: {service.Score}", new Vector2(1585, cardsY + 62), Color.LimeGreen);

        // 3. Barra inferior de ayuda de controles
        Keys resetKey = config.Bindings[GameAction.ResetATP];
        string helpText = $"[{accelKey}] Subir Muesca   |   [{brakeKey}] Bajar Muesca   |   [{doorsKey}] Puertas   |   [{resetKey}] Rearmar ATP   |   [ESC] Menu Principal";
        sb.DrawString(_font, helpText, new Vector2(42, hudTop + 218), Color.Gray);
    }

    private void DrawPanel(SpriteBatch sb, int x, int y, int w, int h)
    {
        DrawRect(sb, x, y, w, h, new Color(44, 50, 62));
        DrawRect(sb, x + 2, y + 2, w - 4, h - 4, new Color(24, 28, 36));
    }

    private void DrawRect(SpriteBatch sb, int x, int y, int width, int height, Color color)
    {
        sb.Draw(_pixel, new Rectangle(x, y, width, height), color);
    }
}