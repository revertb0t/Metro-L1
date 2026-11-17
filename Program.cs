// Designed & Developed by Revert_b0t
using System;
using System.IO;

try
{
    using var game = new videojuego.Game1();
    game.Run();
}
catch (Exception ex)
{
    try { File.WriteAllText(Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "crash.log"), ex.ToString()); }
    catch (Exception writeEx) { Console.Error.WriteLine($"Error al escribir en crash.log: {writeEx.Message}"); }
    Console.Error.WriteLine(ex.ToString());
}
