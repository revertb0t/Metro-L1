using System;
using System.IO;

try
{
    using var game = new videojuego.Game1();
    game.Run();
}
catch (Exception ex)
{
    File.WriteAllText("crash.log", ex.ToString());
    Console.Error.WriteLine(ex.ToString());
}
