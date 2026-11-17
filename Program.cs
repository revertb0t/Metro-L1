// Designed & Developed by Revert_b0t
// ============================================================================
// Punto de entrada principal (Entry Point) del videojuego.
// Se encarga de inicializar y arrancar la instancia del juego (Game1),
// así como de capturar y registrar de forma segura cualquier excepción no controlada.
// ============================================================================

// [BLOQUE: Espacios de nombres]
using System;
using System.IO;

// [BLOQUE: Ejecución principal del juego]
try
{
    using var game = new videojuego.Game1();
    game.Run();
}
// [BLOQUE: Manejo y registro de excepciones]
catch (Exception ex)
{
    // [BLOQUE: Escritura segura del archivo crash.log]
    try
    {
        string logPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log");
        File.WriteAllText(logPath, ex.ToString());
    }
    catch (Exception writeEx)
    {
        Console.Error.WriteLine($"Error al escribir en crash.log: {writeEx.Message}");
    }

    // [BLOQUE: Salida del error por consola]
    Console.Error.WriteLine(ex.ToString());
}
