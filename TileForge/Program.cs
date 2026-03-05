using System;
using TileForge;

DebugLog.Log("Program starting");
try
{
    using var game = new TileForgeGame();
    DebugLog.Log("TileForgeGame constructed, calling Run()");
    game.Run();
    DebugLog.Log("Game.Run() exited normally");
}
catch (Exception ex)
{
    DebugLog.Error("FATAL — unhandled exception in Program.Main", ex);
    throw; // re-throw so the OS still reports the crash
}
