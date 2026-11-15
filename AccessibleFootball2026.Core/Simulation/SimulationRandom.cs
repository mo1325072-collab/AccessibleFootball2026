namespace AccessibleFootball2026.Core.Simulation;

/// <summary>
/// مصدر العشوائية الواحد لمحاكاة المباراة. في اللعب الفعلي هو Random.Shared؛
/// تستطيع الاختبارات تثبيت سلسلة حتمية ببذرة عبر Override لإعادة إنتاج أي سيناريو.
/// </summary>
internal static class SimulationRandom
{
    internal static Func<double>? Override;
    internal static double Next() => Override?.Invoke() ?? Random.Shared.NextDouble();
}
