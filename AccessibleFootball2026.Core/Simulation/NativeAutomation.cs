using AccessibleFootball2026.Core.Models;
using AccessibleFootball2026.Core.Tactics;

namespace AccessibleFootball2026.Core.Simulation;

public partial class MatchEngine
{
    private bool _substitutionStoppageActive;
    private int _substitutionStoppageSerial;
    private int _lastAutomaticSubstitutionStop = -1;
    private float _nextAutomaticTacticsMinute = 5;

    private void BeginSubstitutionStoppage()
    {
        if (_substitutionStoppageActive) return;
        _substitutionStoppageActive = true;
        _substitutionStoppageSerial++;
        HomeTeam.ResetStoppageWindowFlag();
        AwayTeam.ResetStoppageWindowFlag();
    }

    private void EndSubstitutionStoppage()
    {
        _substitutionStoppageActive = false;
        HomeTeam.ResetStoppageWindowFlag();
        AwayTeam.ResetStoppageWindowFlag();
    }

    /// <summary>Native rules, not Qwen. Never changes the user's opt-in settings.</summary>
    public void RunNativeAutomation()
    {
        if (PendingInjuryPlayer != null) return;
        if (State is MatchState.GameOver or MatchState.PenaltyShootout or MatchState.GoalCelebration or MatchState.Paused) return;
        bool naturalStop = State == MatchState.HalfTime || (AwaitingStoppageConfirmation && State != MatchState.Paused);
        if (naturalStop)
        {
            BeginSubstitutionStoppage();
            if (_lastAutomaticSubstitutionStop != _substitutionStoppageSerial)
            {
                _lastAutomaticSubstitutionStop = _substitutionStoppageSerial;
                // User-selected native fatigue/card-risk policy, applied only at a usable stopped-play opportunity.
                TacticalAiAdvisor.TryPerformAutoSubstitution(this, out _);
            }
        }
        if (MatchTimeMinutes < _nextAutomaticTacticsMinute) return;
        _nextAutomaticTacticsMinute = MatchTimeMinutes + 5;
        TacticalAiAdvisor.TryPerformDynamicTacticalAdjustment(this, HomeTeam, out _);
        TacticalAiAdvisor.TryPerformDynamicTacticalAdjustment(this, AwayTeam, out _, forceAi: true);
    }
}
