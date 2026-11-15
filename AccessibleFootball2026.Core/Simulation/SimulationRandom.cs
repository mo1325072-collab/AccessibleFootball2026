namespace AccessibleFootball2026.Core.Simulation;
// Injectable deterministic sequence for reproducible match scenarios; default play uses independent random draws.
internal static class SimulationRandom
{
    internal static Func<double>? Override;
    internal static double Next() => Override?.Invoke() ?? Random.Shared.NextDouble();
}
