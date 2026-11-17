using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace videojuego.Core;

/// <summary>
/// Gestiona la transformación de coordenadas entre la resolución virtual (1920x1080) y la resolución real de la ventana. Permite soporte multi-aspecto (16:9, 21:9, etc.) escalando la altura virtual y ajustando el ancho dinámicamente.
/// </summary>
public class ViewportManager
{
    public const float VirtualHeight = 1080f;
    public float VirtualWidth { get; private set; } = 1920f;
    public float Scale { get; private set; } = 1f;
    public Matrix TransformMatrix { get; private set; } = Matrix.Identity;

    public void Update(GraphicsDevice gd)
    {
        // [BLOQUE: Cálculo de escala y dimensiones virtuales]
        if (gd == null) return;

        int screenW = gd.PresentationParameters.BackBufferWidth;
        int screenH = gd.PresentationParameters.BackBufferHeight;

        if (screenW <= 0 || screenH <= 0) return;

        Scale = screenH / VirtualHeight;
        if (Scale <= 0.01f) Scale = 1f;

        VirtualWidth = screenW / Scale;
        TransformMatrix = Matrix.CreateScale(Scale, Scale, 1f);
    }

    public Point ScreenToVirtual(Point screenPos)
    {
        // [BLOQUE: Conversión de coordenadas de pantalla a virtuales]
        return new Point((int)(screenPos.X / Scale), (int)(screenPos.Y / Scale));
    }

    public MouseState GetVirtualMouseState(MouseState rawMouse)
    {
        // [BLOQUE: Transformación del estado del ratón a coordenadas virtuales]
        Point vPos = ScreenToVirtual(rawMouse.Position);
        return new MouseState(
            vPos.X,
            vPos.Y,
            rawMouse.ScrollWheelValue,
            rawMouse.LeftButton,
            rawMouse.MiddleButton,
            rawMouse.RightButton,
            rawMouse.XButton1,
            rawMouse.XButton2
        );
    }
}
